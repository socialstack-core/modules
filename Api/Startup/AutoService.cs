using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

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
	/// Set this flag true to get the raw data from the db.
	/// </summary>
	RawFlag = 4,

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
public partial class AutoService<T, ID> : AutoService 
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
	/// Gets the underlying mapping service from this type to the given type, with the given map name. The map name is the same as the "list as" attribute on the target type.
	/// </summary>
	/// <typeparam name="MAP_TARGET"></typeparam>
	/// <typeparam name="T_ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <returns></returns>
	public async ValueTask<MappingService<T, MAP_TARGET, ID, T_ID>> GetMap<MAP_TARGET, T_ID>(string mappingName)
		where T_ID : struct, IEquatable<T_ID>, IConvertible, IComparable<T_ID>
		where MAP_TARGET : Content<T_ID>, new()
	{
		// Get mapping service:
		var targetSvc = Services.GetByContentType(typeof(MAP_TARGET));

		// Get the mapping type:
		var mappingService = await MappingTypeEngine.GetOrGenerate(this, targetSvc, mappingName) as MappingService<T, MAP_TARGET, ID, T_ID>;

		return mappingService;
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
		var res = new List<T>();
		await ListByTarget<MAP_TARGET, T_ID>(context, targetId, mappingName, (Context c, T r, int ind, object a, object b) =>
		{
			var set = (List<T>)a;
			set.Add(r);
			return new ValueTask();
		}, res, null, options);

		return res;
	}

	/// <summary>
	/// List a set of values from this service which are present in a mapping of the given target type.
	/// This is backwards from the typical mapping flow - i.e. you're getting the list of sources with a given single target value.
	/// </summary>
	/// <typeparam name="MAP_SOURCE"></typeparam>
	/// <typeparam name="S_ID"></typeparam>
	/// <param name="context"></param>
	/// <param name="src"></param>
	/// <param name="srcId"></param>
	/// <param name="mappingName"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public async ValueTask<List<T>> ListBySource<MAP_SOURCE, S_ID>(Context context, AutoService<MAP_SOURCE, S_ID> src, S_ID srcId, string mappingName, DataOptions options = DataOptions.Default)
		where S_ID : struct, IEquatable<S_ID>, IConvertible, IComparable<S_ID>
		where MAP_SOURCE : Content<S_ID>, new()
	{
		var set = new List<T>();

		await ListBySource(context, src, srcId, mappingName, (Context c, T obj, int index, object a, object b) => {

			var passedSet = (List<T>)a;
			passedSet.Add(obj);
			return new ValueTask();

		}, set, null, options);

		return set;
	}

	/// <summary>
	/// List a set of values from this service which are present in a mapping of the given target type.
	/// This is backwards from the typical mapping flow - i.e. you're getting the list of sources with a given single target value.
	/// </summary>
	/// <typeparam name="MAP_SOURCE"></typeparam>
	/// <typeparam name="S_ID"></typeparam>
	/// <param name="context"></param>
	/// <param name="src"></param>
	/// <param name="srcId"></param>
	/// <param name="mappingName"></param>
	/// <param name="onResult"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public async ValueTask ListBySource<MAP_SOURCE, S_ID>(Context context, AutoService<MAP_SOURCE, S_ID> src, S_ID srcId, string mappingName, Func<Context, T, int, object, object, ValueTask> onResult, object a, object b, DataOptions options = DataOptions.Default)
		where S_ID : struct, IEquatable<S_ID>, IConvertible, IComparable<S_ID>
		where MAP_SOURCE : Content<S_ID>, new()
	{
		// Get map:
		var mappingService = await src.GetMap<T, ID>(mappingName);

		var collector = new IDCollector<ID>();
		
		// Ask mapping service for all target values with the given source ID.
		await mappingService.ListTargetIdBySource(context, srcId, (Context ctx, ID id, object src) => {
			var _collector = (IDCollector<ID>)src;
			_collector.Add(id);
			return new ValueTask();
		}, collector);

		await Where("Id=[?]", options).Bind(collector).ListAll(context, onResult, a, b);
		collector.Release();
	}

	/// <summary>
	/// List a set of values from this service which are present in a mapping of the given target type.
	/// This is backwards from the typical mapping flow - i.e. you're getting the list of sources with a given single target value.
	/// </summary>
	/// <typeparam name="MAP_SOURCE"></typeparam>
	/// <typeparam name="S_ID"></typeparam>
	/// <param name="context"></param>
	/// <param name="src"></param>
	/// <param name="srcIds"></param>
	/// <param name="mappingName"></param>
	/// <param name="onResult"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public async ValueTask ListBySource<MAP_SOURCE, S_ID>(Context context, AutoService<MAP_SOURCE, S_ID> src, IDCollector<S_ID> srcIds, string mappingName, Func<Context, T, int, object, object, ValueTask> onResult, object a, object b, DataOptions options = DataOptions.Default)
		where S_ID : struct, IEquatable<S_ID>, IConvertible, IComparable<S_ID>
		where MAP_SOURCE : Content<S_ID>, new()
	{
		// Get map:
		var mappingService = await src.GetMap<T, ID>(mappingName);

		var collector = new IDCollector<ID>();

		// Ask mapping service for all target values with the given source ID.
		await mappingService.ListTargetIdBySource(context, srcIds, (Context ctx, ID id, object src) => {
			var _collector = (IDCollector<ID>)src;
			_collector.Add(id);
			return new ValueTask();
		}, collector);

		await Where("Id=[?]", options).Bind(collector).ListAll(context, onResult, a, b);
		collector.Release();
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
		// Get map:
		var mappingService = await GetMap<MAP_TARGET, T_ID>(mappingName);

		// Ask mapping service for all source values with the given target ID.
		var collector = new IDCollector<ID>();

		await mappingService.ListSourceIdByTarget(context, targetId, (Context ctx, ID id, object src) => {
			var _collector = (IDCollector<ID>)src;
			_collector.Add(id);
			return new ValueTask();
		}, collector);

		await Where("Id=[?]", options).Bind(collector).ListAll(context, onResult, a, b);
		collector.Release();
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
				var array = argSet[i] as JArray;

				if (array != null)
				{
					// Can't mix types here - can either be a string or a uint array.
					// If it's an empty array, use original behaviour for now.
					JToken v = array.Count != 0 ? array[0] as JToken : null;

					if (v != null && v.Type == JTokenType.String)
					{
						// Act like an array of strings.
						var strSet = new List<string>();

						foreach (var jValue in array)
						{
							if (!(jValue is JValue))
							{
								throw new PublicException(
									"Arg #" + (i + 1) + " in the args set is invalid - an array of objects was given, but it can only be an array of strings.",
									"filter_invalid"
								);
							}

							var strVal = jValue.Value<string>();
							strSet.Add(strVal);
						}

						filter.BindUnknown(strSet as IEnumerable<string>);
					}
					else
					{
						// Act like an array of uint's.
						var idSet = new List<uint>();

						foreach (var jValue in array)
						{
							if (!(jValue is JValue))
							{
								throw new PublicException(
									"Arg #" + (i + 1) + " in the args set is invalid - an array of objects was given, but it can only be an array of IDs or strings.",
									"filter_invalid"
								);
							}

							var id = jValue.Value<uint>();
							idSet.Add(id);
						}

						filter.BindUnknown(idSet as IEnumerable<uint>);
					}
				}
				else
				{
					var value = argSet[i] as JValue;

					if (value == null)
					{
						throw new PublicException(
							"Arg #" + (i + 1) + " in the args set is invalid - it can't be an object, only a string or numeric/ bool value.",
							"filter_invalid"
						);
					}

					// The underlying JSON token is textual, so we'll use a general use bind from string method.
					if (value.Type == JTokenType.Date)
					{
						var date = value.Value as DateTime?;

						// The target value could be a nullable date, in which case we'd need to use Bind(DateTime?)
						if (filter.NextBindType == typeof(DateTime?))
						{
							filter.Bind(date);
						}
						else
						{
							filter.Bind(date.Value);
						}
					}
					else if (value.Type == JTokenType.Boolean)
					{
						var boolVal = value.Value as bool?;

						// The target value could be a nullable bool, in which case we'd need to use Bind(bool?)
						if (filter.NextBindType == typeof(bool?))
						{
							filter.Bind(boolVal);
						}
						else
						{
							filter.Bind(boolVal.Value);
						}
					}
					else if(value.Type == JTokenType.Null)
					{
						filter.BindFromString(null);
					}
					else
					{
						filter.BindFromString(value.Value<string>());
					}
				}
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

		if (pageSize.HasValue)
		{
			filter.SetPage(pageIndex.HasValue ? pageIndex.Value : 0, pageSize.Value);
		}
		else if (pageIndex.HasValue)
		{
			// Default page size used
			filter.SetPage(pageIndex.Value);
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

		// Next, rent any necessary collectors, and execute the collections. RentAndCollect internally performs Setup as well.
		// The first time this happens on a given filter type may also cause the mapping services to load, thus it is awaitable.
		queryPair.QueryA.FirstCollector = await queryPair.QueryA.RentAndCollect(context, this);

		// Ensure B is setup:
		if (queryPair.QueryB.RequiresSetup)
		{
			await queryPair.QueryB.Setup();
		}

		queryPair = await EventGroup.List.Dispatch(context, queryPair);
		var total = queryPair.Total;

		// If collectors were made, let's now release them.
		if (queryPair.QueryA.FirstCollector != null)
        {
			var col = queryPair.QueryA.FirstCollector;
			while (col != null)
			{
				var next = col.NextCollector;
				col.Release();
				col = next;
			}
		}

		return total;
	}

	/// <summary>
	/// Gets an object from this service which matches the given filter and values. If multiple match, it's only ever the first one.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filter"></param>
	/// <param name="filterValues"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public override async ValueTask<object> GetObjectByFilter(Context context, string filter, List<string> filterValues, DataOptions options = DataOptions.Default)
	{
		var filterObject = Where(filter); // Region=? and Slug=? and Article=?

		for (var i = 0; i < filterValues.Count; i++) // polar regions, svalbard, where-to-go
		{
			filterObject = filterObject.BindFromString(filterValues[i]);
		}

		return await filterObject.First(context);
	}

	/// <summary>
	/// Gets an object from this service. Generally use Get instead with a fixed type.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="fieldName"></param>
	/// <param name="fieldValue"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public override async ValueTask<object> GetObject(Context context, string fieldName, string fieldValue, DataOptions options = DataOptions.Default)
	{
		return await Where(fieldName + "=?", options).BindFromString(fieldValue).First(context);
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
	/// Checks if the given target Id is mapped to the given source in the given named map.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="srcId"></param>
	/// <param name="target"></param>
	/// <param name="targetId"></param>
	/// <param name="mapName"></param>
	public async ValueTask<bool> CheckIfMappingExists<T_ID>(Context context, ID srcId, AutoService target, T_ID targetId, string mapName)
		where T_ID : struct, IEquatable<T_ID>, IConvertible, IComparable<T_ID>
	{
		// First, get the mapping service:
		var mapping = await MappingTypeEngine.GetOrGenerate(
			this,
			target,
			mapName
		) as MappingService<ID, T_ID>;

		// Create if not exists:
		return await mapping.CheckIfExists(context, srcId, targetId);
	}
	
	/// <summary>
	/// Deletes a given src->target map entry, returning true if it existed and has been removed.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="srcId"></param>
	/// <param name="target"></param>
	/// <param name="targetId"></param>
	/// <param name="mapName"></param>
	public async ValueTask<bool> DeleteMapping<T_ID>(Context context, ID srcId, AutoService target, T_ID targetId, string mapName)
		where T_ID : struct, IEquatable<T_ID>, IConvertible, IComparable<T_ID>
	{
		// First, get the mapping service:
		var mapping = await MappingTypeEngine.GetOrGenerate(
			this,
			target,
			mapName
		) as MappingService<ID, T_ID>;

		// Delete:
		return await mapping.DeleteByIds(context, srcId, targetId);
	}
	
	/// <summary>
	/// Ensures the given target Id is mapped to the given source in the given named map.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="srcId"></param>
	/// <param name="target"></param>
	/// <param name="targetId"></param>
	/// <param name="mapName"></param>
	public async ValueTask<bool> CreateMappingIfNotExists<T_ID>(Context context, ID srcId, AutoService target, T_ID targetId, string mapName)
		where T_ID : struct, IEquatable<T_ID>, IConvertible, IComparable<T_ID>
	{
		// First, get the mapping service:
		var mapping = await MappingTypeEngine.GetOrGenerate(
			this,
			target,
			mapName
		) as MappingService<ID, T_ID>;

		// Create if not exists:
		return await mapping.CreateIfNotExists(context, srcId, targetId);
	}

	/// <summary>
	/// Ensures the list of target IDs are mapped to the given source in the given named map.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="src"></param>
	/// <param name="target"></param>
	/// <param name="targetIds"></param>
	/// <param name="mapName"></param>
	public virtual async ValueTask EnsureMapping<T_ID>(Context context, T src, AutoService target, IEnumerable<T_ID> targetIds, string mapName)
		where T_ID : struct, IEquatable<T_ID>, IConvertible, IComparable<T_ID>
	{
		// First, get the mapping service:
		var mapping = await MappingTypeEngine.GetOrGenerate(
			this,
			target,
			mapName
		) as MappingService<ID, T_ID>;

		// Ask it to validate:
		await mapping.EnsureMapping(context, src.Id, targetIds);
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
	/// Populates the given raw entity from the given entity. Any blank localised fields are copied from the primary entity.
	/// </summary>
	/// <param name="raw"></param>
	/// <param name="entity"></param>
	/// <param name="primaryEntity"></param>
	public void PopulateRawEntityFromTarget(T raw, T entity, T primaryEntity)
	{
		// First, all the fields we'll be working with:
		var allFields = FieldMap.Fields;

		for (var i = 0; i < allFields.Count; i++)
		{
			// Get the field:
			var field = allFields[i];

			// Read the value from the original object:
			var value = field.TargetField.GetValue(entity);

			// If the field is localised, and the raw value is null, use value from primaryEntity instead.
			// Note! [Localised] fields must be a nullable type for this to ever happen.

			if (field.LocalisedName != null && field.IsNullable())
			{
				// If the entity field matches the primary content one and the type is nullable, set a null into raw.
				var primaryContentValue = field.TargetField.GetValue(primaryEntity);

				if (primaryContentValue != null)
				{
					if (primaryContentValue.Equals(value))
					{
						// This localised type has the same value as the primary locale. Don't translate it.
						value = null;
					}
				}
			}

			field.TargetField.SetValue(raw, value);
		}
	}

	/// <summary>
	/// Creates a raw entity from a given localised target.
	/// This clones the given object and sets any localised fields to their default.
	/// </summary>
	/// <param name="entity"></param>
	/// <returns></returns>
	public T CreateRawEntityFromTarget(T entity)
	{
		var raw = (T)Activator.CreateInstance(InstanceType);

		var allFields = FieldMap.Fields;

		for (var i = 0; i < allFields.Count; i++)
		{
			// Get the field:
			var field = allFields[i];

			// If the field is localised, it remains on its default.
			if (field.LocalisedName == null)
			{
				// Not a localised field - set its value from the given entity.
				var value = field.TargetField.GetValue(entity);
				field.TargetField.SetValue(raw, value);
			}
		}

		return raw;
	}

	/// <summary>
	/// Populates the given entity from the given raw and primary entities.
	/// The raw entity is used to check if a locale specific override exists.
	/// If it does, the value comes from the raw entity. Otherwise, it comes from the primary entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="raw"></param>
	/// <param name="primaryEntity"></param>
	public void PopulateTargetEntityFromRaw(T entity, T raw, T primaryEntity)
	{
		// First, all the fields we'll be working with:
		var allFields = FieldMap.Fields;

		for (var i = 0; i < allFields.Count; i++)
		{
			// Get the field:
			var field = allFields[i];

			object value;

			// If the field is not localised, then the new value comes from the primary entity.
			if (field.LocalisedName == null)
			{
				value = field.TargetField.GetValue(primaryEntity);
			}
			else
			{
				// Does the locale specify a value for this field?
				// Read the value from the raw object:
				value = field.TargetField.GetValue(raw);

				// If the field is localised, and the raw value is null, use value from primaryEntity instead.
				// Note! [Localised] fields must be a nullable type for this to ever happen.
				if (value == null)
				{
					// Read from primary entity instead:
					value = field.TargetField.GetValue(primaryEntity);
				}
			}

			field.TargetField.SetValue(entity, value);
		}

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
	/// Call this when the primary object changes. It makes sure any localised versions are updated.
	/// </summary>
	/// <param name="entity"></param>
	public void OnPrimaryEntityChanged(T entity)
	{
		// Primary locale update - must update all other caches in case they contain content from the primary locale.
		var id = entity.GetId();

		var caches = _cacheSet?.Caches;

		if (caches == null)
		{
			return;
		}

		for (var i = 1; i < caches.Length; i++)
		{
			var altLocaleCache = caches[i];

			if (altLocaleCache == null)
			{
				continue;
			}

			var altRaw = altLocaleCache.GetRaw(id);
			var alt = altLocaleCache.Get(id);

			if (altRaw == null || alt == null)
			{
				// This row is not in this locale.
				continue;
			}

			// Update the alt object again:
			PopulateTargetEntityFromRaw(alt, altRaw, entity);
		}
	}

	/// <summary>
	/// Performs an update on the given entity. If updating the object is permitted, the callback is executed. 
	/// You must only set fields on the object in that callback, or in a BeforeUpdate handle.
	/// </summary>
	public virtual async ValueTask<T> Update(Context context, ID id, Action<Context, T, T> cb, DataOptions options = DataOptions.Default)
	{
		var entity = await Get(context, id);
		
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
			var flds = new FieldMap(typeof(T), EntityName);

			// Remove "Id" field as it's not permitted to be marked as changed:
			flds.Remove("Id");

			_diffFields = flds;

			if (flds.Count > 64)
			{
				// You've ignored the other error for too long and it has become more severe.
				// If you encounter this situation and having this many fields is required, ChangedFields needs to instead allocate a ulong array.
				throw new Exception("Too many fields");
			}
			else if (flds.Count >= 50)
			{
				Log.Warn(LogTag, "This service has an unusually large amount of fields (" + flds.Count + "). 64 is the current limit.");
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
				
				if (mainType == typeof(DateTime) || mainType == typeof(string) || mainType == typeof(decimal))
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

			foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, field);
				generator.Emit(OpCodes.Stfld, field);
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
		var entityToUpdate = await StartUpdate(context, cachedEntity, options);

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
			ce = new T();
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
		var entityToUpdate = await StartUpdate(context, cachedEntity, options);

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
	/// For simpler usage, see Update. This is for advanced non-allocating updates. Returns the object that you MUST apply your changes to.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="entity"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public async ValueTask<T> StartUpdate(Context context, T entity, DataOptions options = DataOptions.Default)
	{
		if (options != DataOptions.IgnorePermissions)
		{
			// Perform the permission test now:
			await EventGroup.BeforeUpdate.TestCapability(context, entity);
		}

		// Minor todo: get this object from a pool for high velocity updates.
		var t = new T();
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

		entityToUpdate = await EventGroup.AfterUpdate.Dispatch(context, entityToUpdate);
		return entityToUpdate;
	}
	
}

/// <summary>
/// The base class of all AutoService instances.
/// </summary>
public partial class AutoService
{
	/// <summary>
	/// True if this type is synced. This is typically set to true if the cache is active.
	/// </summary>
	public bool Synced;
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
	/// Outputs a list of things from this service as JSON into the given writer.
	/// Executes the given collector(s) whilst it happens, which can also be null.
	/// Does not perform permission checks internally.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="collectors"></param>
	/// <param name="idSet"></param>
	/// <param name="writer"></param>
	/// <param name="viaInclude">True if it's via an include, and therefore the "from" filter field is implied true.</param>
	/// <param name="functionalIncludes">Optional set of functional includes to execute on each node as the json is rendered.</param>
	/// <returns></returns>
	public virtual ValueTask OutputJsonList(Context context, IDCollector collectors, IDCollector idSet, Writer writer, bool viaInclude, FunctionalInclusionNode[] functionalIncludes = null)
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
	/// <returns></returns>
	public virtual ValueTask ObjectToJson(Context context, object entity, Writer writer, Stream targetStream = null, string includes = null)
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
	/// Outputs a list of things from this service as JSON into the given writer.
	/// Executes the given collector(s) whilst it happens, which can also be null.
	/// Does not perform permission checks internally.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="collectors"></param>
	/// <param name="idSet"></param>
	/// <param name="setField"></param>
	/// <param name="writer"></param>
	/// <param name="viaIncludes">True if the list is via includes</param>
	/// <param name="functionalIncludes">Optional set of functional includes to execute on each node as the json is rendered.</param>
	/// <returns></returns>
	public virtual ValueTask OutputJsonList<S_ID>(Context context, IDCollector collectors, IDCollector idSet, string setField, Writer writer, bool viaIncludes, FunctionalInclusionNode[] functionalIncludes = null)
		 where S_ID : struct, IEquatable<S_ID>, IComparable<S_ID>
	{
		// Not supported on this service.
		return new ValueTask();
	}

	/// <summary>
	/// Outputs a mapping. Only valid on a Mapping service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="mappingCollector"></param>
	/// <param name="idSet"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public virtual ValueTask OutputMap(Context context, IDCollector mappingCollector, IDCollector idSet, Writer writer)
	{
		// Not supported on this service.
		return new ValueTask();
	}

	/// <summary>
	/// Outputs a single object from this service as JSON into the given writer. Acts like include * was specified by default.
	/// Executes the given collector(s) whilst it happens, which can also be null.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="writer"></param>
	/// <param name="includes"></param>
	/// <returns></returns>
	public virtual ValueTask OutputById(Context context, ulong id, Writer writer, string includes = "*")
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
	/// Gets a map which lists the available fields in the content type.
	/// </summary>
	/// <returns></returns>
	public ContentFields GetContentFields()
	{
		if (_contentFields == null)
		{
			_contentFields = new ContentFields(this);
		}

		return _contentFields;
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
	/// Gets an object from this service which matches the given particular field/value. If multiple match, it's only ever the first one.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="fieldName"></param>
	/// <param name="fieldValue"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public virtual ValueTask<object> GetObject(Context context, string fieldName, string fieldValue, DataOptions options = DataOptions.Default)
	{
		return new ValueTask<object>(null);
	}

	/// <summary>
	/// Gets an object from this service which matches the given filter and values. If multiple match, it's only ever the first one.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filter"></param>
	/// <param name="filterValues"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public virtual ValueTask<object> GetObjectByFilter(Context context, string filter, List<string> filterValues, DataOptions options = DataOptions.Default)
	{
		return new ValueTask<object>(null);
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
	/// List is mappings linked back to this service e.g tags
	/// </summary>
    public List<MappingServiceGenerationMeta> GeneratedMappings;


    /// <summary>
    /// Installs generic admin pages for this service.
    /// Does nothing if there isn't a page service installed, or if the admin pages already exist.
    /// </summary>
    /// <param name="fields"></param>
    protected void InstallAdminPages(string[] fields)
	{
		InstallAdminPages(null, null, fields, null);
	}

	/// <summary>
	/// Installs generic admin pages for this service, including the nav menu entry.
	/// Does nothing if there isn't a page service installed, or if the admin pages already exist.
	/// </summary>
	/// <param name="navMenuLabel">The text to show on the navmenu.</param>
	/// <param name="navMenuIconRef">The ref for the icon to use on the navmenu. Usually a fontawesome icon, of the form "fa:fa-thing".</param>
	/// <param name="fields">The fields to show in the list of your content type. Usually include at least some sort of name or title.</param>
	/// <param name="childAdminPage">
	/// A shortcut for specifying that your type has some kind of sub-type.
	/// For example, the NavMenu admin page specifies a child type of NavMenuItem, meaning each NavMenu ends up with a list of NavMenuItems.
	/// Make sure you specify the fields that'll be visible from the child type in the list on the parent type.
	/// For example, if you'd like each child entry to show its Id and Title fields, specify new string[]{"id", "title"}.
	/// </param>
	protected void InstallAdminPages(string navMenuLabel, string navMenuIconRef, string[] fields, ChildAdminPageOptions childAdminPage = null)
	{
		if (Services.Started)
		{
			InstallAdminPagesInternal(navMenuLabel, navMenuIconRef, fields, childAdminPage);
		}
		else
		{
			// Must happen after services start otherwise the page service isn't necessarily available yet.
			Events.Service.AfterStart.AddEventListener((Context ctx, object src) =>
			{
				InstallAdminPagesInternal(navMenuLabel, navMenuIconRef, fields, childAdminPage);
				return new ValueTask<object>(src);
			});
		}
	}
	
	private void InstallAdminPagesInternal(string navMenuLabel, string navMenuIconRef, string[] fields, ChildAdminPageOptions childAdminPage)
	{
		var pageService = Api.Startup.Services.Get("PageService");

		if (pageService == null)
		{
			// No point installing nav menu entries either if there's no pages.
			return;
		}

		Task.Run(async () =>
		{
			var installPages = pageService.GetType().GetMethod("InstallAdminPages");
			
			if (installPages != null)
			{
				// InstallAdminPages(string typeName, string[] fields)
				await (ValueTask)installPages.Invoke(pageService, new object[] {
					ServicedType,
					fields,
					childAdminPage
				});
			}

			// Nav menu also?
			if (navMenuLabel != null)
			{
				var navMenuItemService = Api.Startup.Services.Get("AdminNavMenuItemService");

				if (navMenuItemService != null)
				{
					var installNavMenuEntry = navMenuItemService.GetType().GetMethod("InstallAdminEntry");

					if (installNavMenuEntry != null)
					{
						// InstallAdminEntry(string targetUrl, string iconRef, string label)
						await (ValueTask)installNavMenuEntry.Invoke(navMenuItemService, new object[] {
							"/en-admin/" + ServicedType.Name.ToLower(),
							navMenuIconRef,
							navMenuLabel
						});
					}
				}
			}
		});

	}

	/// <summary>
	/// Installs one or more roles. You must provide a Key and no Id on each one.
	/// The permissions module is required anyway and must be up to date.
	/// </summary>
	protected void InstallRoles(params Role[] roles)
	{
		if (Services.Started)
		{
			InstallRolesInternal(roles);
		}
		else
		{
			// Must happen after services start otherwise the role service isn't necessarily available yet.
			Events.Service.AfterStart.AddEventListener((Context ctx, object src) =>
			{
				InstallRolesInternal(roles);
				return new ValueTask<object>(src);
			});
		}
	}
	
	private static void InstallRolesInternal(Role[] roles)
	{
		var roleService = Services.Get<RoleService>();

		if (roleService == null)
		{
			return;
		}
		
		Task.Run(async () =>
		{
			await roleService.InstallNow(roles);
		});

	}

}
