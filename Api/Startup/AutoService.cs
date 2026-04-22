using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Translate;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Api.AutoForms;

/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class AutoService<T> : AutoService<T, uint>
	where T : Content<uint>, new()
{
	/// <summary>
	/// Instanced automatically
	/// </summary>
	/// <param name="eventGroup"></param>
	public AutoService(EventGroup<T> eventGroup):base(eventGroup)
	{ }

	/// <summary>
	/// Quick helper function
	/// </summary>
	/// <returns></returns>
	public static bool IsTestSuite()
	{
		return Services.BuildHost == "xunit";
	}
}

/// <summary>
/// Options when requesting data from a service.
/// </summary>
public enum DataOptions : int
{
	/// <summary>
	/// Checks if the given row has not changed based on the EditedUtc date.
	/// </summary>
	CheckNotChanged = 8,
	
	/// <summary>
	/// Cache flag
	/// </summary>
	CacheFlag = 2,

	/// <summary>
	/// Perms flag
	/// </summary>
	PermissionsFlag = 1,

	/// <summary>
	/// Only use the database but permissions are active.
	/// </summary>
	NoCache = 1,
	/// <summary>
	/// Ignore permissions and only use the database.
	/// </summary>
	NoCacheIgnorePermissions = 0,
	/// <summary>
	/// Default with the permission system and cache active.
	/// </summary>
	Default = 3,
	/// <summary>
	/// Permissions will be disabled on this request for data. I hope you know what you're doing! 
	/// As a general piece of guidance, using this is fine if the data that you obtain is not returned directly to the end user.
	/// For example, the end user will likely be denied the ability to search users by email, but the login system needs to be able to do that.
	/// It's ok to ignore the permission engine given we're not just outright returning the user data unless the login is valid.
	/// </summary>
	IgnorePermissions = 2
}

