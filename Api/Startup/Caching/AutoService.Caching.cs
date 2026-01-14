using Api.Contexts;
using Api.Startup;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
public partial class AutoService<T, ID> {
	
	/// <summary>
	/// The config for the cache.
	/// </summary>
	protected CacheConfig _cacheConfig;
	/// <summary>
	/// The cache, if enabled. Call Cache() to set this service as one with caching active.
	/// </summary>
	protected ServiceCache<T, ID> _cache;

	/// <summary>
	/// True if a cache is available.
	/// </summary>
	public bool CacheAvailable => _cache != null;

	/// <summary>
	/// The cache config for this service (if any).
	/// </summary>
	/// <returns></returns>
	public CacheConfig GetCacheConfig()
	{
		return _cacheConfig;
	}

	/// <summary>
	/// Gets the index ID of a cache index with the given key name.
	/// </summary>
	/// <param name="keyName"></param>
	/// <returns></returns>
	public int GetCacheIndexId(string keyName)
	{
		if (_cache == null)
		{
			throw new Exception("Can only get a cache index ID on a service with a cache.");
		}

		return _cache.GetIndexId(keyName);
	}

	/// <summary>
	/// Indicates that entities of this service should be cached in memory.
	/// Auto establishes if everything should be loaded now or later.
	/// </summary>
	public override void Cache(CacheConfig cfg = null)
	{
		if (cfg == null)
		{
			// Default config:
			cfg = new CacheConfig();
		}

		_cacheConfig = cfg;
	}

	/// <summary>
	/// Get the cache for this service, if it has one.
	/// </summary>
	/// <returns></returns>
	public ServiceCache<T, ID> GetCache()
	{
		return _cache;
	}

	/// <summary>
	/// Sets up the cache on this service. Use Cache() instead of this - SetupCacheNow is invoked during service startup.
	/// </summary>
	/// <returns></returns>
	public override async ValueTask SetupCacheIfNeeded()
	{
		if (_cacheConfig == null)
		{
			return;
		}

		await PopulateCache(true);

		if (!IsMapping)
		{
			// Log that the cache is on:
			Log.Ok(LogTag, InstanceType.Name + " - cache ready");
		}
	}

	/// <summary>
	/// Reloads a particular cached item.
	/// </summary>
	/// <returns></returns>
	public async ValueTask InvalidateCachedItem(ID id)
	{
		if (_cacheConfig == null)
		{
			throw new PublicException("Not a cached service - no cache to invalidate", "no_cache");
		}

		await InvalidateCachedItems(new List<ID>() { id });
	}
	
	/// <summary>
	/// Invalidates a particular set of cached items.
	/// </summary>
	/// <returns></returns>
	public async ValueTask InvalidateCachedItems(List<ID> ids)
	{
		if (_cacheConfig == null)
		{
			throw new PublicException("Not a cached service - no cache to invalidate", "no_cache");
		}

		var cache = _cache;

		if (cache == null)
		{
			// They weren't cached anyway: we're still going through the startup process.
			return;
		}

		var ctx = new Context();

		// Get the entries (for primary locale, it makes no difference).
		var items = await Where("Id=[?]", DataOptions.NoCacheIgnorePermissions)
			.Bind(ids)
			.ListAll(ctx);

		Dictionary<ID, T> objLookup = null;

		if (ids.Count > 5)
		{
			objLookup = new Dictionary<ID, T>();
			
			// For larger item clusters, we'll use an ID based lookup.
			foreach (var item in items)
			{
				objLookup[item.Id] = item;
			}
		}

		// It's possible that they don't exist anymore in which case
		// the items set is going to not return the target object.
		foreach(var id in ids)
		{
			T currentEntity = null;

			if (objLookup != null)
			{
				objLookup.TryGetValue(id, out currentEntity);
			}
			else
			{
				foreach (var item in items)
				{
					if (item.Id.Equals(id))
					{
						currentEntity = item;
						break;
					}
				}
			}

			if (currentEntity == null)
			{
				// It's been deleted - remove from the cache
				cache.Remove(ctx, id);
			}
			else
			{
				// Update the cached entity.
				cache.Add(ctx, currentEntity);
			}
		}
	}


	/// <summary>
	/// Reloads the whole cache from the db.
	/// </summary>
	/// <returns></returns>
	public async ValueTask InvalidateCache()
	{
		if (_cacheConfig == null)
		{
			throw new PublicException("Not a cached service - no cache to reload", "no_cache");
		}

		await PopulateCache(false);
	}

	/// <summary>
	/// Apply an existing cache set to this service. If you apply a null set, it will initialise an empty cache.
	/// </summary>
	/// <param name="cache"></param>
	public async override ValueTask ApplyCache(ServiceCache cache)
	{
		// It must be of the correct type:
		var typed = cache as ServiceCache<T, ID>;

		if (typed == null)
		{
			throw new Exception("Incorrect cache type. It must be a '" + typeof(ServiceCache<T, ID>).Name + "' but was given a " + 
				(cache == null ? "(null cache set)" : cache.GetType().Name));
		}

		_cache = typed;

		// Set OnChange handlers:
		var genericCfg = _cacheConfig as CacheConfig<T>;

		_cache.OnChange = genericCfg?.OnChange;

		if (_cacheConfig != null && _cacheConfig.OnCacheLoaded != null)
		{
			await _cacheConfig.OnCacheLoaded();
		}
	}

	/// <summary>
	/// Populates the cache.
	/// </summary>
	/// <param name="stopIfAlreadyPopulated"></param>
	/// <returns></returns>
	public async ValueTask PopulateCache(bool stopIfAlreadyPopulated = false)
	{
		if (stopIfAlreadyPopulated && _cache != null)
		{
			// Cache setup elsewhere.
			return;
		}

		var genericCfg = _cacheConfig as CacheConfig<T>;

		var indices = GetContentFields().IndexList;

		// Get everything
		var ctx = new Context();
		var everything = await Where(DataOptions.NoCacheIgnorePermissions).ListAll(ctx);
		var cache = new ServiceCache<T, ID>(indices);

		foreach (var entity in everything)
		{
			cache.Add(ctx, entity);
		}

		_cache = cache;

		if (_cacheConfig.OnCacheLoaded != null)
		{
			await _cacheConfig.OnCacheLoaded();
		}
	}
}

public partial class AutoService
{
	/// <summary>
	/// Apply an existing cache to this service.
	/// </summary>
	/// <param name="cache"></param>
	public virtual ValueTask ApplyCache(ServiceCache cache)
	{
		// Overriden by AutoService<T, ID>
		return new ValueTask();
	}
}