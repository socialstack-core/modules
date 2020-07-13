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

/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class AutoService<T> : AutoService where T: DatabaseRow, new(){
	
	/// <summary>
	/// The caches, if enabled. Call Cache() to set this service as one with caching active.
	/// It's an array as there's one per locale.
	/// </summary>
	protected ServiceCache<T>[] _cache;

	/// <summary>
	/// Gets the index ID of a cache index with the given key name.
	/// </summary>
	/// <param name="keyName"></param>
	/// <returns></returns>
	public int GetCacheIndexId(string keyName)
	{
		if (_cache == null || _cache.Length == 0 || _cache[0] == null)
		{
			throw new Exception("Can only get a cache index ID on a service with a cache.");
		}

		return _cache[0].GetIndexId(keyName);
	}

	/// <summary>
	/// Indicates that entities of this service should be cached in memory.
	/// Auto establishes if everything should be loaded now or later.
	/// </summary>
	public void Cache(CacheConfig<T> cfg = null)
	{
		if (cfg == null)
		{
			// Default config:
			cfg = new CacheConfig<T>();
		}

		var indices = _database.GetIndices(typeof(T));

		var localeSet = _database.Locales;

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

		_cache = new ServiceCache<T>[localeSet.Length];

		for (var i = 0; i < localeSet.Length; i++)
		{
			var locale = localeSet[i];

			if (locale == null)
			{
				// Happens if there's gaps in IDs (because a locale was deleted for ex).
				continue;
			}

			_cache[i] = new ServiceCache<T>(indices);
			_cache[i].OnChange = cfg.OnChange;
		}

		// Do we need to preload the values?
		if (cfg.Preload.HasValue && cfg.Preload.Value)
		{
			if (Services.Started)
			{
				// Services already started - preload right now:
				var task = PreloadCacheInternal(localeSet);
				task.Wait();
			}
			else
			{
				Events.ServicesAfterStart.AddEventListener(async (Context ctx, object src) =>
				{
					await PreloadCacheInternal(localeSet);
					return src;
				}, cfg.PreloadPriority);
			}
		}
	}

	/// <summary>
	/// Gets a cache for a given locale ID. Null if none.
	/// </summary>
	/// <param name="localeId"></param>
	/// <returns></returns>
	public ServiceCache<T> GetCacheForLocale(int localeId)
	{
		if (_cache == null || localeId <= 0 || localeId > _cache.Length)
		{
			return null;
		}
		return _cache[localeId - 1];
	}

	private async Task PreloadCacheInternal(Locale[] locales)
	{
		// Get everything, for each supported locale:
		for (var i = 0; i < locales.Length; i++)
		{
			var locale = locales[i];

			if (locale == null)
			{
				continue;
			}

			var cache = _cache[i];

			var ctx = new Context()
			{
				LocaleId = locale.Id
			};

			var everything = await List(ctx, null);

			foreach (var row in everything)
			{
				cache.Add(ctx, row);
			}
		}
	}
}