/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="ID">ID type (usually int)</typeparam>
public partial class AutoService<T, ID> : AutoService, ContentStreamSource<T, ID>
	where T: Content<ID>, new()
	where ID: struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	/// <summary>
	/// The set of update/ delete/ create etc events for this type.
	/// </summary>
	public EventGroup<T, ID> EventGroup;

	/// <summary>
	/// Sets up the common service type fields.
	/// </summary>
	public AutoService(EventGroup eventGroup, Type instanceType = null, string entityName = null) : base(
		typeof(T),
		typeof(ID),
		instanceType,
		entityName
	)
	{
		EventGroup = eventGroup as EventGroup<T, ID>;

		if (typeof(ID) == typeof(uint))
		{
			_idConverter = new UInt32IDConverter() as IDConverter<ID>;
		}
		else if (typeof(ID) == typeof(ulong))
		{
			_idConverter = new UInt64IDConverter() as IDConverter<ID>;
		}
		else
		{
			throw new ArgumentException("Currently unrecognised ID type: ", nameof(ID));
		}
	}

	private IDConverter<ID> _idConverter;
	private JsonStructure<T,ID>[] _jsonStructures = null;

	/// <summary>
	/// Updates the instance type, clearing any internal caches which used it before.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="instanceType"></param>
	public override async ValueTask SetInstanceType(Context context, Type instanceType)
	{
		await base.SetInstanceType(context, instanceType);
		_jsonStructures = null;
		_filterSets.Clear();
		_diffFields = null;
		_diffDelegate = null;
		_cloneDelegate = null;
		await EventGroup.AfterInstanceTypeUpdate.Dispatch(context, this);
	}

	/// <summary>
	/// Gets a particular metadata field by its name. Common ones are "title" and "description".
	/// Use this to generically read common descriptive things about a given content type.
	/// Note that as fields vary by role, it is possible for users of different roles to obtain different meta values.
	/// </summary>
	public async ValueTask<JsonField<T,ID>> GetTypedMetaField(Context context, string fieldName)
	{
		var structure = await GetTypedJsonStructure(context);
		if (structure == null)
		{
			return null;
		}

		// Get the field:
		return structure.GetTypedMetaField(fieldName);
	}
	
	/// <summary>
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public override async ValueTask<JsonStructure> GetJsonStructure(Context ctx)
	{
		return await GetTypedJsonStructure(ctx);
	}

	/// <summary>
	/// Used whilst building json structures.
	/// </summary>
	private object structureLock = new object();

	/// <summary>
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public async ValueTask<JsonStructure<T,ID>> GetTypedJsonStructure(Context ctx)
	{
		var roleId = ctx.RoleId;

		var size = _jsonStructures == null ? 0 : _jsonStructures.Length;

		if(size < roleId)
		{
			lock (structureLock)
			{
				// Check again, just in case a thread we were waiting for has already done what we need.
				if (size < roleId)
				{
					if (_jsonStructures == null)
					{
						_jsonStructures = new JsonStructure<T, ID>[roleId];
					}
					else if (roleId > _jsonStructures.Length)
					{
						Array.Resize(ref _jsonStructures, (int)roleId);
					}
				}
			}
		}

		var index = roleId - 1;
		var structure = _jsonStructures[index];
		
		if(structure == null)
		{
			// Not built yet. Build it now:
			var role = await Services.Get<RoleService>().Get(new Context(), roleId, DataOptions.IgnorePermissions);
			structure = new JsonStructure<T,ID>(role);
			structure.Service = this;
			await structure.Build(GetContentFields(), EventGroup.BeforeSettable, EventGroup.BeforeGettable);

			// Note that multiple threads can build the structure simultaneously because we apply the created structure set afterwards.
			// It has no other side effects though so its a non-issue if it happens.
			
			lock (structureLock)
			{
				// In the event that multiple threads have been making it at the same time, this check 
				// just ensures we're not using multiple different structures and are just using one of them.
				var existing = _jsonStructures[index];
				if (existing == null)
				{
					_jsonStructures[index] = structure;
				}
				else
				{
					structure = existing;
				}
			}
		}
		
		return structure;
	}

	/// <summary>
	/// Deletes an entity by its ID.
	/// </summary>
	/// <returns></returns>
	public virtual async ValueTask<bool> Delete(Context context, ID id, DataOptions options = DataOptions.Default)
	{
		var result = await Get(context, id, options);
		return await Delete(context, result, options);
	}

	/// <summary>
	/// Deletes an entity.
	/// </summary>
	/// <returns></returns>
	public virtual async ValueTask<bool> Delete(Context context, T result, DataOptions options = DataOptions.Default)
	{
		// Ignoring the permissions only needs to occur on Before.
		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = (options & DataOptions.PermissionsFlag) != DataOptions.PermissionsFlag;
		result = await EventGroup.BeforeDelete.Dispatch(context, result);
		context.IgnorePermissions = previousPermState;

		if (result == null)
		{
			return false;
		}

		await EventGroup.Delete.Dispatch(context, result);

		result = await EventGroup.AfterDelete.Dispatch(context, result);

		// Ok!
		return result != null;
	}

	/// <summary>
	/// List a set of values from this service which are present in a mapping of the given target type.
	/// This is backwards from the typical mapping flow - i.e. you're getting the list of sources with a given single target value.
	/// </summary>
	/// <typeparam name="MAP_TARGET"></typeparam>
	/// <typeparam name="T_ID"></typeparam>
	/// <param name="context"></param>
	/// <param name="targetId"></param>
	/// <param name="mappingName"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public async ValueTask<List<T>> ListByTarget<MAP_TARGET, T_ID>(Context context, T_ID targetId, string mappingName, DataOptions options = DataOptions.Default)
		where T_ID : struct, IEquatable<T_ID>, IConvertible, IComparable<T_ID>
		where MAP_TARGET : Content<T_ID>, new()
	{
		return await Where(mappingName + " contains ?", options).Bind(targetId).ListAll(context);
	}
	
	/// <summary>
	/// List a set of values from this service which are present in a mapping of the given target type.
	/// This is backwards from the typical mapping flow - i.e. you're getting the list of sources with a given single target value.
	/// </summary>
	/// <typeparam name="MAP_TARGET"></typeparam>
	/// <typeparam name="T_ID"></typeparam>
	/// <param name="context"></param>
	/// <param name="targetId"></param>
	/// <param name="mappingName"></param>
	/// <param name="onResult"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public async ValueTask ListByTarget<MAP_TARGET, T_ID>(Context context, T_ID targetId, string mappingName, Func<Context, T, int, object, object, ValueTask> onResult, object a, object b, DataOptions options = DataOptions.Default)
	where T_ID: struct, IEquatable<T_ID>, IConvertible, IComparable<T_ID>
	where MAP_TARGET: Content<T_ID>, new()
	{
		await Where(mappingName + " contains ?", options).Bind(targetId).ListAll(context, onResult, a, b);
	}
	
	/// <summary>
	/// List a set of values from this service which are present in a mapping of the given target type.
	/// </summary>
	/// <typeparam name="S_ID"></typeparam>
	/// <param name="context"></param>
	/// <param name="src"></param>
	/// <param name="mappingName"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public async ValueTask<List<T>> ListBySource<S_ID>(Context context, Content<S_ID> src, string mappingName, DataOptions options = DataOptions.Default)
		where S_ID : struct, IEquatable<S_ID>, IConvertible, IComparable<S_ID>
	{
		var set = new List<T>();

		await ListBySource(context, src, mappingName, (Context c, T obj, int index, object a, object b) => {

			var passedSet = (List<T>)a;
			passedSet.Add(obj);
			return new ValueTask();

		}, set, null, options);

		return set;
	}

	/// <summary>
	/// List a set of values from this service which are present in a mapping of the given target type.
	/// </summary>
	/// <typeparam name="S_ID"></typeparam>
	/// <param name="context"></param>
	/// <param name="src"></param>
	/// <param name="mappingName"></param>
	/// <param name="onResult"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public async ValueTask ListBySource<S_ID>(Context context, Content<S_ID> src, string mappingName, 
		Func<Context, T, int, object, object, ValueTask> onResult, object a, object b, DataOptions options = DataOptions.Default)
		where S_ID : struct, IEquatable<S_ID>, IConvertible, IComparable<S_ID>
	{
		if (src == null)
		{
			// Nothing to do.
			return;
		}

		var ids = src.Mappings.Get(mappingName);

		if (ids == null)
		{
			return;
		}

		await Where("Id=[?]", options)
			.Bind(ids)
			.ListAll(context, onResult, a, b);
	}

	private ConcurrentDictionary<string, FilterMeta<T,ID>> _filterSets = new ConcurrentDictionary<string, FilterMeta<T,ID>>();

	/// <summary>
	/// Gets a fast filter for the given query text.
	/// </summary>
	/// <param name="query"></param>
	/// <param name="canContainConstants"></param>
	/// <returns></returns>
	public override FilterBase GetGeneralFilterFor(string query, bool canContainConstants = false)
	{
		return GetFilterFor(query, DataOptions.Default, canContainConstants);
	}

	/// <summary>
	/// Loads a filter from the given newtonsoft representation. You must .Release() this filter when you're done with it.
	/// </summary>
	/// <param name="newtonsoft"></param>
	/// <returns></returns>
	public override FilterBase LoadFilter(JObject newtonsoft)
	{
		string str;

		if (newtonsoft == null)
		{
			str = "";
		}
		else
		{
			var query = newtonsoft["query"];
			str = query == null ? "" : query.Value<string>();
		}

		// Get the filter base:
		var filter = GetFilterFor(str, DataOptions.Default, false);

		if (newtonsoft == null)
		{
			return filter;
		}

		var argTypes = filter.GetArgTypes();

		if (argTypes != null && argTypes.Count > 0)
		{
			var argSet = newtonsoft["args"] as JArray;
			if (argSet == null)
			{
				throw new PublicException(
					"Your filter has arguments (?) in it, but no args were given, or the args were not an array. Please provide an array of args.",
					"filter_invalid"
				);
			}

			if (argSet.Count != argTypes.Count)
			{
				throw new PublicException(
					"Not enough arguments were given. Your filter has " + argTypes.Count + " but the given args array only has " + argSet.Count,
					"filter_invalid"
				);
			}

			for (var i = 0; i < argSet.Count; i++)
			{
				filter.BindJToken(argSet[i]);
			}

		}

		// Handle universal pagination:
		var pageSizeJToken = newtonsoft["pageSize"];
		int? pageSize = null;

		if (pageSizeJToken != null && pageSizeJToken.Type == JTokenType.Integer)
		{
			pageSize = pageSizeJToken.Value<int>();
		}

		var pageIndexJToken = newtonsoft["pageIndex"];
		int? pageIndex = null;

		if (pageIndexJToken != null && pageSizeJToken.Type == JTokenType.Integer)
		{
			pageIndex = pageIndexJToken.Value<int>();
		}

		var includeTotalJToken = newtonsoft["includeTotal"];
		bool? includeTotal = null;

		if (includeTotalJToken != null && includeTotalJToken.Type == JTokenType.Boolean)
		{
			includeTotal = includeTotalJToken.Value<bool>();
		}

		if (includeTotal.HasValue)
		{
			filter.IncludeTotal = includeTotal.Value;
		}

		if (pageSize.HasValue)
		{
			filter.SetPage(pageIndex.HasValue ? pageIndex.Value : 0, pageSize.Value);
			filter.IncludeTotal = true;
		}
		else if (pageIndex.HasValue)
		{
			// Default page size used
			filter.SetPage(pageIndex.Value);
			filter.IncludeTotal = true;
		}

		var sort = newtonsoft["sort"] as JObject;
		if (sort != null)
		{
			if (sort["field"] != null)
			{
				string field = sort["field"].ToString();

				if (sort["direction"] != null && sort["direction"].ToString() == "desc")
				{
					filter.Sort(field, false);
				}
				else
				{
					filter.Sort(field);
				}
			}
		}

		return filter;
	}

	/// <summary>
	/// Loads a filter from the given newtonsoft representation. You must .Release() this filter when you're done with it.
	/// </summary>
	/// <param name="filterConfig"></param>
	/// <returns></returns>
	public override FilterBase LoadFilter(ListFilter filterConfig)
	{
		string str = (filterConfig == null || filterConfig.Query == null) ? "" : filterConfig.Query;

		// Get the filter base:
		var filter = GetFilterFor(str, DataOptions.Default, false);

		if (filterConfig == null)
		{
			return filter;
		}

		var argTypes = filter.GetArgTypes();

		if (argTypes != null && argTypes.Count > 0)
		{
			var argSet = filterConfig.Args;
			if (argSet == null)
			{
				throw new PublicException(
					"Your filter has arguments (?) in it, but no args were given, or the args were not an array. Please provide an array of args.",
					"filter_invalid"
				);
			}

			if (argSet.Count != argTypes.Count)
			{
				throw new PublicException(
					"Not enough arguments were given. Your filter has " + argTypes.Count + " but the given args array only has " + argSet.Count,
					"filter_invalid"
				);
			}

			for (var i = 0; i < argSet.Count; i++)
			{
				var arg = argSet[i];
				filter.Bind(arg);
			}
		}

		if (filterConfig.IncludeTotal.HasValue)
		{
			filter.IncludeTotal = filterConfig.IncludeTotal.Value;
		}

		// Handle universal pagination:
		var pageSize = filterConfig.PageSize;
		var pageIndex = filterConfig.PageIndex;

		if (pageSize != 0)
		{
			filter.SetPage(pageIndex, pageSize);
			filter.IncludeTotal = true;
		}
		else if (pageIndex != 0)
		{
			// Default page size used
			filter.SetPage(pageIndex);
			filter.IncludeTotal = true;
		}

		if (filterConfig.Sort.HasValue)
		{
			var sort = filterConfig.Sort.Value;
			var field = sort.Field;
			var dir = sort.Direction;
			
			if (field != null)
			{
				if (dir != null && dir == "desc")
				{
					filter.Sort(field, false);
				}
				else
				{
					filter.Sort(field, true);
				}
			}
		}

		return filter;
	}

	/// <summary>
	/// Gets a fast filter for the given query text. 
	/// You should ensure the query text is constant and that you use binded args on the filter instead of baking values into a string.
	/// </summary>
	/// <param name="query"></param>
	/// <param name="opts"></param>
	/// <param name="canContainConstants"></param>
	/// <returns></returns>
	public Filter<T,ID> GetFilterFor(string query, DataOptions opts = DataOptions.Default, bool canContainConstants = false)
	{
		if (query == null)
		{
			query = string.Empty;
		}

		if (!_filterSets.TryGetValue(query, out FilterMeta<T,ID> meta))
		{
			if (_filterSets.Count >= 10000)
			{
                Log.Warn(LogTag, "Clearing large filter cache. If this happens frequently it's a symptom of poorly designed filters.");
				_filterSets.Clear();
			}

			meta = new FilterMeta<T,ID>(this, query, canContainConstants);
			meta.Construct();
			_filterSets[query] = meta;
		}

		// Get from pool:
		var filt = meta.GetPooled();
		filt.DataOptions = opts;
		return filt;
	}

	/// <summary>
	/// Non-allocating where selection of objects from this service. On the returned object, use e.g. .List()
	/// </summary>
	/// <returns></returns>
	public Filter<T, ID> Where(DataOptions opts = DataOptions.Default)
	{
		// Get the filter for the user query:
		return GetFilterFor("", opts);
	}

	/// <summary>
	/// Non-allocating where selection of objects from this service. On the returned object, use e.g. List()
	/// You should ensure the query text is constant and that you use binded args on the filter instead of baking values into a string.
	/// </summary>
	/// <param name="query"></param>
	/// <param name="opts"></param>
	/// <returns></returns>
	public Filter<T,ID> Where(string query, DataOptions opts = DataOptions.Default)
	{
		// Get the filter for the user query:
		return GetFilterFor(query, opts);
	}

	/// <summary>
	/// A filter which is simply empty (the filter for "")
	/// </summary>
	private Filter<T, ID> _emptyFilter;

	/// <summary>
	/// A filter which is simply empty (the filter for "")
	/// </summary>
	public Filter<T, ID> EmptyFilter {
		get {
			if (_emptyFilter == null)
			{
				_emptyFilter = GetFilterFor("");
			}

			return _emptyFilter;
		}
	}

	/// <summary>
	/// Starts streaming results for the given filter. 
	/// Usually use Where and then one if its convenience functions instead.
	/// </summary>
	/// <param name="filter"></param>
	/// <param name="release">Release the filter afterwards.</param>
	/// <returns>Total, if filter.IncludeTotal is set. Otherwise its meaning is undefined.</returns>
	public ContentStream<T, ID> GetResults(Filter<T, ID> filter, bool release = true)
	{
		var res = new ContentStream<T, ID>() {
			Source = this,
			ServiceForType = this,
			Filter = filter,
			ReleaseFilter = release
		};

		return res;
	}

	/// <summary>
	/// If this content stream source has more than one additional source, this gets the next one.
	/// </summary>
	/// <returns></returns>
	public SecondaryContentStreamSource GetNextSource()
	{
		return null;
	}

	/// <summary>
	/// Starts cycling results for the given filter with the given callback function. Usually use Where and then one if its convenience functions instead.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filter"></param>
	/// <param name="onResult"></param>
	/// <param name="srcA"></param>
	/// <param name="srcB"></param>
	/// <returns>Total, if filter.IncludeTotal is set. Otherwise its meaning is undefined.</returns>
	public async ValueTask<int> GetResults(Context context, Filter<T, ID> filter, Func<Context, T, int, object, object, ValueTask> onResult, object srcA, object srcB)
	{
		// Filter is not optional. As it holds state for a particular run, it also cannot be an instance shared with other runs.
		// Note that the above is not true for QueryB filters (see below) as they are stateless by design in order to maximise rapid reuse.

		if (!filter.FullyBound())
		{
			// Safety check - filters come from a pool, so if a filter has not been fully 
			// bound then it is likely to have other data from a previous request in it.
			throw new PublicException(
				"This filter has " + filter.Pool.ArgTypes.Count + " args but not all were bound. Make sure you .Bind() all args.",
				"filter_invalid"
			);
		}
		
		// struct:
		var queryPair = new QueryPair<T, ID>()
		{
			QueryA = filter,
			SrcA = srcA,
			SrcB = srcB,
			OnResult = async (Context resultCtx, T result, int index, object resultSrc, object resultSrcB) => {
				result = await this.EventGroup.ListEntry.Dispatch(resultCtx, result);
				await onResult(resultCtx, result, index, resultSrc, resultSrcB);
			}
			// QueryB is set by perm system.
		};

		// Ignoring the permissions only needs to occur on Before.
		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = (filter.DataOptions & DataOptions.PermissionsFlag) != DataOptions.PermissionsFlag;
		queryPair = await EventGroup.BeforeList.Dispatch(context, queryPair);
		context.IgnorePermissions = previousPermState;

		if (queryPair.QueryB == null)
		{
			queryPair.QueryB = EmptyFilter;
		}

		queryPair = await EventGroup.List.Dispatch(context, queryPair);
		var total = queryPair.Total;

		return total;
	}

	/// <summary>
	/// Gets an object from this service. Generally use Get instead with a fixed type.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public override async ValueTask<object> GetObject(Context context, ulong id, DataOptions options = DataOptions.Default)
	{
		var convertedId = _idConverter.Convert(id);
		return await Get(context, convertedId, options);
	}

	/// <summary>
	/// Gets a single entity by its ID.
	/// </summary>
	public virtual async ValueTask<T> Get(Context context, ID id, DataOptions options = DataOptions.Default)
	{
		// Note: Get is unique in that its permission check happens in the After event handler.
		id = await EventGroup.BeforeLoad.Dispatch(context, id);
		
		T item = null;
		item = await EventGroup.Load.Dispatch(context, item, id);

		// Ignoring the permissions only needs to occur on *After* for Gets.
		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = (options & DataOptions.PermissionsFlag) != DataOptions.PermissionsFlag;
		item = await EventGroup.AfterLoad.Dispatch(context, item);
		context.IgnorePermissions = previousPermState;
		
		return item;
	}

	/// <summary>
	/// Sets the fields from the given JSON object on the given target object, based on the user role in the context.
	/// Note that there's 2 sets of fields - a primary set, then also a secondary set which are set only after the ID of the object is known.
	/// E.g. during create, the object is instanced, initial fields are set, it's then actually created, and then the after ID set is run.
	/// </summary>
	/// <param name="target"></param>
	/// <param name="context"></param>
	/// <param name="body"></param>
	public async ValueTask SetFieldsOnObject(T target, Context context, JObject body)
	{
		// Get the JSON meta which will indicate exactly which fields are editable by this user (role):
		var availableFields = await GetTypedJsonStructure(context);

		foreach (var property in body.Properties())
		{
			if (property.Name == "on")
			{
				continue;
			}

			// Attempt to get the available field:
			var field = availableFields.GetField(property.Name);

			if (field == null)
			{
				continue;
			}

			var writeRule = field.GetWriteAccessRule();

			if (writeRule.IsGranted(context, target))
			{
				// Try setting the value now:
				await field.SetFieldValue(context, target, property.Value);
			}
		}
	}

	/// <summary>
	/// Bulk creates more than one entity. The set that you provide may be modified inline.
	/// </summary>
	public virtual async ValueTask<List<T>> CreateAll(Context context, List<T> set, DataOptions options = DataOptions.Default)
	{
		if (set == null || set.Count == 0)
		{
			return new List<T>();
		}
		
		for(var i=0;i<set.Count;i++)
		{
			var previousPermState = context.IgnorePermissions;
			context.IgnorePermissions = (options & DataOptions.PermissionsFlag) != DataOptions.PermissionsFlag;
			set[i] = await EventGroup.BeforeCreate.Dispatch(context, set[i]);
			context.IgnorePermissions = previousPermState;
		}

		set = await EventGroup.CreateAll.Dispatch(context, set);

		for (var i = 0; i < set.Count; i++)
		{
			// Handles cache updates and after create event calls.
			// These are almost always things which complete instantly
			// so we don't get O(N) waiting behaviour.
			set[i] = await CreatePartialComplete(context, set[i]);
		}

		return set;
	}

	/// <summary>
	/// Creates a new entity.
	/// </summary>
	public virtual async ValueTask<T> Create(Context context, T entity, DataOptions options = DataOptions.Default)
	{
		entity = await CreatePartial(context, entity, options);
		return await CreatePartialComplete(context, entity);
	}

	/// <summary>
	/// Creates a new entity but without calling AfterCreate. This allows you to update fields after the ID has been set, but before AfterCreate is called.
	/// You must always call CreatePartialComplete afterwards to trigger the AfterCreate calls.
	/// </summary>
	public virtual async ValueTask<T> CreatePartial(Context context, T entity, DataOptions options)
	{
		// Ignoring the permissions only needs to occur on Before.
		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = (options & DataOptions.PermissionsFlag) != DataOptions.PermissionsFlag;
		entity = await EventGroup.BeforeCreate.Dispatch(context, entity);
		context.IgnorePermissions = previousPermState;

		// Note: The Id field is automatically updated by Run here.
		if (entity == null)
		{
			return entity;
		}

		entity = await EventGroup.Create.Dispatch(context, entity);

		return entity;
	}

	/// <summary>
	/// Returns the EventGroup[T] for this AutoService, or null if it is an autoService without an EventGroup.
	/// </summary>
	/// <returns></returns>
	public override EventGroup GetEventGroup()
	{
		return EventGroup;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="context"></param>
	/// <param name="raw"></param>
	/// <returns></returns>
	public virtual async ValueTask<T> CreatePartialComplete(Context context, T raw)
	{
		raw = await EventGroup.CreatePartial.Dispatch(context, raw);
		raw = await EventGroup.AfterCreate.Dispatch(context, raw);
		return raw;
	}

	/// <summary>
	/// Converts the given ulong ID to one this autoservice can use.
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	public ID ConvertId(ulong input)
	{
		return _idConverter.Convert(input);
	}

	/// <summary>
	/// Converts the given ID for this service into a ulong.
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	public ulong ReverseId(ID input)
	{
		return _idConverter.Reverse(input);
	}

	/// <summary>
	/// Performs an update on the given entity. If updating the object is permitted, the callback is executed. 
	/// You must only set fields on the object in that callback, or in a BeforeUpdate handle.
	/// </summary>
	public virtual async ValueTask<T> Update(Context context, ID id, Action<Context, T, T> cb, DataOptions options = DataOptions.Default)
	{
		var entity = await Get(context, id, options);
		
		if (entity == null)
		{
			return null;
		}
		
		return await Update(context, entity, cb, options);
	}

	/// <summary>
	/// The field set used by Diff.
	/// </summary>
	private FieldMap _diffFields;

	/// <summary>
	/// Used by Diff.
	/// </summary>
	private Func<T, T, ChangedFields> _diffDelegate;

	/// <summary>
	/// Diffs the given objects returning information about fields which have changed. Does not allocate.
	/// </summary>
	/// <param name="updated"></param>
	/// <param name="original"></param>
	public ChangedFields Diff(T updated, T original)
	{
		if (_diffDelegate == null)
		{
			var dymMethod = new DynamicMethod("Diff", typeof(ChangedFields), new Type[] { typeof(T), typeof(T) }, true);
			var generator = dymMethod.GetILGenerator();

			// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
			// Note that these maps aren't shared between queries so the fields can be removed etc from them.
			var flds = new FieldMap(InstanceType, EntityName);

			// Remove "Id" field as it's not permitted to be marked as changed:
			flds.Remove("Id");

			_diffFields = flds;

			if (flds.Count > 64)
			{
				// This project has ignored the 50+ error for too long and it has become more severe.
				// If you encounter this situation and having this many fields is required,
				// ChangedFields needs to instead allocate a ulong array for larger types and a ulong (unchanged) for everything else.
				throw new Exception("Too many fields");
			}
			else if (flds.Count >= 50)
			{
				Log.Warn(LogTag, EntityName + " has an unusually large amount of fields (" + flds.Count + "). 64 is the current limit.");
			}

			var bitField = generator.DeclareLocal(typeof(ulong));
			//var cfField = generator.DeclareLocal(typeof(ChangedFields));

			generator.Emit(OpCodes.Ldc_I4_0);
			generator.Emit(OpCodes.Conv_U8);
			generator.Emit(OpCodes.Stloc, bitField);

			for (var i=0;i<flds.Count;i++)
			{
				var field = flds[i];
				var type = field.Type;
				var nullableBase = Nullable.GetUnderlyingType(type);
				var endLabel = generator.DefineLabel();

				var fieldValue = ((ulong)1) << i;
				var mainType = type;

				if (nullableBase != null)
				{
					mainType = nullableBase;
					var hasValueMethod = type.GetProperty("HasValue").GetGetMethod();
					var getValueMethod = type.GetProperty("Value").GetGetMethod();

					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Ldflda, field.TargetField);
					generator.Emit(OpCodes.Callvirt, hasValueMethod);
					generator.Emit(OpCodes.Dup);

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldflda, field.TargetField);
					generator.Emit(OpCodes.Callvirt, hasValueMethod);

					// Are they the same?
					generator.Emit(OpCodes.Ceq);

					Label sameType = generator.DefineLabel();

					generator.Emit(OpCodes.Brtrue, sameType); // They both have a value or are both null.
					generator.Emit(OpCodes.Pop); // Cancel out the dup earlier
					generator.Emit(OpCodes.Ldloc, bitField);
					generator.Emit(OpCodes.Ldc_I8, (long)fieldValue);
					generator.Emit(OpCodes.Conv_U8);
					generator.Emit(OpCodes.Or);
					generator.Emit(OpCodes.Stloc, bitField);
					generator.Emit(OpCodes.Br, endLabel);

					generator.MarkLabel(sameType);

					// If they're both null, go to end.
					generator.Emit(OpCodes.Brfalse, endLabel);

					// Otherwise, check if their values match. Load the values now.
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Ldflda, field.TargetField);
					generator.Emit(OpCodes.Callvirt, getValueMethod);

					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldflda, field.TargetField);
					generator.Emit(OpCodes.Callvirt, getValueMethod);

				}
				else
				{
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Ldfld, field.TargetField);
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, field.TargetField);
				}
				
				if (mainType == typeof(DateTime) || mainType == typeof(string) || 
					mainType == typeof(decimal) || mainType == typeof(MappingData) || mainType == typeof(JsonString) ||
					(mainType.IsGenericType && mainType.GetGenericTypeDefinition() == typeof(Localized<>)))
				{
					var eq = mainType.GetMethod("Equals", new Type[] { mainType, mainType });
					generator.Emit(OpCodes.Call, eq);
				}
				else
				{
					generator.Emit(OpCodes.Ceq);
				}

				// if(false){ bitField |= fieldValue };

				generator.Emit(OpCodes.Brtrue, endLabel);
				generator.Emit(OpCodes.Ldloc, bitField);
				generator.Emit(OpCodes.Ldc_I8, (long)fieldValue);
				generator.Emit(OpCodes.Conv_U8);
				generator.Emit(OpCodes.Or);
				generator.Emit(OpCodes.Stloc, bitField);
				generator.MarkLabel(endLabel);
			}

			// Return the new ChangedFields (struct).
			var ctor = typeof(ChangedFields).GetConstructor(
				BindingFlags.Public | BindingFlags.Instance,
				new Type[] { typeof(ulong) }
			);

			generator.Emit(OpCodes.Ldloc, bitField);
			generator.Emit(OpCodes.Newobj, ctor);
			generator.Emit(OpCodes.Ret);

			_diffDelegate = dymMethod.CreateDelegate<Func<T, T, ChangedFields>>();
		}

		var chgFields = _diffDelegate(updated, original);
		chgFields.Fields = _diffFields;
		return chgFields;
	}

	/// <summary>
	/// Used by CloneEntityInto.
	/// </summary>
	private Action<T, T> _cloneDelegate;

	/// <summary>
	/// Clones the fields of the given source object into the given target object. Does not allocate.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="target"></param>
	public void CloneEntityInto(T source, T target)
	{
		if (_cloneDelegate == null)
		{
			var dymMethod = new DynamicMethod("CloneEntityInto", typeof(void), new Type[] { typeof(T), typeof(T) }, true);
			var generator = dymMethod.GetILGenerator();

			foreach (var field in InstanceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (field.FieldType == typeof(MappingData))
				{
					// A deep clone is required to avoid references to the same dictionary and list objects.
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldflda, field);
					generator.Emit(OpCodes.Callvirt, typeof(MappingData).GetMethod(nameof(MappingData.Clone)));
					generator.Emit(OpCodes.Stfld, field);
				}
				else
				{
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, field);
					generator.Emit(OpCodes.Stfld, field);
				}
			}

			// Return
			generator.Emit(OpCodes.Ret);

			_cloneDelegate = dymMethod.CreateDelegate<Action<T, T>>();
		}

		_cloneDelegate(source, target);
	}

	/// <summary>
	/// Performs an update on the given entity. If updating the object is permitted, the callback is executed. 
	/// You must only set fields on the object in that callback, or in a BeforeUpdate handle.
	/// </summary>
	public virtual async ValueTask<T> Update(Context context, T cachedEntity, Action<Context, T, T> cb, DataOptions options = DataOptions.Default)
	{
		var entityToUpdate = StartUpdate(context, cachedEntity, options);

		if (entityToUpdate == null)
		{
			// Note it would've thrown if there was a permission issue.
			return null;
		}

		if (cb == null)
		{
			throw new ArgumentNullException("An update callback is required. Inside this callback is the only place where you can safely set field values, aside from BeforeUpdate event handlers.");
		}

		T ce = cachedEntity;

		if (CacheAvailable)
		{
			// cachedEntity did actually come from the cache.
			// To avoid invalid diffs if the cache object is changed by some other thread we need to clone it too.
			// Like StartUpdate, this object should ultimately come from and return to a pool.
			ce = (T)Activator.CreateInstance(InstanceType);
			CloneEntityInto(entityToUpdate, ce);
		}

		// Set fields now:
		cb(context, entityToUpdate, ce);

		// And perform the save:
		return await FinishUpdate(context, entityToUpdate, ce, options);
	}
	
	/// <summary>
	/// Performs an update on the given entity. If updating the object is permitted, the callback is executed and awaited. 
	/// You must only set fields on the object in that callback, or in a BeforeUpdate handle.
	/// </summary>
	public virtual async ValueTask<T> Update(Context context, T cachedEntity, Func<Context, T, T, ValueTask> cb, DataOptions options = DataOptions.Default)
	{
		var entityToUpdate = StartUpdate(context, cachedEntity, options);

		if (entityToUpdate == null)
		{
			// Note it would've thrown if there was a permission issue.
			return null;
		}

		if (cb == null)
		{
			throw new ArgumentNullException("An update callback is required. Inside this callback is the only place where you can safely set field values, aside from BeforeUpdate event handlers.");
		}

		// Set fields now:
		await cb(context, entityToUpdate, cachedEntity);

		// And perform the save:
		return await FinishUpdate(context, entityToUpdate, cachedEntity, options);
	}

	/// <summary>
	/// Performs an update on the given entity, setting it to the specified object.
	/// </summary>
	public virtual async ValueTask<T> UpdateExact(Context context, T entityToUpdate, DataOptions options = DataOptions.Default)
	{
		if ((options & DataOptions.PermissionsFlag) == DataOptions.PermissionsFlag)
		{
			// Perform the permission test now:
			EventGroup.BeforeUpdate.TestCapability(context, entityToUpdate);
		}

		var originalEntity = await Get(context, entityToUpdate.Id, options);

		if (originalEntity == null)
		{
			return null;
		}

		await FinishUpdate(context, entityToUpdate, originalEntity, options);
		return entityToUpdate;
	}

	/// <summary>
	/// For simpler usage, see Update. This is for advanced non-allocating updates. Returns the object that you MUST apply your changes to.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="entity"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public T StartUpdate(Context context, T entity, DataOptions options = DataOptions.Default)
	{
		if ((options & DataOptions.PermissionsFlag) == DataOptions.PermissionsFlag)
		{
			// Perform the permission test now:
			EventGroup.BeforeUpdate.TestCapability(context, entity);
		}

		// Minor todo: get this object from a pool for high velocity updates.
		var t = (T)Activator.CreateInstance(InstanceType);
		CloneEntityInto(entity, t);
		return t;
	}

	/// <summary>
	/// Used together with StartUpdate in the form if(await CanUpdate){ set fields, await DoUpdate }.
	/// This route is used to set fields on the object without an allocation.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="entityToUpdate">The entity returned by StartUpdate.</param>
	/// <param name="originalEntity">The original, unmodified entity.</param>
	/// <param name="options">Data options</param>
	/// <returns></returns>
	public async ValueTask<T> FinishUpdate(Context context, T entityToUpdate, T originalEntity, DataOptions options = DataOptions.Default)
	{
		entityToUpdate = await EventGroup.BeforeUpdate.Dispatch(context, entityToUpdate, originalEntity);
		
		if (entityToUpdate == null)
		{
			return null;
		}

		// Set editedUtc, before the diff is calculated:
		DateTime? prevDate = null;

		if (entityToUpdate is Api.Users.IHaveTimestamps revRow)
		{
			if ((options & DataOptions.CheckNotChanged) == DataOptions.CheckNotChanged)
			{
				prevDate = revRow.GetEditedUtc();
			}

			if (context.PermitEditedUtcChange)
			{
				revRow.SetEditedUtc(DateTime.UtcNow);
			}
		}

		// Calculate the diff:
		var changes = Diff(entityToUpdate, originalEntity);
		changes.PreviousEditedUtc = prevDate;

		entityToUpdate = await EventGroup.Update.Dispatch(context, entityToUpdate, changes, options);

		if (entityToUpdate == null)
		{
			return null;
		}

		// OriginalEntity at this point is likely to have fields matching the original entity.
		// That's because Update internally updates the cache, resulting in the original (often from a cache) object therefore being updated.

		entityToUpdate = await EventGroup.AfterUpdate.Dispatch(context, entityToUpdate, changes);
		return entityToUpdate;
	}
	
}

