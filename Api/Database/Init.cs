using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using System;
using System.Threading.Tasks;

namespace Api.Database;

/// <summary>
/// Instanced automatically. Inits the main caches.
/// </summary>
[EventListener]
public class Init
{
	
	/// <summary>
	/// Instanced automatically. Inits the main caches.
	/// </summary>
	public Init()
	{
		var setupHandlersMethod = GetType().GetMethod(nameof(SetupService));

		Events.Service.AfterCreate.AddEventListener((Context context, AutoService service) => {

			if (service == null || service.ServicedType == null)
			{
				return new ValueTask<AutoService>(service);
			}

			var servicedType = service.ServicedType;

			if (servicedType != null)
			{
				// Add data load events:
				var setupType = setupHandlersMethod.MakeGenericMethod(new Type[] {
						servicedType,
						service.IdType
					});

				setupType.Invoke(this, new object[] {
					service
				});
			}

			return new ValueTask<AutoService>(service);
		}, 1);

		Events.Service.AfterCreate.AddEventListener(async (Context context, AutoService service) => {

			if (service == null || service.ServicedType == null)
			{
				return service;
			}

			// Service can now attempt to load its cache:
			await service.SetupCacheIfNeeded();

			return service;
		}, 3);
	}

	/// <summary>
	/// Sets up for the given type with its event group along with updating any DB tables.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	/// <param name="service"></param>
	public void SetupService<T, ID>(AutoService<T, ID> service)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		// Special case if it is a mapping type.
		var entityName = service.EntityName;
		var isDbStored = service.DataIsPersistent;

		service.EventGroup.Delete.AddEventListener((Context context, T result) =>
		{
			if (result == null)
			{
				return new ValueTask<T>((T)null);
			}

			// Remove from the cache:
			var cache = service.GetCache();

			if (cache != null)
			{
				cache.Remove(context, result.Id);
			}

			return new ValueTask<T>(result);
		}, 100);

		service.EventGroup.Update.AddEventListener((Context context, T entity, ChangedFields changes, DataOptions opts) =>
		{
			if (entity == null)
			{
				return new ValueTask<T>(entity);
			}

			// Cache update.
			var locale = context == null ? 1 : context.LocaleId;
			var cache = service.GetCache();

			if (cache == null)
			{
				return new ValueTask<T>(entity);
			}

			// Copy fields from entity -> orig.
			var orig = cache.Get(entity.Id);

			// Anything that makes an assumption that the object doesn't change can continue with that assumption.
			service.CloneEntityInto(entity, orig);

			cache.Add(context, orig);

			return new ValueTask<T>(entity);
		}, 100);

		service.EventGroup.Load.AddEventListener((Context context, T item, ID id) =>
		{
			if (item != null)
			{
				return new ValueTask<T>(item);
			}

			// Load from cache if there is one.
			var cache = service.GetCache();

			if (cache != null)
			{
				item = cache.Get(id);
			}

			return new ValueTask<T>(item);
		}, 5);

		service.EventGroup.CreatePartial.AddEventListener((Context context, T newEntity) =>
		{
			// If this is a cached type, add it to cache.
			var cache = service.GetCache();

			if (cache != null)
			{
				cache.Add(context, newEntity);
			}

			return new ValueTask<T>(newEntity);
		});

		service.EventGroup.List.AddEventListener(async (Context context, QueryPair<T, ID> queryPair) =>
		{

			if (queryPair.Handled)
			{
				return queryPair;
			}

			// Do we have a cache?
			var cache = (queryPair.QueryA.DataOptions & DataOptions.CacheFlag) == DataOptions.CacheFlag ? service.GetCache() : null;

			if (cache != null)
			{
				queryPair.Handled = true;

				// Great - we're using the cache:
				queryPair.Total = await cache.GetResults(context, queryPair, queryPair.OnResult, queryPair.SrcA, queryPair.SrcB);
			}

			return queryPair;
		}, 5);

	}

}