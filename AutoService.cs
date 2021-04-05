using Api.Contexts;
using Api.Database;
using Api.DatabaseDiff;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
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
	where T : class, IHaveId<uint>, new()
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
	/// Default with the permission system active.
	/// </summary>
	Default = 1,
	/// <summary>
	/// Permissions will be disabled on this request for data. I hope you know what you're doing! 
	/// As a general piece of guidance, using this is fine if the data that you obtain is not returned directly to the end user.
	/// For example, the end user will likely be denied the ability to search users by email, but the login system needs to be able to do that.
	/// It's ok to ignore the permission engine given we're not just outright returning the user data unless the login is valid.
	/// </summary>
	IgnorePermissions = 0
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
	where T: class, IHaveId<ID>, new()
	where ID: struct, IConvertible
{
	
	/// <summary>
	/// A query which deletes 1 entity.
	/// </summary>
	protected readonly Query<T> deleteQuery;

	/// <summary>
	/// A query which creates 1 entity.
	/// </summary>
	protected readonly Query<T> createQuery;
	
	/// <summary>
	/// A query which creates 1 entity with a known ID.
	/// </summary>
	protected readonly Query<T> createWithIdQuery;

	/// <summary>
	/// A query which selects 1 entity.
	/// </summary>
	protected readonly Query<T> selectQuery;

	/// <summary>
	/// A query which lists multiple entities.
	/// </summary>
	protected readonly Query<T> listQuery;
	
	/// <summary>
	/// A query which lists multiple raw entities.
	/// </summary>
	protected readonly Query<T> listRawQuery;

	/// <summary>
	/// A query which updates 1 entity.
	/// </summary>
	protected readonly Query<T> updateQuery;

	/// <summary>
	/// The set of update/ delete/ create etc events for this type.
	/// </summary>
	public EventGroup<T, ID> EventGroup;
	
	/// <summary>
	/// Sets up the common service type fields.
	/// </summary>
	public AutoService(EventGroup eventGroup) : base(typeof(T), typeof(ID))
	{
		// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
		// whilst also using a high-level abstraction as another plugin entry point.
		deleteQuery = Query.Delete<T>();
		createQuery = Query.Insert<T>();
		createWithIdQuery = Query.Insert<T>(true);
		updateQuery = Query.Update<T>();
		selectQuery = Query.Select<T>();
		listQuery = Query.List<T>();
		listRawQuery = Query.List<T>();
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
	private JsonStructure<T>[] _jsonStructures = null;

	/// <summary>
	/// Gets a particular metadata field by its name. Common ones are "title" and "description".
	/// Use this to generically read common descriptive things about a given content type.
	/// Note that as fields vary by role, it is possible for users of different roles to obtain different meta values.
	/// </summary>
	public async ValueTask<JsonField<T>> GetTypedMetaField(Context context, string fieldName)
	{
		// If a context is not given, this will use anonymous role structure:
		var structure = await GetTypedJsonStructure(context == null ? 0 : context.RoleId);
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
	public override async ValueTask<JsonStructure> GetJsonStructure(uint roleId)
	{
		return await GetTypedJsonStructure(roleId);
	}

	/// <summary>
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public async ValueTask<JsonStructure<T>> GetTypedJsonStructure(uint roleId)
	{
		if(_jsonStructures == null)
		{
			_jsonStructures = new JsonStructure<T>[Roles.All.Length];
		}
		
		if(roleId < 0 || roleId >= Roles.All.Length)
		{
			// Bad role ID.
			return null;
		}
		
		var structure = _jsonStructures[roleId];
		
		if(structure == null)
		{
			// Not built yet. Build it now:
			_jsonStructures[roleId] = structure = new JsonStructure<T>(Roles.All[roleId]);
			await structure.Build(EventGroup.BeforeSettable);
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
		context.IgnorePermissions = options == DataOptions.IgnorePermissions;
		result = await EventGroup.BeforeDelete.Dispatch(context, result);
		context.IgnorePermissions = previousPermState;

		if (result == null)
		{
			return false;
		}

		// Delete the entry:
		if (_database != null)
		{
			await _database.Run(context, deleteQuery, result.GetId());
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
	/// List a filtered set of entities along with the total number of (unpaginated) results.
	/// </summary>
	/// <returns></returns>
	public virtual async ValueTask<ListWithTotal<T>> ListWithTotal(Context context, Filter<T> filter, DataOptions options = DataOptions.Default)
	{
		if (NestableAddMask != 0 && (context.NestedTypes & NestableAddMask) == NestableAddMask)
		{
			// This happens when we're nesting List calls.
			// For example, a User has Tags which in turn have a (creator) User.
			return new ListWithTotal<T>()
			{
				Results = new List<T>(),
				Total = 0
			};
		}

		// Ignoring the permissions only needs to occur on Before.
		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = options == DataOptions.IgnorePermissions;
		context.NestedTypes |= NestableAddMask;
		filter = await EventGroup.BeforeList.Dispatch(context, filter);
		context.NestedTypes &= NestableRemoveMask;
		context.IgnorePermissions = previousPermState;

		// If the filter doesn't have join or group by nodes, we can potentially run it through the cache.
		var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

		ListWithTotal<T> listAndTotal;

		if (cache != null)
		{
			List<ResolvedValue> values = null;

			if (filter != null)
			{
				values = await filter.ResolveValues(context);
			}
			listAndTotal = cache.ListWithTotal(filter, values);
		}
		else if (_database == null)
		{
			listAndTotal = new ListWithTotal<T>
			{
				Results = new List<T>()
			};
		}
		else
		{
			listAndTotal = await _database.ListWithTotal(context, listQuery, filter);
		}
		
		context.NestedTypes |= NestableAddMask;
		listAndTotal.Results = await EventGroup.AfterList.Dispatch(context, listAndTotal.Results);
		context.NestedTypes &= NestableRemoveMask;
		return listAndTotal;
	}

	/// <summary>
	/// List a filtered set of entities.
	/// </summary>
	/// <returns></returns>
	public virtual async ValueTask<List<T>> List(Context context, Filter<T> filter, DataOptions options = DataOptions.Default)
	{
		if(NestableAddMask!=0 && (context.NestedTypes & NestableAddMask) == NestableAddMask){
			// This happens when we're nesting List calls.
			// For example, a User has Tags which in turn have a (creator) User.
			return new List<T>();
		}

		// Ignoring the permissions only needs to occur on Before.
		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = options == DataOptions.IgnorePermissions;
		context.NestedTypes |= NestableAddMask;
		filter = await EventGroup.BeforeList.Dispatch(context, filter);
		context.NestedTypes &= NestableRemoveMask;
		context.IgnorePermissions = previousPermState;

		// If the filter doesn't have join or group by nodes, we can potentially run it through the cache.
		var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

		List<T> list;

		if (cache != null)
		{
			List<ResolvedValue> values = null;

			if (filter != null)
			{
				values = await filter.ResolveValues(context);
			}

			list = cache.List(filter, values, out _);
		}
		else if (_database == null)
		{
			list = new List<T>();
		}
		else
		{
			list = await _database.List(context, listQuery, filter);
		}
		
		context.NestedTypes |= NestableAddMask;
		list = await EventGroup.AfterList.Dispatch(context, list);
		context.NestedTypes &= NestableRemoveMask;
		return list;
	}

	/// <summary>
	/// List a filtered set of entities without using the cache.
	/// </summary>
	/// <returns></returns>
	public virtual async ValueTask<List<T>> ListNoCache(Context context, Filter<T> filter, bool raw = false, DataOptions options = DataOptions.Default)
	{
		if (_database == null)
		{
			// Non database backed services start empty.
			return new List<T>();
		}

		if (NestableAddMask != 0 && (context.NestedTypes & NestableAddMask) == NestableAddMask)
		{
			// This happens when we're nesting List calls.
			// For example, a User has Tags which in turn have a (creator) User.
			return new List<T>();
		}

		// Ignoring the permissions only needs to occur on Before.
		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = options == DataOptions.IgnorePermissions;
		context.NestedTypes |= NestableAddMask;
		filter = await EventGroup.BeforeList.Dispatch(context, filter);
		context.NestedTypes &= NestableRemoveMask;
		context.IgnorePermissions = previousPermState;

		var list = await _database.List(context, raw? listRawQuery : listQuery, filter);

		context.NestedTypes |= NestableAddMask;
		list = await EventGroup.AfterList.Dispatch(context, list);
		context.NestedTypes &= NestableRemoveMask;
		return list;
	}

	/// <summary>
	/// Gets an object from this service. Generally use Get instead with a fixed type.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="fieldName"></param>
	/// <param name="fieldValue"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public override async ValueTask<object> GetObject(Context context, string fieldName, object fieldValue, DataOptions options = DataOptions.Default)
	{
		var filter = new Filter<T>();
		filter.EqualsField(fieldName, fieldValue);
		filter.PageSize = 1;
		var results = await List(context, filter, options);
		if (results == null || results.Count == 0)
		{
			return null;
		}
		return results[0];
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
	/// Updates an object in this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="content"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public override async ValueTask<object> UpdateObject(Context context, object content, DataOptions options = DataOptions.Default)
	{
		return await Update(context, content as T, options);
	}

	/// <summary>
	/// Gets an object from this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="ids"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public override async ValueTask<IEnumerable> ListObjects(Context context, IEnumerable<uint> ids, DataOptions options = DataOptions.Default)
	{
		if (ids == null || !ids.Any())
		{
			return null;
		}

		// Get the list:
		var results = await List(context, new Filter<T>().Id(ids), options);

		return results;
	}

	/// <summary>
	/// Gets objects from this service using a generic serialized filter. Use List instead whenever possible.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filterJson"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public override async ValueTask<ListWithTotal> ListObjects(Context context, string filterJson, DataOptions options = DataOptions.Default)
	{
		var filters = Newtonsoft.Json.JsonConvert.DeserializeObject(filterJson) as JObject;
		var filter = new Filter<T>(filters);

		ListWithTotal<T> response;

		if (filter.PageSize != 0 && filters != null && filters["includeTotal"] != null)
		{
			// Get the total number of non-paginated results as well:
			response = await ListWithTotal(context, filter, options);
		}
		else
		{
			// Not paginated or requestor doesn't care about the total.
			var results = await List(context, filter, options);

			response = new ListWithTotal<T>()
			{
				Results = results
			};

			if (filter.PageSize == 0)
			{
				// Trivial instance - pagination is off so the total is just the result set length.
				response.Total = results == null ? 0 : results.Count;
			}
		}

		return response;
	}

	/// <summary>
	/// Gets a single entity by its ID.
	/// </summary>
	public virtual async ValueTask<T> Get(Context context, ID id, DataOptions options = DataOptions.Default)
	{
		if (NestableAddMask != 0 && (context.NestedTypes & NestableAddMask) == NestableAddMask)
		{
			// This happens when we're nesting Get calls.
			// For example, a User has Tags which in turn have a (creator) User.
			return null;
		}

		// Note: Get is unique in that its permission check happens in the After event handler.
		context.NestedTypes |= NestableAddMask;
		id = await EventGroup.BeforeLoad.Dispatch(context, id);
		context.NestedTypes &= NestableRemoveMask;
		
		T item = null;

		var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

		if (cache != null)
		{
			item = cache.Get(id);
		}

		if (item == null && _database != null)
		{
			item = await _database.Select(context, selectQuery, id);
		}
		
		// Ignoring the permissions only needs to occur on *After* for Gets.
		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = options == DataOptions.IgnorePermissions;
		context.NestedTypes |= NestableAddMask;
		item = await EventGroup.AfterLoad.Dispatch(context, item);
		context.NestedTypes &= NestableRemoveMask;
		context.IgnorePermissions = previousPermState;
		
		return item;
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
		context.IgnorePermissions = options == DataOptions.IgnorePermissions;
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
				await _database.Run(context, createWithIdQuery, entity);
			}
			else if (!await _database.Run(context, createQuery, entity))
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
				await _database.Run(context, deleteQuery, deletedId);
			}
			else if (mode == 'U' || mode == 'C')
			{
				// Updated or created
				// In both cases, check if it exists first:
				var id = raw.GetId();
				if (await _database.Select(context, selectQuery, id) != null)
				{
					// Update
					await _database.Run(context, updateQuery, raw, id);
				}
				else
				{
					// Create
					await _database.Run(context, createWithIdQuery, raw);
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
	/// Updates the given entity.
	/// </summary>
	public virtual async ValueTask<T> Update(Context context, T entity, DataOptions options = DataOptions.Default)
	{
		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = options == DataOptions.IgnorePermissions;
		entity = await EventGroup.BeforeUpdate.Dispatch(context, entity);
		context.IgnorePermissions = previousPermState;

		if (entity == null)
		{
			return null;
		}

		var id = entity.GetId();

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
				primaryEntity = await Get(new Context() {
					LocaleId = 1,
					UserId = context.UserId
				}, id);
			}
			else
			{
				primaryEntity = GetCacheForLocale(1).Get(id);
			}

			PopulateRawEntityFromTarget(raw, entity, primaryEntity);
		}

		if (_database != null && !await _database.Run(context, updateQuery, raw, id))
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
public partial class AutoService : IHaveId<uint>
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
	/// The add mask to use for a nestable service.
	/// Nested services essentially automatically block infinite recursion when loading data.
	/// </summary>
	public ulong NestableAddMask = 0;

	/// <summary>
	/// The remove mask to use for a nestable service.
	/// Nested services essentially automatically block infinite recursion when loading data.
	/// </summary>
	public ulong NestableRemoveMask = ulong.MaxValue;

	/// <summary>
	/// True if this service is 'nestable' meaning it can be used during a List event in some other service.
	/// Nested services essentially automatically block infinite recursion when loading data.
	/// </summary>
	public bool IsNestable
	{
		get
		{
			return NestableAddMask != 0;
		}
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
	/// Creates a new AutoService.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="idType"></param>
	public AutoService(Type type = null, Type idType = null)
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
		var json = await GetJsonStructure(context == null ? 0 : context.RoleId);

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
	public virtual ValueTask<JsonStructure> GetJsonStructure(uint roleId)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Updates an object in this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="content"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public virtual ValueTask<object> UpdateObject(Context context, object content, DataOptions options = DataOptions.Default)
	{
		return new ValueTask<object>(null);
	}

	/// <summary>
	/// Gets an object from this service which matches the given particular field/value. If multiple match, it's only ever the first one.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="fieldName"></param>
	/// <param name="fieldValue"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public virtual ValueTask<object> GetObject(Context context, string fieldName, object fieldValue, DataOptions options = DataOptions.Default)
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
	/// Gets an list of objects from this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="ids"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public virtual ValueTask<IEnumerable> ListObjects(Context context, IEnumerable<uint> ids, DataOptions options = DataOptions.Default)
	{
		return new ValueTask<IEnumerable>((IEnumerable)null);
	}

	/// <summary>
	/// Gets objects from this service using a generic serialized filter. Use List instead whenever possible.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="filterJson"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public virtual ValueTask<ListWithTotal> ListObjects(Context context, string filterJson, DataOptions options = DataOptions.Default)
	{
		return new ValueTask<ListWithTotal>((ListWithTotal)null);
	}

	/// <summary>
	/// Makes this service type 'nestable'.
	/// </summary>
	public void MakeNestable()
	{
		if (IsNestable)
		{
			return;
		}

		var id = Api.Startup.AutoServiceNesting.TypeId++;

		NestableAddMask = (ulong)1 << id;
		NestableRemoveMask = ~NestableAddMask;
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
	protected void InstallRoles(params UserRole[] roles)
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
	
	private static void InstallRolesInternal(UserRole[] roles)
	{
		var roleService = Services.Get<UserRoleService>();

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
	
	/// <summary>
	/// Defines a new IHave* interface.
	/// It's added to content types to declare they have e.g. an array of tags, categories etc.
	/// This one specifically is where the mapping type is also the array entries.
	/// </summary>
	protected IHaveArrayHandler<T, U, U> DefineIHaveArrayHandler<T, U>(Action<T, List<U>> setResult, bool retainOrder = false)
		where T : class
		where U : MappingEntity, new()
	{
		return DefineIHaveArrayHandler<T, U, U>(null, null, setResult, retainOrder);
	}
	
	/// <summary>
	/// Defines a new IHave* interface.
	/// It's added to content types to declare they have e.g. an array of tags, categories etc.
	/// </summary>
	protected IHaveArrayHandler<T, U, M> DefineIHaveArrayHandler<T, U, M>(string whereFieldName, string mapperFieldName, Action<T, List<U>> setResult, bool retainOrder = false)
		where T : class
		where U : Content<uint>, new()
		where M : MappingEntity, new()
	{
		IHaveArrayHandler<T, U, M> mapper;

		if (typeof(U) == typeof(M))
		{
			mapper = new IHaveArrayHandler<T, M>()
			{
				WhereFieldName = whereFieldName,
				MapperFieldName = mapperFieldName,
				OnSetResult = setResult as Action<T, List<M>>,
				Database = _database,
				RetainOrder = retainOrder
			} as IHaveArrayHandler<T, U, M>;
		}
		else
		{
			mapper = new IHaveArrayHandler<T, U, M>()
			{
				WhereFieldName = whereFieldName,
				MapperFieldName = mapperFieldName,
				OnSetResult = setResult,
				Database = _database,
				RetainOrder = retainOrder
			};
		}

		mapper.Map();

		return mapper;
	}
	
	/// <summary>
	/// Defines a new IHave* interface.
	/// It's added to content types to declare they have e.g. an array of tags, categories etc.
	/// </summary>
	protected IHaveArrayHandler<T, Api.Users.User, M> DefineIHaveArrayHandler<T, M>(string whereFieldName, Action<T, List<Api.Users.UserProfile>> setResult, bool retainOrder = false)
		where T : class
		where M : MappingEntity, new()
	{
		Api.Users.UserService _users = null;

		var mapper = new IHaveArrayHandler<T, Api.Users.User, M>() {
			WhereFieldName = whereFieldName,
			MapperFieldName = "UserId",
			OnSetResult = (T content, List<Api.Users.User> users) => {

				if (users == null)
				{
					setResult(content, null);
					return;
				}

				if (_users == null)
				{
					_users = Services.Get<Api.Users.UserService>();
				}

				// Map users to a UserProfile list:
				var set = new List<Api.Users.UserProfile>();

				foreach (var user in users)
				{
					set.Add(_users.GetProfile(user));
				}

				setResult(content, set);
			},
			Database = _database,
			RetainOrder = retainOrder
		};

		mapper.Map();

		return mapper;
	}

	/// <summary>
	/// The ID of the service itself.
	/// </summary>
	/// <returns></returns>
	public uint GetId()
	{
		if (ServicedType != null)
		{
			return (uint)ContentTypes.GetId(ServicedType);
		}

		throw new NotImplementedException("Typeless services don't have an ID. You probably meant to use something else, like Get.");
	}

	/// <summary>
	/// The ID of the service itself.
	/// </summary>
	/// <param name="id"></param>
	public void SetId(uint id)
	{
		throw new NotImplementedException("This is readonly");
	}
}

namespace Api.Startup {

	/// <summary>
	/// Used to help nestable AutoServices.
	/// </summary>
	public static class AutoServiceNesting {

		/// <summary>
		/// All nestable AutoServices are given an ID. It's not stored in the database so it's assigned at runtime as the services start.
		/// This tracks the number assigned so far.
		/// </summary>
		public static int TypeId;
	}

}