/// <summary>
/// The base class of all AutoService instances.
/// </summary>
public partial class AutoService
{
	/// <summary>
	/// The type that this AutoService is servicing, if any. E.g. a User, ForumPost etc.
	/// </summary>
	public Type ServicedType;
	/// <summary>
	/// True if this service stores persistent data.
	/// </summary>
	public bool DataIsPersistent;
	/// <summary>
	/// The actual instance type of this service. This always equals ServicedType or inherits it.
	/// </summary>
	public Type InstanceType;
	/// <summary>
	/// The type that this AutoService uses for IDs, if any. Almost always int, but some use ulong.
	/// </summary>
	public Type IdType;
	/// <summary>
	/// Map of the available fields in the services InstanceType.
	/// </summary>
	public FieldMap FieldMap;
	/// <summary>
	/// The name of the instance types of this service.
	/// Usually the same as InstanceType.Name but can be different, such as on mappings.
	/// </summary>
	public string EntityName;

	/// <summary>
	/// A generated tag to use with Log.* methods.
	/// </summary>
	public string LogTag;

	/// <summary>
	/// True if this is a mapping service.
	/// </summary>
	public virtual bool IsMapping
	{
		get
		{
			return false;
		}
	}

	/// <summary>
	/// The source type if this is a mapping service.
	/// </summary>
	public virtual Type MappingSourceType => null;
	
