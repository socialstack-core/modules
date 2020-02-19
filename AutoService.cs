using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
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
public partial class AutoService<T> where T: DatabaseRow, new(){

	/// <summary>
	/// The database service.
	/// </summary>
	protected IDatabaseService _database;

	/// <summary>
	/// A query which deletes 1 entity.
	/// </summary>
	protected readonly Query<T> deleteQuery;

	/// <summary>
	/// A query which creates 1 entity.
	/// </summary>
	protected readonly Query<T> createQuery;

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
	public AutoService(EventGroup<T> eventGroup) {
		_database = Api.Startup.Services.Get<IDatabaseService>();

		// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
		// whilst also using a high-level abstraction as another plugin entry point.
		deleteQuery = Query.Delete<T>();
		createQuery = Query.Insert<T>();
		updateQuery = Query.Update<T>();
		selectQuery = Query.Select<T>();
		listQuery = Query.List<T>();

		EventGroup = eventGroup;
	}


	/// <summary>
	/// Deletes an entity by its ID.
	/// Optionally includes uploaded content refs in there too.
	/// </summary>
	/// <returns></returns>
	public virtual async Task<bool> Delete(Context context, int id)
	{
		// Delete the entry:
		await _database.Run(deleteQuery, id);

		// Ok!
		return true;
	}

	/// <summary>
	/// List a filtered set of entities.
	/// </summary>
	/// <returns></returns>
	public virtual async Task<List<T>> List(Context context, Filter<T> filter)
	{
		filter = await EventGroup.BeforeList.Dispatch(context, filter);
		var list = await _database.List(listQuery, filter);
		list = await EventGroup.AfterList.Dispatch(context, list);
		return list;
	}

	/// <summary>
	/// Gets a single entity by its ID.
	/// </summary>
	public virtual async Task<T> Get(Context context, int id)
	{
		return await _database.Select(selectQuery, id);
	}

	/// <summary>
	/// Creates a new entity.
	/// </summary>
	public virtual async Task<T> Create(Context context, T entity)
	{
		entity = await EventGroup.BeforeCreate.Dispatch(context, entity);

		// Note: The Id field is automatically updated by Run here.
		if (entity == null || !await _database.Run(createQuery, entity))
		{
			return default(T);
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

		if (entity == null || !await _database.Run(updateQuery, entity, entity.Id))
		{
			return null;
		}

		entity = await EventGroup.AfterUpdate.Dispatch(context, entity);
		return entity;
	}
	
}
