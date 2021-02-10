using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
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
public partial class AutoService<T> : AutoService<T, int>
	where T : class, IHaveId<int>, new()
{
	/// <summary>
	/// Instanced automatically
	/// </summary>
	/// <param name="eventGroup"></param>
	public AutoService(EventGroup<T> eventGroup):base(eventGroup)
	{ }
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
	/// A query which updates 1 entity.
	/// </summary>
	protected readonly Query<T> updateQuery;

	/// <summary>
	/// The set of update/ delete/ create etc events for this type.
	/// </summary>
	public EventGroup<T> EventGroup;
	
	/// <summary>
	/// Sets up the common service type fields.
	/// </summary>
	public AutoService(EventGroup<T> eventGroup) : base(typeof(T))
	{
		// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
		// whilst also using a high-level abstraction as another plugin entry point.
		deleteQuery = Query.Delete<T>();
		createQuery = Query.Insert<T>();
		createWithIdQuery = Query.Insert<T>(true);
		updateQuery = Query.Update<T>();
		selectQuery = Query.Select<T>();
		listQuery = Query.List<T>();

		EventGroup = eventGroup;

		// GetObject specifically uses integer IDs (and is only available on services that have integer IDs)
		// We need to make a delegate that it can use for mapping that integer through to whatever typeof(ID) is.
		if (typeof(ID) == typeof(int))
		{
			var getMethodDelegate = (Func<Context, ID, ValueTask<T>>)Get;
			_getWithIntId = (getMethodDelegate as Func<Context, int, ValueTask<T>>);
		}

	}
	
	private Func<Context, int, ValueTask<T>> _getWithIntId;
	private JsonStructure<T>[] _jsonStructures = null;

	/// <summary>
	/// Reads a particular metadata field by its name. Common ones are "title" and "description".
	/// Use this to generically read common descriptive things about a given content type.
	/// Note that as fields vary by role, it is possible for users of different roles to obtain different meta values.
	/// </summary>
	public async ValueTask<object> GetMetaFieldValue(Context context, string fieldName, T content)
	{
		var field = await GetMetaField(context, fieldName);
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
	/// Gets a particular metadata field by its name. Common ones are "title" and "description".
	/// Use this to generically read common descriptive things about a given content type.
	/// Note that as fields vary by role, it is possible for users of different roles to obtain different meta values.
	/// </summary>
	public async ValueTask<JsonField<T>> GetMetaField(Context context, string fieldName)
	{
		// If a context is not given, this will use anonymous role structure:
		var structure = await GetTypedJsonStructure(context == null ? 0 : context.RoleId);
		if (structure == null)
		{
			return null;
		}

		// Get the field:
		return structure.GetMetaField(fieldName);
	}
		
	/// <summary>
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public override async ValueTask<JsonStructure> GetJsonStructure(int roleId)
	{
		return await GetTypedJsonStructure(roleId);
	}

	/// <summary>
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public async ValueTask<JsonStructure<T>> GetTypedJsonStructure(int roleId)
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
			await structure.Build(EventGroup);
		}
		
		return structure;
	}

	/// <summary>
	/// Deletes an entity by its ID.
	/// </summary>
	/// <returns></returns>
	public virtual async ValueTask<bool> Delete(Context context, ID id)
	{
		var result = await Get(context, id);
		return await Delete(context, result);
	}

	/// <summary>
	/// Deletes an entity.
	/// </summary>
	/// <returns></returns>
	public virtual async ValueTask<bool> Delete(Context context, T result)
	{
		result = await EventGroup.BeforeDelete.Dispatch(context, result);

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
	public virtual async ValueTask<ListWithTotal<T>> ListWithTotal(Context context, Filter<T> filter)
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

		context.NestedTypes |= NestableAddMask;
		filter = await EventGroup.BeforeList.Dispatch(context, filter);
		context.NestedTypes &= NestableRemoveMask;

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
			listAndTotal = new ListWithTotal<T>();
			listAndTotal.Results = new List<T>();
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
	public virtual async ValueTask<List<T>> List(Context context, Filter<T> filter)
	{
		if(NestableAddMask!=0 && (context.NestedTypes & NestableAddMask) == NestableAddMask){
			// This happens when we're nesting List calls.
			// For example, a User has Tags which in turn have a (creator) User.
			return new List<T>();
		}
		context.NestedTypes |= NestableAddMask;
		filter = await EventGroup.BeforeList.Dispatch(context, filter);
		context.NestedTypes &= NestableRemoveMask;
		
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
			list = cache.List(filter, values, out int total);
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
	public virtual async ValueTask<List<T>> ListNoCache(Context context, Filter<T> filter)
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
		context.NestedTypes |= NestableAddMask;
		filter = await EventGroup.BeforeList.Dispatch(context, filter);
		context.NestedTypes &= NestableRemoveMask;
		
		var list = await _database.List(context, listQuery, filter);

		context.NestedTypes |= NestableAddMask;
		list = await EventGroup.AfterList.Dispatch(context, list);
		context.NestedTypes &= NestableRemoveMask;
		return list;
	}

	/// <summary>
	/// Gets an object from this service. Generally use Get instead with a fixed type.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	public override async ValueTask<object> GetObject(Context context, int id)
	{
		if (_getWithIntId == null)
		{
			throw new Exception("Only available on types with integer IDs. " + typeof(T) + " uses an ID which is a " + typeof(ID));
		}

		return await _getWithIntId(context, id);
	}
	
	/// <summary>
	/// Updates an object in this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="content"></param>
	/// <returns></returns>
	public override async ValueTask<object> UpdateObject(Context context, object content)
	{
		return await Update(context, content as T);
	}

	/// <summary>
	/// Gets an object from this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="ids"></param>
	/// <returns></returns>
	public override async ValueTask<IEnumerable> ListObjects(Context context, IEnumerable<int> ids)
	{
		if (ids == null || !ids.Any())
		{
			return null;
		}

		// Get the list:
		var results = await List(context, new Filter<T>().Id(ids));

		return results;
	}
	
	/// <summary>
	/// Gets a single entity by its ID, then performs a permission system check.
	/// Throws if the permission system rejects the call.
	/// </summary>
	public virtual async ValueTask<T> GetIfPermitted(Context context, ID id)
	{
		var result = await Get(context, id);
		if(result == null)
		{
			return result;
		}
		
		await context.Role.IsGranted(GetLoadCapability(), context, result);
		return result;
	}
	
	/// <summary>
	/// Gets a single entity by its ID.
	/// </summary>
	public virtual async ValueTask<T> Get(Context context, ID id)
	{
		if (NestableAddMask != 0 && (context.NestedTypes & NestableAddMask) == NestableAddMask)
		{
			// This happens when we're nesting Get calls.
			// For example, a User has Tags which in turn have a (creator) User.
			return null;
		}

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
		
		context.NestedTypes |= NestableAddMask;
		item = await EventGroup.AfterLoad.Dispatch(context, item);
		context.NestedTypes &= NestableRemoveMask;
		return item;
	}

	/// <summary>
	/// Creates a new entity.
	/// </summary>
	public virtual async ValueTask<T> Create(Context context, T entity)
	{
		entity = await CreatePartial(context, entity);
		return await CreatePartialComplete(context, entity);
	}

	/// <summary>
	/// Creates a new entity but without calling AfterCreate. This allows you to update fields after the ID has been set, but before AfterCreate is called.
	/// You must always call CreatePartialComplete afterwards to trigger the AfterCreate calls.
	/// </summary>
	public virtual async ValueTask<T> CreatePartial(Context context, T entity)
	{
		entity = await EventGroup.BeforeCreate.Dispatch(context, entity);

		// Note: The Id field is automatically updated by Run here.
		if (entity == null)
		{
			return entity;
		}

		if (_database != null)
		{
			if (!entity.GetId().Equals(0))
			{
				// Explicit ID has been provided.
				await _database.Run(context, createWithIdQuery, entity);
			}
			else if (!await _database.Run(context, createQuery, entity))
			{
				return default(T);
			}
		}

		return entity;
	}

	/// <summary>
	/// Updates the database and the cache without triggering any events, based on the provided mode.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="entity"></param>
	/// <param name="mode">U = update, C = Create, D = Delete</param>
	/// <param name="deletedId"></param>
	public async Task RawUpdateEntity(Context context, T entity, char mode, ID deletedId)
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
				var id = entity.GetId();
				if (await _database.Select(context, selectQuery, id) != null)
				{
					// Update
					await _database.Run(context, updateQuery, entity, id);
				}
				else
				{
					// Create
					await _database.Run(context, createWithIdQuery, entity);
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
					// Created
					cache.Add(context, entity);
				}
				else if (mode == 'D')
				{
					// Deleted
					cache.Remove(context, entity.GetId());
				}
			}
		}

	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="context"></param>
	/// <param name="entity"></param>
	/// <returns></returns>
	public virtual async ValueTask<T> CreatePartialComplete(Context context, T entity)
	{
		var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

		if (cache != null)
		{
			cache.Add(context, entity);
		}

		return await EventGroup.AfterCreate.Dispatch(context, entity);
	}

	/// <summary>
	/// Updates the given entity.
	/// </summary>
	public virtual async ValueTask<T> Update(Context context, T entity)
	{
		entity = await EventGroup.BeforeUpdate.Dispatch(context, entity);

		if (entity == null)
		{
			return null;
		}

		if (_database != null && !await _database.Run(context, updateQuery, entity, entity.GetId()))
		{
			return null;
		}

		var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

		if (cache != null)
		{
			cache.Add(context, entity);
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
	/// The type that this AutoService is servicing. E.g. a User, ForumPost etc.
	/// </summary>
	public Type ServicedType;

	/// <summary>
	/// The database service.
	/// </summary>
	protected DatabaseService _database;
	
	/// <summary>
	/// The load capability for this service.
	/// </summary>
	private Capability _loadCapability;
	
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
	/// Gets the load capability.
	/// </summary>
	public Capability GetLoadCapability()
	{
		if(_loadCapability != null)
		{
			return _loadCapability;
		}

		Capabilities.All.TryGetValue(ServicedType.Name.ToLower() + "_load", out _loadCapability);
		return _loadCapability;
	}
	
	/// <summary>
	/// Creates a new AutoService.
	/// </summary>
	/// <param name="type"></param>
	public AutoService(Type type = null)
	{
		// _database is left blank if:
		// * Type is given.
		// * Type does not have DatabaseRow<> in its inheritence hierarchy.
		// This is such that basic services without a type have a convenience database field, 
		// but types that are in-memory only don't attempt to use the database.
		if (type != null && !IsDatabaseRowType(type))
		{
			_database = null;
		}
		else
		{
			_database = Api.Startup.Services.Get<DatabaseService>();
		}

		ServicedType = type;
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
	/// True if the given is a DatabaseRow type (i.e. if it should be persistent or not).
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	private bool IsDatabaseRowType(Type t)
	{
		if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(DatabaseRow<>))
		{
			return true;
		}

		if (t.BaseType == null)
		{
			return false;
		}

		return IsDatabaseRowType(t.BaseType);
	}

	/// <summary>
	/// Sets up the cache on this service. If you're not sure, use Cache instead of this.
	/// </summary>
	/// <returns></returns>
	public virtual Task SetupCacheNow(CacheConfig cfg)
	{
		throw new NotImplementedException();
	}
	
	/// <summary>
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public virtual ValueTask<JsonStructure> GetJsonStructure(int roleId)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Updates an object in this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="content"></param>
	/// <returns></returns>
	public virtual ValueTask<object> UpdateObject(Context context, object content)
	{
		return new ValueTask<object>(null);
	}

	/// <summary>
	/// Gets an object from this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	public virtual ValueTask<object> GetObject(Context context, int id)
	{
		return new ValueTask<object>(null);
	}

	/// <summary>
	/// Gets an list of objects from this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="ids"></param>
	/// <returns></returns>
	public virtual ValueTask<IEnumerable> ListObjects(Context context, IEnumerable<int> ids)
	{
		return new ValueTask<IEnumerable>((IEnumerable)null);
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
		InstallAdminPages(null, null, fields, null, null);
	}

	/// <summary>
	/// Installs generic admin pages for this service, including the nav menu entry.
	/// Does nothing if there isn't a page service installed, or if the admin pages already exist.
	/// </summary>
	/// <param name="navMenuLabel">The text to show on the navmenu.</param>
	/// <param name="navMenuIconRef">The ref for the icon to use on the navmenu. Usually a fontawesome icon, of the form "fa:fa-thing".</param>
	/// <param name="fields">The fields to show in the list of your content type. Usually include at least some sort of name or title.</param>
	protected void InstallAdminPages(string navMenuLabel, string navMenuIconRef, string[] fields)
	{
		InstallAdminPages(navMenuLabel, navMenuIconRef, fields, null, null);
	}

	/// <summary>
	/// Installs generic admin pages for this service, including the nav menu entry.
	/// Does nothing if there isn't a page service installed, or if the admin pages already exist.
	/// </summary>
	/// <param name="navMenuLabel">The text to show on the navmenu.</param>
	/// <param name="navMenuIconRef">The ref for the icon to use on the navmenu. Usually a fontawesome icon, of the form "fa:fa-thing".</param>
	/// <param name="fields">The fields to show in the list of your content type. Usually include at least some sort of name or title.</param>
	/// <param name="childListType">
	/// A shortcut for specifying that your type has some kind of sub-type.
	/// For example, the NavMenu admin page specifies a child type of NavMenuItem, meaning each NavMenu ends up with a list of NavMenuItems.
	/// Used with childListFields.
	/// </param>
	/// <param name="childListFields">
	/// Used with childListType - specifies the fields that'll be visible from the child type in the list on the parent type.
	/// For example, if you'd like each child entry to show its Id and Title fields, specify new string[]{"id", "title"}.
	/// </param>
	protected void InstallAdminPages(string navMenuLabel, string navMenuIconRef, string[] fields, string childListType, string[] childListFields)
	{
		if (Services.Started)
		{
			InstallAdminPagesInternal(navMenuLabel, navMenuIconRef, fields, childListType, childListFields);
		}
		else
		{
			// Must happen after services start otherwise the page service isn't necessarily available yet.
			Events.ServicesAfterStart.AddEventListener((Context ctx, object src) =>
			{
				InstallAdminPagesInternal(navMenuLabel, navMenuIconRef, fields, childListType, childListFields);
				return new ValueTask<object>(src);
			});
		}
	}

	private void InstallAdminPagesInternal(string navMenuLabel, string navMenuIconRef, string[] fields, string childListType, string[] childListFields)
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
					childListType,
					childListFields
				});
			}

			// Nav menu also?
			if (navMenuLabel != null)
			{
				var navMenuItemService = Api.Startup.Services.Get("NavMenuItemService");

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
	/// Defines a new IHave* interface.
	/// It's added to content types to declare they have e.g. an array of tags, categories etc.
	/// This one specifically is where the mapping type is also the array entries.
	/// </summary>
	protected IHaveArrayHandler<T, U, U> DefineIHaveArrayHandler<T, U>(Action<T, List<U>> setResult, bool retainOrder = false)
		where T : class
		where U : MappingRow, new()
	{
		return DefineIHaveArrayHandler<T, U, U>(null, null, setResult, retainOrder);
	}
	
	/// <summary>
	/// Defines a new IHave* interface.
	/// It's added to content types to declare they have e.g. an array of tags, categories etc.
	/// </summary>
	protected IHaveArrayHandler<T, U, M> DefineIHaveArrayHandler<T, U, M>(string whereFieldName, string mapperFieldName, Action<T, List<U>> setResult, bool retainOrder = false)
		where T : class
		where U : DatabaseRow<int>, new()
		where M : MappingRow, new()
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
		where M : MappingRow, new()
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