	/// <summary>
	/// The target type if this is a mapping service.
	/// </summary>
	public virtual Type MappingTargetType => null;

	/// <summary>
	/// The source Id type if this is a mapping service.
	/// </summary>
	public virtual Type MappingSourceIdType => null;

	/// <summary>
	/// The target Id type if this is a mapping service.
	/// </summary>
	public virtual Type MappingTargetIdType => null;

	/// <summary>
	/// Updates the instance type, clearing any internal caches which used it before.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="instanceType"></param>
	public virtual ValueTask SetInstanceType(Context context, Type instanceType)
	{
		InstanceType = instanceType == null ? ServicedType : instanceType;
		_contentFields = null;
		FieldMap = new FieldMap(InstanceType, EntityName);
		return new ValueTask();
	}

	/// <summary>
	/// Outputs a list of things from this service as JSON into the given writer.
	/// Executes the given collector(s) whilst it happens, which can also be null.
	/// Does not perform permission checks internally.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="collectors"></param>
	/// <param name="idSet"></param>
	/// <param name="writer"></param>
	/// <param name="flags">Additional serialiser flags which can be used to control things like field visibility in protected contexts. 
	/// For example IsIncluded is true if it's via an include, and therefore the "from" filter field is implied true.</param>
	/// <param name="functionalIncludes">Optional set of functional includes to execute on each node as the json is rendered.</param>
	/// <returns></returns>
	public virtual ValueTask OutputJsonList(Context context, IDCollector collectors, IDCollector idSet, Writer writer, ContextFlags flags, FunctionalInclusionNode[] functionalIncludes = null)
	{
		// Not supported on this service.
		return new ValueTask();
	}

