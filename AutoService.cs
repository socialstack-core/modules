using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class AutoService<T> : AutoService where T: DatabaseRow, new(){
	
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
	}
	
	private JsonStructure<T>[] _jsonStructures = null;

	/// <summary>
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public override async Task<JsonStructure> GetJsonStructure(int roleId)
	{
		return await GetTypedJsonStructure(roleId);
	}

	/// <summary>
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public async Task<JsonStructure<T>> GetTypedJsonStructure(int roleId)
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
	/// Optionally includes uploaded content refs in there too.
	/// </summary>
	/// <returns></returns>
	public virtual async Task<bool> Delete(Context context, int id)
	{
		// Delete the entry:
		await _database.Run(context, deleteQuery, id);

		var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

		if (cache != null)
		{
			cache.Remove(context, id);
		}

		// Ok!
		return true;
	}

	/// <summary>
	/// List a filtered set of entities along with the total number of (unpaginated) results.
	/// </summary>
	/// <returns></returns>
	public virtual async Task<ListWithTotal<T>> ListWithTotal(Context context, Filter<T> filter)
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

		var listAndTotal = await _database.ListWithTotal(context, listQuery, filter);

		context.NestedTypes |= NestableAddMask;
		listAndTotal.Results = await EventGroup.AfterList.Dispatch(context, listAndTotal.Results);
		context.NestedTypes &= NestableRemoveMask;
		return listAndTotal;
	}

	/// <summary>
	/// List a filtered set of entities.
	/// </summary>
	/// <returns></returns>
	public virtual async Task<List<T>> List(Context context, Filter<T> filter)
	{
		if(NestableAddMask!=0 && (context.NestedTypes & NestableAddMask) == NestableAddMask){
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
	public override async Task<object> GetObject(Context context, int id)
	{
		return await Get(context, id);
	}

	/// <summary>
	/// Gets a single entity by its ID.
	/// </summary>
	public virtual async Task<T> Get(Context context, int id)
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

		if (item == null)
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
	public virtual Task<T> Create(Context context, T entity)
	{
		return Create(context, entity, null);
	}
	
	/// <summary>
	/// Creates a new entity.
	/// </summary>
	public virtual async Task<T> Create(Context context, T entity, Action<Context, T> postIdCallback)
	{
		entity = await EventGroup.BeforeCreate.Dispatch(context, entity);

		// Note: The Id field is automatically updated by Run here.
		if (entity == null)
		{
			return entity;
		}

		if (entity.Id != 0)
		{
			// Explicit ID has been provided.
			await _database.Run(context, createWithIdQuery, entity);
		}
		else if (!await _database.Run(context, createQuery, entity))
		{
			return default(T);
		}

		postIdCallback?.Invoke(context, entity);

		var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

		if (cache != null)
		{
			cache.Add(context, entity);
		}

		entity = await EventGroup.AfterCreate.Dispatch(context, entity);
		return entity;
	}

	/// <summary>
	/// Updates the given entity.
	/// </summary>
	public virtual async Task<T> Update(Context context, T entity)
	{
		entity = await EventGroup.BeforeUpdate.Dispatch(context, entity);

		if (entity == null || !await _database.Run(context, updateQuery, entity, entity.Id))
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
public class AutoService
{
	/// <summary>
	/// The type that this AutoService is servicing. E.g. a User, ForumPost etc.
	/// </summary>
	public Type ServicedType;

	/// <summary>
	/// The database service.
	/// </summary>
	protected IDatabaseService _database;

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
	/// Creates a new AutoService.
	/// </summary>
	/// <param name="type"></param>
	public AutoService(Type type)
	{
		_database = Api.Startup.Services.Get<IDatabaseService>();
		ServicedType = type;
	}

	/// <summary>
	/// Gets the JSON structure. Defines settable fields for a particular role.
	/// </summary>
	public virtual Task<JsonStructure> GetJsonStructure(int roleId)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Gets an object from this service.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	public virtual Task<object> GetObject(Context context, int id)
	{
		return Task.FromResult((object)null);
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
		InstallAdminPages(null, null, fields);
	}

	/// <summary>
	/// Installs generic admin pages for this service, including the nav menu entry.
	/// Does nothing if there isn't a page service installed, or if the admin pages already exist.
	/// </summary>
	/// <param name="navMenuLabel"></param>
	/// <param name="navMenuIconRef"></param>
	/// <param name="fields"></param>
	protected void InstallAdminPages(string navMenuLabel, string navMenuIconRef, string[] fields)
	{
		if (Services.Started)
		{
			InstallAdminPagesInternal(navMenuLabel, navMenuIconRef, fields);
		}
		else
		{
			// Must happen after services start otherwise the page service isn't necessarily available yet.
			Events.ServicesAfterStart.AddEventListener((Context ctx, object src) =>
			{
				InstallAdminPagesInternal(navMenuLabel, navMenuIconRef, fields);
				return Task.FromResult(src);
			});
		}
	}

	private void InstallAdminPagesInternal(string navMenuLabel, string navMenuIconRef, string[] fields)
	{
		var pageService = Api.Startup.Services.Get("IPageService");

		if (pageService == null)
		{
			// No point installing nav menu entries either if there's no pages.
			return;
		}

		Task.Run(async () =>
		{
			var installPages = pageService.GetType().GetMethod("InstallAdminPages");
			var typeName = ServicedType.Name;

			if (installPages != null)
			{
				// InstallAdminPages(string typeName, string[] fields)
				await (Task)installPages.Invoke(pageService, new object[] {
					typeName,
					fields
				});
			}

			// Nav menu also?
			if (navMenuLabel != null)
			{
				var navMenuItemService = Api.Startup.Services.Get("INavMenuItemService");

				if (navMenuItemService != null)
				{
					var installNavMenuEntry = navMenuItemService.GetType().GetMethod("InstallAdminEntry");

					if (installNavMenuEntry != null)
					{
						// InstallAdminEntry(string targetUrl, string iconRef, string label)
						await (Task)installNavMenuEntry.Invoke(navMenuItemService, new object[] {
							"/en-admin/" + typeName.ToLower(),
							navMenuIconRef,
							navMenuLabel
						});
					}
				}
			}
		});

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
