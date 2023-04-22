using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Translate;
using Api.ColourConsole;

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
	/// The caches, if enabled. Call Cache() to set this service as one with caching active.
	/// It's an array as there's one per locale.
	/// </summary>
	protected CacheSet<T, ID> _cacheSet;

	/// <summary>
	/// True if a cache is available.
	/// </summary>
	public bool CacheAvailable => _cacheSet != null;

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
		if (_cacheSet == null || _cacheSet.Caches[0] == null)
		{
			throw new Exception("Can only get a cache index ID on a service with a cache.");
		}

		return _cacheSet.Caches[0].GetIndexId(keyName);
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

		var alreadyWaiting = (_cacheConfig != null);

		_cacheConfig = cfg;
		Synced = cfg == null || cfg.ClusterSync;
	}

	/// <summary>
	/// Gets a cache for a given locale ID. Null if none.
	/// </summary>
	/// <param name="localeId"></param>
	/// <returns></returns>
	public ServiceCache<T, ID> GetCacheForLocale(uint localeId)
	{
		if (_cacheSet == null || localeId <= 0 || localeId > _cacheSet.Length)
		{
			return null;
		}
		return _cacheSet.Caches[localeId - 1];
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

		if (!IsMapping)
		{
            // Log that the cache is on:
            WriteColourLine.Success(InstanceType.Name + " - cache [on]");
		}

		await SetupCache();
	}

	/*
	/// <summary>
	/// Dumps some metadata about the cache
	/// </summary>
	/// <returns></returns>
	public async ValueTask DumpCacheMeta(Microsoft.AspNetCore.Http.HttpResponse response)
	{

	}
	*/

	/// <summary>
	/// Sets up the cache.
	/// </summary>
	/// <returns></returns>
	public async ValueTask Recache()
	{
		if (_cacheConfig == null)
		{
			throw new PublicException("Not a cached service - no cache to reload", "no_cache");
		}

		await SetupCache();
	}

	/// <summary>
	/// Apply an existing cache set to this service. If you apply a null set, it will initialise an empty cache.
	/// </summary>
	/// <param name="set"></param>
	public async override ValueTask ApplyCache(CacheSet set)
	{
		if (_contentFields == null)
		{
			SetContentFields(set.ContentFields);
		}

		// It must be of the correct type:
		var typedSet = set as CacheSet<T, ID>;

		if (typedSet == null)
		{
			throw new Exception("Incorrect cache set type. It must be a '" + typeof(CacheSet<T, ID>).Name + "' but was given a " + (set == null ? "(null cache set)" : set.GetType().Name));
		}

		if (typedSet != null)
		{
			_cacheSet = typedSet;
		}

		// Set OnChange handlers:
		var genericCfg = _cacheConfig as CacheConfig<T>;

		_cacheSet.SetOnChange(genericCfg?.OnChange);

		if (_cacheConfig != null && _cacheConfig.OnCacheLoaded != null)
		{
			await _cacheConfig.OnCacheLoaded();
		}
	}

	private async ValueTask SetupCache()
	{
		if (_cacheSet != null)
		{
			// Cache setup elsewhere.
			return;
		}

		var genericCfg = _cacheConfig as CacheConfig<T>;

		var indices = GetContentFields().IndexList;

		var localeSet = ContentTypes.Locales;

		if (localeSet == null)
		{
			// Likely never happens, but just in case.
			localeSet = new Api.Translate.Locale[] {
				new Locale(){
					Id = 1,
					Name = "English",
					Code = "en"
				}
			};
		}

		_cacheSet = new CacheSet<T, ID>(GetContentFields(), EntityName);

		if (localeSet.Length == 0)
		{
			return;
		}

		for (var i = 0; i < localeSet.Length; i++)
		{
			var locale = localeSet[i];

			if (locale == null)
			{
				// Happens if there's gaps in IDs (because a locale was deleted for ex).
				continue;
			}

			_cacheSet.RequireCacheForLocale(locale.Id);
		}

		var primaryLocaleCache = _cacheSet.Caches[0];

		// Get everything, for each supported locale:
		for (var i = 0; i < localeSet.Length; i++)
		{
			var locale = localeSet[i];

			if (locale == null)
			{
				continue;
			}

			var cache = _cacheSet.Caches[i];

			var ctx = new Context()
			{
				LocaleId = locale.Id
			};

			// Get the *raw* entries (for primary locale, it makes no difference).
			var everything = await Where(DataOptions.NoCacheIgnorePermissions | DataOptions.RawFlag).ListAll(ctx);

			foreach (var raw in everything)
			{
				if (i == 0)
				{
					// Primary - raw and target are the same object.
					cache.Add(ctx, raw, raw);
				}
				else
				{
					// Secondary locale. The target object is a clone of the raw object, 
					// but then with any unset fields from the primary locale.
					var entity = (T)Activator.CreateInstance(InstanceType);

					PopulateTargetEntityFromRaw(entity, raw, primaryLocaleCache.Get(raw.GetId()));

					cache.Add(ctx, entity, raw);
				}

				
			}
		}

		if (_cacheConfig.OnCacheLoaded != null)
		{
			await _cacheConfig.OnCacheLoaded();
		}
	}
}

public partial class AutoService
{
	/// <summary>
	/// Apply an existing cache set to this service.
	/// </summary>
	/// <param name="set"></param>
	public virtual ValueTask ApplyCache(CacheSet set)
	{
		// Overriden by AutoService<T, ID>
		return new ValueTask();
	}
}