	/// <summary>
	///  Creates a mapping from the given src to the given target. Only available on mapping services. 
	///  It's more ideal to use the type specific overloads whenever possible (particularly as they're available on regular services, rather than this mapping service specific one).
	///  See also: CreateMappingIfNotExists, EnsureMapping
	/// </summary>
	/// <param name="context"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="opts"></param>
	/// <returns></returns>
	public virtual ValueTask<bool> CreateMapping(Context context, object a, object b, DataOptions opts = DataOptions.Default)
	{
		throw new NotImplementedException("Only available on a mapping service.");
	}

	/// <summary>
	/// Outputs the given object (an entity from this service) to JSON in the given writer.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="entity"></param>
	/// <param name="writer"></param>
	/// <param name="targetStream"></param>
	/// <param name="includes"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public virtual ValueTask ObjectToJson(Context context, object entity, Writer writer, Stream targetStream = null, string includes = null, ContextFlags flags = ContextFlags.None)
	{
		// Not supported on this service.
		return new ValueTask();
	}
	
	/// <summary>
	/// Outputs the given object (an entity from this service) to JSON in the given writer.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="entity"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public virtual ValueTask ObjectToTypeAndIdJson(Context context, object entity, Writer writer)
	{
		// Not supported on this service.
		return new ValueTask();
	}

