using Api.Contexts;
using Api.Database;
using Api.DatabaseDiff;
using Api.Eventing;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	where ID: struct, IConvertible, IEquatable<ID>
{
	
	/// <summary>
	/// A query which deletes 1 entity.
	/// </summary>
	protected readonly Query deleteQuery;

	/// <summary>
	/// A query which creates 1 entity.
	/// </summary>
	protected readonly Query createQuery;
	
	/// <summary>
	/// A query which creates 1 entity with a known ID.
	/// </summary>
	protected readonly Query createWithIdQuery;

	/// <summary>
	/// A query which selects 1 entity.
	/// </summary>
	protected readonly Query selectQuery;

	/// <summary>
	/// A query which lists multiple entities.
	/// </summary>
	protected readonly Query listQuery;
	
	/// <summary>
	/// A query which lists multiple raw entities.
	/// </summary>
	protected readonly Query listRawQuery;

	/// <summary>
	/// A query which updates 1 entity.
	/// </summary>
	protected readonly Query updateQuery;

	/// <summary>
	/// The set of update/ delete/ create etc events for this type.
	/// </summary>
	public EventGroup<T, ID> EventGroup;

	/// <summary>
	/// Sets up the common service type fields.
	/// </summary>
	public AutoService(EventGroup eventGroup, Type instanceType = null) : base(typeof(T), typeof(ID), instanceType)
	{
		// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
		// whilst also using a high-level abstraction as another plugin entry point.
		deleteQuery = Query.Delete(InstanceType);
		createQuery = Query.Insert(InstanceType);
		createWithIdQuery = Query.Insert(InstanceType, true);
		updateQuery = Query.Update(InstanceType);
		selectQuery = Query.Select(InstanceType);
		listQuery = Query.List(InstanceType);
		listRawQuery = Query.List(InstanceType);
		listRawQuery.Raw = true;

		EventGroup = eventGroup as EventGroup<T, ID>;

		// GetObject specifically uses integer IDs (and is only available on services that have integer IDs)
		// We need to make a delegate that it can use for mapping that integer through to whatever typeof(ID) is.
		if (typeof(ID) == typeof(uint))
		{
			var getMethodDelegate = (Func<Context, ID, DataOptions, ValueTask<T>>)Get;
			_getWithIntId = (getMethodDelegate as Func<Context, uint, DataOptions, ValueTask<T>>);
		}
		else if (typeof(ID) == typeof(int))
		{
			throw new ArgumentException("All ID's must now be unsigned types. Consider changing to Content<uint>.", nameof(ID));
		}
	}
	
	private readonly Func<Context, uint, DataOptions, ValueTask<T>> _getWithIntId;
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
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public async ValueTask<JsonStructure<T,ID>> GetTypedJsonStructure(Context ctx)
	{
		var roleId = ctx.RoleId;

		if (_jsonStructures == null)
		{
			_jsonStructures = new JsonStructure<T,ID>[roleId];
		}
		else if (roleId > _jsonStructures.Length)
		{
			Array.Resize(ref _jsonStructures, (int)roleId);
		}

		var structure = _jsonStructures[roleId - 1];
		
		if(structure == null)
		{
			// Not built yet. Build it now:
			var role = await Services.Get<RoleService>().Get(new Context(), roleId, DataOptions.IgnorePermissions);
			_jsonStructures[roleId - 1] = structure = new JsonStructure<T,ID>(role);
			structure.Service = this;
			await structure.Build(GetContentFields(), EventGroup.BeforeSettable, EventGroup.BeforeGettable);
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

		// Delete the entry:
		if (_database != null)
		{
			await _database.RunWithId(context, deleteQuery, result.Id);
		}

		var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

		if (cache != null)
		{
			cache.Remove(context, result.GetId());
		}

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
		where T_ID : struct, IEquatable<T_ID>, IConvertible
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
	/// <returns></returns>
	public async ValueTask<List<T>> ListByTarget<MAP_TARGET, T_ID>(Context context, T_ID targetId, string mappingName)
		where T_ID : struct, IEquatable<T_ID>, IConvertible
		where MAP_TARGET : Content<T_ID>, new()
	{
		var res = new List<T>();
		await ListByTarget<MAP_TARGET, T_ID>(context, targetId, mappingName, (Context c, T r, int ind, object a, object b) =>
		{
			var set = (List<T>)a;
			set.Add(r);
			return new ValueTask();
		}, res, null);

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
	/// <param name="onResult"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public async ValueTask ListBySource<MAP_SOURCE, S_ID>(Context context, AutoService<MAP_SOURCE, S_ID> src, S_ID srcId, string mappingName, Func<Context, T, int, object, object, ValueTask> onResult, object a, object b)
		where S_ID : struct, IEquatable<S_ID>, IConvertible
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

		await Where("Id=[?]").Bind(collector).ListAll(context, onResult, a, b);
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
	/// <returns></returns>
	public async ValueTask ListBySource<MAP_SOURCE, S_ID>(Context context, AutoService<MAP_SOURCE, S_ID> src, IDCollector<S_ID> srcIds, string mappingName, Func<Context, T, int, object, object, ValueTask> onResult, object a, object b)
		where S_ID : struct, IEquatable<S_ID>, IConvertible
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

		await Where("Id=[?]").Bind(collector).ListAll(context, onResult, a, b);
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
	/// <returns></returns>
	public async ValueTask ListByTarget<MAP_TARGET, T_ID>(Context context, T_ID targetId, string mappingName, Func<Context, T, int, object, object, ValueTask> onResult, object a, object b)
	where T_ID: struct, IEquatable<T_ID>, IConvertible
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

		await Where("Id=[?]").Bind(collector).ListAll(context, onResult, a, b);
		collector.Release();
	}

	private Dictionary<string, FilterMeta<T,ID>> _filterSets = new Dictionary<string, FilterMeta<T,ID>>();

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
	/// Gets a fast filter for the given query text.
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
				Console.WriteLine("[WARN] Clearing large filter cache. If this happens frequently it's a symptom of poorly designed filters.");
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
	/// Non-allocating where selection of objects from this service. On the returned object, use e.g. GetList()
	/// </summary>
	/// <returns></returns>
	public Filter<T, ID> Where(DataOptions opts = DataOptions.Default)
	{
		// Get the filter for the user query:
		return GetFilterFor("", opts);
	}

	/// <summary>
	/// Non-allocating where selection of objects from this service. On the returned object, use e.g. GetList()
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

		if (!filter.FullyBound())
		{
			// Safety check - filters come from a pool, so if a filter has not been fully 
			// bound then it is likely to have other data from a previous request in it.
			throw new PublicException(
				"This filter has " + filter.Pool.ArgTypes.Count + " args but not all were bound. Make sure you .Bind() all args.",
				"filter_invalid"
			);
		}
		
		var total = 0;

		// struct:
		var queryPair = new QueryPair<T, ID>()
		{
			QueryA = filter
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

		// Next, rent any necessary collectors, and execute the collections.
		// The first time this happens on a given filter type may also cause the mapping services to load, thus it is awaitable.
		queryPair.QueryA.FirstACollector = await queryPair.QueryA.RentAndCollect(context, this);
		queryPair.QueryA.FirstBCollector = await queryPair.QueryB.RentAndCollect(context, this);

		// Do we have a cache?
		var cache = (filter.DataOptions & DataOptions.CacheFlag) == DataOptions.CacheFlag ? GetCacheForLocale(context.LocaleId) : null;

		if (cache != null)
		{
			// Great - we're using the cache:
			total = await cache.GetResults(context, queryPair, onResult, srcA, srcB);
		}
		else if (_database != null)
		{
			// "Raw" results are as-is from the database.
			// That means the fields are not automatically filled in with the default locale when they're empty.
			var raw = (filter.DataOptions & DataOptions.RawFlag) == DataOptions.RawFlag;

			// Get the results from the database:
			total = await _database.GetResults(context, queryPair, onResult, srcA, srcB, InstanceType, raw ? listRawQuery : listQuery);
		}

		return total;
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
	public override async ValueTask<object> GetObject(Context context, uint id, DataOptions options = DataOptions.Default)
	{
		if (_getWithIntId == null)
		{
			throw new Exception("Only available on types with integer IDs. " + typeof(T) + " uses an ID which is a " + typeof(ID));
		}

		return await _getWithIntId(context, id, options);
	}

	/// <summary>
	/// Gets a single entity by its ID.
	/// </summary>
	public virtual async ValueTask<T> Get(Context context, ID id, DataOptions options = DataOptions.Default)
	{
		// Note: Get is unique in that its permission check happens in the After event handler.
		id = await EventGroup.BeforeLoad.Dispatch(context, id);
		
		T item = null;

		var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

		if (cache != null)
		{
			item = cache.Get(id);
		}

		if (item == null && _database != null)
		{
			item = await _database.Select<T, ID>(context, selectQuery, InstanceType, id);
		}
		
		// Ignoring the permissions only needs to occur on *After* for Gets.
		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = (options & DataOptions.PermissionsFlag) != DataOptions.PermissionsFlag;
		item = await EventGroup.AfterLoad.Dispatch(context, item);
		context.IgnorePermissions = previousPermState;
		
		return item;
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
		where T_ID : struct, IEquatable<T_ID>, IConvertible
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
		where T_ID : struct, IEquatable<T_ID>, IConvertible
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

		if (_database != null)
		{
			if (!entity.GetId().Equals(default(ID)))
			{
				// Explicit ID has been provided.
				await _database.Run<T,ID>(context, createWithIdQuery, entity);
			}
			else if (!await _database.Run<T, ID>(context, createQuery, entity))
			{
				return default;
			}
		}

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
		var allFields = listQuery.Fields.Fields;

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
	/// Populates the given entity from the given raw entity. Any blank localised fields are copied from the primary entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="raw"></param>
	/// <param name="primaryEntity"></param>
	public void PopulateTargetEntityFromRaw(T entity, T raw, T primaryEntity)
	{
		// First, all the fields we'll be working with:
		var allFields = listQuery.Fields.Fields;

		for (var i = 0; i < allFields.Count; i++)
		{
			// Get the field:
			var field = allFields[i];

			// Read the value from the raw object:
			var value = field.TargetField.GetValue(raw);

			// If the field is localised, and the raw value is null, use value from primaryEntity instead.
			// Note! [Localised] fields must be a nullable type for this to ever happen.
			if (field.LocalisedName != null && value == null && primaryEntity != null)
			{
				// Read from primary entity instead:
				value = field.TargetField.GetValue(primaryEntity);
			}
			
			field.TargetField.SetValue(entity, value);
		}

	}

	/// <summary>
	/// Updates the database and the cache without triggering any events, based on the provided mode.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="raw">The raw version of the entity.</param>
	/// <param name="mode">U = update, C = Create, D = Delete</param>
	/// <param name="deletedId"></param>
	public async Task RawUpdateEntity(Context context, T raw, char mode, ID deletedId)
	{
		if (_database != null)
		{
			if (mode == 'D')
			{
				// Delete
				await _database.RunWithId(context, deleteQuery, deletedId);
			}
			else if (mode == 'U' || mode == 'C')
			{
				// Updated or created
				// In both cases, check if it exists first:
				var id = raw.GetId();
				if (await _database.Select<T, ID>(context, selectQuery, InstanceType, id) != null)
				{
					// Update
					await _database.Run<T, ID>(context, updateQuery, raw, id);
				}
				else
				{
					// Create
					await _database.Run<T, ID>(context, createWithIdQuery, raw);
				}
			}
		}

		// Update the cache next:
		var cache = GetCacheForLocale(context.LocaleId);

		if (cache != null)
		{
			lock (cache)
			{
				if (mode == 'C' || mode == 'U')
				{
					// Created or updated

					T entity;

					if (context.LocaleId == 1)
					{
						// Primary locale. Entity == raw entity, and no transferring needs to happen.
						entity = raw;
					}
					else
					{
						// Get the 'real' (not raw) entity from the cache. We'll copy the fields from the raw object to it.
						var id = raw.GetId();
						entity = cache.Get(id);

						if (entity == null)
						{
							entity = new T();
						}

						// Transfer fields from raw to entity, using the primary object as a source of blank fields:
						PopulateTargetEntityFromRaw(entity, raw, GetCacheForLocale(1).Get(id));
					}

					cache.Add(context, entity, raw);
				}
				else if (mode == 'D')
				{
					// Deleted
					cache.Remove(context, raw.GetId());
				}
			}
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
		var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

		if (cache != null)
		{
			T entity;

			if (context.LocaleId == 1)
			{
				// Primary locale. Entity == raw entity, and no transferring needs to happen. This is expected to always be the case for creations.
				entity = raw;
			}
			else
			{
				// Get the 'real' (not raw) entity from the cache. We'll copy the fields from the raw object to it.
				entity = new T();

				// Transfer fields from raw to entity, using the primary object as a source of blank fields:
				PopulateTargetEntityFromRaw(entity, raw, null);
			}

			cache.Add(context, entity, raw);
		}

		return await EventGroup.AfterCreate.Dispatch(context, raw);
	}

	/// <summary>
	/// Gets objects from this service using a generic serialized filter. Use List instead whenever possible.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filterJson"></param>
	/// <param name="includes"></param>
	/// <param name="so"></param>
	/// <returns></returns>
	public async Task ListForSSR(Context context, string filterJson, string includes, Microsoft.ClearScript.ScriptObject so)
	{
		var filterNs = Newtonsoft.Json.JsonConvert.DeserializeObject(filterJson) as JObject;

		var filter = LoadFilter(filterNs) as Filter<T, ID>;

		// Write:
		var writer = Writer.GetPooled();
		writer.Start(null);

		await ToJson(context, filter, async (Context ctx, Filter<T, ID> filt, Func<T, int, ValueTask> onResult) => {

			return await GetResults(ctx, filt, async (Context ctx2, T result, int index, object src, object src2) => {

				var _onResult = src as Func<T, int, ValueTask>;
				await _onResult(result, index);

			}, onResult, null);

		}, writer, null, includes);

		filter.Release();
		var jsonResult = writer.ToUTF8String();
		writer.Release();
		so.Invoke(false, jsonResult);
	}
	
	/// <summary>
	/// Gets an object from this service for use by the serverside renderer. Returns it by executing the given callback.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="includes"></param>
	/// <param name="so"></param>
	public async Task GetForSSR(Context context, ID id, string includes, Microsoft.ClearScript.ScriptObject so)
	{
		// Get the object (includes the perm checks):
		var content = await Get(context, id);

		// Write:
		var writer = Writer.GetPooled();
		writer.Start(null);
		await ToJson(context, content, writer, null, includes);
		var jsonResult = writer.ToUTF8String();
		writer.Release();
		so.Invoke(false, jsonResult);
	}

	/// <summary>
	/// Call this when the primary object changes. It makes sure any localised versions are updated.
	/// </summary>
	/// <param name="entity"></param>
	public void OnPrimaryEntityChanged(T entity)
	{
		// Primary locale update - must update all other caches in case they contain content from the primary locale.
		var id = entity.GetId();

		for (var i = 1; i < _cache.Length; i++)
		{
			var altLocaleCache = _cache[i];

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
	public virtual async ValueTask<T> Update(Context context, ID id, Action<Context, T> cb, DataOptions options = DataOptions.Default)
	{
		var entity = await Get(context, id);
		
		if (entity == null)
		{
			return null;
		}
		
		return await Update(context, entity, cb, options);
	}
	
	/// <summary>
	/// Performs an update on the given entity. If updating the object is permitted, the callback is executed. 
	/// You must only set fields on the object in that callback, or in a BeforeUpdate handle.
	/// </summary>
	public virtual async ValueTask<T> Update(Context context, T entity, Action<Context, T> cb, DataOptions options = DataOptions.Default)
	{
		if (options != DataOptions.IgnorePermissions && !await StartUpdate(context, entity))
		{
			// Note it would've thrown.
			return null;
		}
		
		// Set fields now:
		cb(context, entity);

		// And perform the save:
		return await FinishUpdate(context, entity);
	}

	/// <summary>
	/// For simpler usage, see Update. This is for advanced non-allocating updates.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="entity"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public ValueTask<bool> StartUpdate(Context context, T entity, DataOptions options = DataOptions.Default)
	{
		if (options != DataOptions.IgnorePermissions)
		{
			// Perform the permission test now:
			EventGroup.BeforeUpdate.TestCapability(context, entity);
		}

		// Reset marked changes:
		entity.ResetChanges();

		return new ValueTask<bool>(true);
	}

	/// <summary>
	/// Used together with CanUpdate in the form if(await CanUpdate){ set fields, await DoUpdate }.
	/// This route is used to set fields on the object without an allocation.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="entity"></param>
	/// <returns></returns>
	public async ValueTask<T> FinishUpdate(Context context, T entity)
	{
		entity = await EventGroup.BeforeUpdate.Dispatch(context, entity);
		
		if (entity == null)
		{
			return null;
		}

		var id = entity.Id;

		T raw = null;

		var locale = context == null ? 1 : context.LocaleId;
		var cache = GetCacheForLocale(locale);

		if (locale == 1)
		{
			raw = entity;
		}
		else
		{
			if (cache != null)
			{
				raw = cache.GetRaw(id);
			}

			if (raw == null)
			{
				raw = new T();
			}

			// Must also update the raw object in the cache (as the given entity is _not_ the raw one).
			T primaryEntity;

			if (cache == null)
			{
				primaryEntity = await Get(new Context(1, context.User, context.RoleId), id);
			}
			else
			{
				primaryEntity = GetCacheForLocale(1).Get(id);
			}

			PopulateRawEntityFromTarget(raw, entity, primaryEntity);
		}

		if (_database != null && !await _database.Run<T, ID>(context, updateQuery, raw, id))
		{
			return null;
		}

		if (cache != null)
		{
			cache.Add(context, entity, raw);

			if (locale == 1)
			{
				OnPrimaryEntityChanged(entity);
			}

		}

		entity = await EventGroup.AfterUpdate.Dispatch(context, entity);
		return entity;
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
	/// The actual instance type of this service. This always equals ServicedType or inherits it.
	/// </summary>
	public Type InstanceType;
	/// <summary>
	/// The type that this AutoService uses for IDs, if any. Almost always int, but some use ulong.
	/// </summary>
	public Type IdType;

	/// <summary>
	/// The database service.
	/// </summary>
	protected DatabaseService _database;

	/// <summary>
	/// The schema for all tables related to this service. This field is updated automatically via modding the schema during the DatabaseDiffBeforeAdd event.
	/// </summary>
	public Schema DatabaseSchema;

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
	/// <returns></returns>
	public virtual ValueTask OutputJsonList(Context context, IDCollector collectors, IDCollector idSet, Writer writer, bool viaInclude)
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
	/// <param name="targetStream"></param>
	/// <param name="includes"></param>
	/// <returns></returns>
	public virtual ValueTask ObjectToJson(Context context, object entity, Writer writer, Stream targetStream = null, string includes = null)
	{
		// Not supported on this service.
		return new ValueTask();
	}

	/// <summary>
	/// Loads a filter from the given newtonsoft representation. You must .Release() this filter when you're done with it.
	/// </summary>
	/// <param name="newtonsoft"></param>
	/// <returns></returns>
	public FilterBase LoadFilter(JObject newtonsoft)
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
		var filter = GetGeneralFilterFor(str);

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
					// Act like an array of uint's.
					var idSet = new List<uint>();

					foreach (var jValue in array)
					{
						if (!(jValue is JValue))
						{
							throw new PublicException(
								"Arg #" + (i + 1) + " in the args set is invalid - an array of objects was given, but it can only be an array of IDs.",
								"filter_invalid"
							);
						}

						var id = jValue.Value<uint>();
						idSet.Add(id);
					}

					filter.BindUnknown(idSet);
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
					if (value.Type == JTokenType.Null)
					{
						filter.BindUnknown(null);
					}
					else
					{
						filter.BindUnknown(value.Value<string>());
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
	/// </summary>
	/// <param name="query"></param>
	/// <param name="canContainConstants"></param>
	/// <returns></returns>
	public virtual FilterBase GetGeneralFilterFor(string query, bool canContainConstants = false)
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
	/// <returns></returns>
	public virtual ValueTask OutputJsonList<S_ID>(Context context, IDCollector collectors, IDCollector idSet, string setField, Writer writer, bool viaIncludes)
		 where S_ID : struct, IEquatable<S_ID>
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
	/// Outputs a single object from this service as JSON into the given writer. Acts like include * was specified.
	/// Executes the given collector(s) whilst it happens, which can also be null.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public virtual ValueTask OutputById(Context context, uint id, Writer writer)
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
	public AutoService(Type type = null, Type idType = null, Type instanceType = null)
	{
		// _database is left blank if:
		// * Type is given.
		// * Type is marked CacheOnly.
		// This is such that basic services without a type have a convenience database field, 
		// but types that are in-memory only don't attempt to use the database.
		if (type != null && !IsPersistentType(type))
		{
			_database = null;
		}
		else
		{
			_database = Services.Get<DatabaseService>();
		}

		ServicedType = type;
		InstanceType = instanceType == null ? type : instanceType;
		IdType = idType;
	}

	/// <summary>
	/// True if this service stores persistent data.
	/// </summary>
	public bool DataIsPersistent
	{
		get
		{
			return ServicedType != null && _database != null;
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
	/// Gets the named change field. Use this in MarkChanged calls. If you're updating multiple fields, use .And("AnotherField") for convenience.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public ComposableChangeField GetChangeField(string name)
	{
		var ccf = new ComposableChangeField();
		ccf.Map = GetContentFields();
		ccf.And(name);
		return ccf;
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
	/// The fields of this type.
	/// </summary>
	private ContentFields _contentFields;

	/// <summary>
	/// True if the given is a persistent type (i.e. if it should be stored in the database or not).
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	private bool IsPersistentType(Type t)
	{
		var cacheOnlyAttribs = t.GetCustomAttributes(typeof(CacheOnlyAttribute), true);
		return (cacheOnlyAttribs == null || cacheOnlyAttribs.Length == 0);
	}

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
	/// Gets an object from this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public virtual ValueTask<object> GetObject(Context context, uint id, DataOptions options = DataOptions.Default)
	{
		return new ValueTask<object>(null);
	}

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

	/// <summary>
	/// Installs one or more email templates.
	/// Schedules the install to happen either immediately if services have not yet started (async) or after services have started.
	/// </summary>
	/// <param name="templates"></param>
	public void InstallEmails(params Api.Emails.EmailTemplate[] templates)
	{
		if (Services.Started)
		{
			InstallEmailsInternal(templates);
		}
		else
		{
			// Must happen after services start otherwise the email template service isn't necessarily ready yet.
			Events.Service.AfterStart.AddEventListener((Context ctx, object src) =>
			{
				InstallEmailsInternal(templates);
				return new ValueTask<object>(src);
			});
		}
	}

	private static void InstallEmailsInternal(Api.Emails.EmailTemplate[] templates)
	{
		var emailService = Services.Get<Api.Emails.EmailTemplateService>();

		if (emailService == null)
		{
			return;
		}

		Task.Run(async () =>
		{
			foreach (var template in templates)
			{
				await emailService.InstallNow(template);
			}
		});

	}
}