	/// <summary>
	/// Gets a fast filter for the given query text.
	/// </summary>
	/// <param name="query"></param>
	/// <param name="canContainConstants"></param>
	/// <returns></returns>
	public virtual FilterBase GetGeneralFilterFor(string query, bool canContainConstants = false)
	{
		return null;
	}

	/// <summary>
	/// Loads a filter from the given newtonsoft representation. You must .Release() this filter when you're done with it.
	/// </summary>
	/// <param name="newtonsoft"></param>
	/// <returns></returns>
	public virtual FilterBase LoadFilter(JObject newtonsoft)
	{
		return null;
	}

	/// <summary>
	/// Loads a filter from the given filterConfig. You must .Release() this filter when you're done with it.
	/// </summary>
	/// <param name="filterConfig"></param>
	/// <returns></returns>
	public virtual FilterBase LoadFilter(ListFilter filterConfig)
	{
		return null;
	}

	/// <summary>
	/// Outputs a single object from this service as JSON into the given writer. Acts like include * was specified by default.
	/// Executes the given collector(s) whilst it happens, which can also be null.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="writer"></param>
	/// <param name="dataOptions"></param>
	/// <param name="includes"></param>
	/// <returns></returns>
	public virtual ValueTask OutputById(Context context, ulong id, Writer writer, DataOptions dataOptions = DataOptions.Default, string includes = "*")
	{
		// Not supported on this service.
		return new ValueTask();
	}

	/// <summary>
	/// Returns the EventGroup[T] for this AutoService, or null if it is an autoService without an EventGroup.
	/// </summary>
	/// <returns></returns>
	public virtual EventGroup GetEventGroup()
	{
		return null;
	}

	/// <summary>
	/// Sets up the cache if this service needs one.
	/// </summary>
	/// <returns></returns>
	public virtual ValueTask SetupCacheIfNeeded()
	{
		// Not all services are AutoService<T>.
		return new ValueTask();
	}

	/// <summary>
	/// Creates a new AutoService.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="idType"></param>
	/// <param name="instanceType"></param>
	/// <param name="entityName"></param>
	public AutoService(Type type = null, Type idType = null, Type instanceType = null, string entityName = null)
	{
		ServicedType = type;
		InstanceType = instanceType == null ? type : instanceType;
		IdType = idType;
		DataIsPersistent = type != null && ContentTypes.IsPersistentType(type);
		EntityName = entityName == null ? (InstanceType == null ? null : InstanceType.Name) : entityName;
		LogTag = EntityName == null ? GetType().Name.ToLower() : EntityName.ToLower();

		if (InstanceType != null)
		{
			FieldMap = new FieldMap(InstanceType, EntityName);
		}
	}

	/// <summary>
	/// Reads a particular metadata field by its name. Common ones are "title" and "description".
	/// Use this to generically read common descriptive things about a given content type.
	/// Note that as fields vary by role, it is possible for users of different roles to obtain different meta values.
	/// </summary>
	public async ValueTask<object> GetMetaFieldValue(Context context, string fieldName, object content)
	{
		// Get the json structure:
		var json = await GetJsonStructure(context);

		var field = json.GetMetaField(fieldName);

		if (field == null)
		{
			return null;
		}

		if (field.PropertyGet != null)
		{
			// It's a property:
			return field.PropertyGet.Invoke(content, null);
		}

		return field.FieldInfo.GetValue(content);
	}

	/// <summary>
	/// Reads a particular metadata field by its name. Common ones are "title" and "description".
	/// Use this to generically read common descriptive things about a given content type.
	/// Note that as fields vary by role, it is possible for users of different roles to obtain different meta values.
	/// </summary>
	public async ValueTask<string> GetMetaString(Context context, string fieldName, object content)
	{
		var baseValue = await GetMetaFieldValue(context, fieldName, content);

		if (baseValue == null)
		{
			return null;
		}

		if (baseValue is string)
		{
			return (string)baseValue;
		}

		if (baseValue is Localized<string>)
		{
			return ((Localized<string>)baseValue).Get(context);
		}

		// Ignores other localized types at the mo.
		return baseValue.ToString();
	}

	/// <summary>
	/// Gets a map which lists the available fields in the content type.
	/// </summary>
	/// <returns></returns>
	public ContentFields GetContentFields()
	{
		var cf = _contentFields;

		if (cf == null)
		{
			cf = new ContentFields(this);
			_contentFields = cf;
		}

		return cf;
	}

	/// <summary>
	/// Sets a custom ContentFields set.
	/// </summary>
	/// <param name="fields"></param>
	/// <returns></returns>
	public void SetContentFields(ContentFields fields)
	{
		_contentFields = fields;
		if (fields != null)
		{
			fields.Service = this;
		}
	}

	/// <summary>
	/// The fields of this type.
	/// </summary>
	protected ContentFields _contentFields;

	/// <summary>
	/// Sets up the cache on this service. If you're not sure, use Cache instead of this.
	/// </summary>
	/// <returns></returns>
	public virtual void Cache(CacheConfig cfg = null)
	{
		throw new NotImplementedException();
	}
	
	/// <summary>
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public virtual ValueTask<JsonStructure> GetJsonStructure(Context ctx)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Gets an object from this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public virtual ValueTask<object> GetObject(Context context, ulong id, DataOptions options = DataOptions.Default)
	{
		return new ValueTask<object>(null);
	}

	/// <summary>
	/// Installs generic admin pages for this service, including the nav menu entry. 
	/// If you need more configuration options such as tabs on your edit/ add page, see the AdminPageOptions overload.
	/// Does nothing if there isn't a page service installed, or if the admin pages already exist.
	/// </summary>
	/// <param name="navMenuLabel">The text to show on the navmenu.</param>
	/// <param name="navMenuIconRef">The ref for the icon to use on the navmenu. Usually a fontawesome icon, of the form "fa:fa-thing".</param>
	/// <param name="fields">The fields to show in the list of your content type. Usually include at least some sort of name or title.</param>
	/// <param name="childAdminPage">
	/// <param name="navMenuParentKey"></param>
	/// A shortcut for specifying that your type has some kind of sub-type.
	/// For example, the NavMenu admin page specifies a child type of NavMenuItem, meaning each NavMenu ends up with a list of NavMenuItems.
	/// Make sure you specify the fields that'll be visible from the child type in the list on the parent type.
	/// For example, if you'd like each child entry to show its Id and Title fields, specify new string[]{"id", "title"}.
	/// </param>
	protected void InstallAdminPages(string navMenuLabel, string navMenuIconRef, string[] fields, AdminPageOptions childAdminPage = null, string navMenuParentKey = null)
	{
		InstallAdminPages(new AdminPageOptions() {
			NavMenuLabel = new Localized<string>(navMenuLabel),
			NavMenuIcon = navMenuIconRef,
			ListFields = fields,
			ChildType = childAdminPage,
			NavMenuParentKey = navMenuParentKey
		});
	}

	/// <summary>
	/// Installs admin pages with more advanced options.
	/// </summary>
	/// <param name="options"></param>
	protected void InstallAdminPages(AdminPageOptions options)
	{
		if (Services.Started)
		{
			InstallAdminPagesInternal(options);
		}
		else
		{
			// Must happen after services start otherwise the page service isn't necessarily available yet.
			Events.Service.AfterStart.AddEventListener((Context ctx, object src) =>
			{
				InstallAdminPagesInternal(options);
				return new ValueTask<object>(src);
			});
		}
	}
	
	private void InstallAdminPagesInternal(AdminPageOptions options)
	{
		var pageService = Api.Startup.Services.Get("PageService");
		var adminNavMenuService = Services.Get("AdminNavMenuItemService");

		if (pageService == null)
		{
			// No point installing nav menu entries either if there's no pages.
			return;
		}

		if (adminNavMenuService is not null)
		{
			var method = adminNavMenuService.GetType().GetMethod("InstallGroups");

			if (method is not null)
			{
				var valueTask = (ValueTask) method.Invoke(adminNavMenuService, []);
				
				valueTask.GetAwaiter().GetResult();
			}
		}
		
		var installPages = pageService.GetType().GetMethod("InstallAdminPagesInt");
			
		if (installPages != null)
		{
			if (options.ContentService == null)
			{
				options.ContentService = this;
			}

			if (options.ListColumns is not null)
			{
				var contentFields = GetContentFields();
				contentFields.MetaFieldMap.TryGetValue("title", out var titleField);

				var titleColumn = (AutoListColumn)null;
				var nameColumn = (AutoListColumn)null;
				var idColumn = (AutoListColumn)null;

				foreach (var item in options.ListColumns)
				{
					if (string.IsNullOrEmpty(item.Label))
					{
						item.Label = AutoFormService.SpaceCamelCase(item.Field);
					}

					if (titleField != null && item.Field.Equals(titleField.Name, StringComparison.OrdinalIgnoreCase))
					{
						item.Title = true;
						titleColumn = item;
					}

					if (item.Field.Equals("name", StringComparison.OrdinalIgnoreCase))
					{
						nameColumn = item;
					}

					if (item.Field.Equals("id", StringComparison.OrdinalIgnoreCase))
					{
						idColumn = item;
					}
				}

				var reorderedColumns = new List<AutoListColumn>();

				var firstColumn = titleColumn ?? nameColumn;
				if (firstColumn != null)
				{
					reorderedColumns.Add(firstColumn);
				}

				foreach (var col in options.ListColumns)
				{
					if (col != firstColumn && col != idColumn)
					{
						reorderedColumns.Add(col);
					}
				}

				if (idColumn != null)
				{
					reorderedColumns.Add(idColumn);
				}

				options.ListColumns = reorderedColumns;
			}

			// InstallAdminPages(string typeName, string[] fields)
			installPages.Invoke(pageService, [
				ServicedType,
				options
			]);
		}
	}
}
