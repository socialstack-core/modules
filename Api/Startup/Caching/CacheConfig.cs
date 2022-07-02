using Api.Contexts;
using System;
using System.Threading.Tasks;

namespace Api.Startup{
	
	/// <summary>
	/// Config indicating how a service should cache its content.
	/// </summary>
	public class CacheConfig<T> : CacheConfig
	{
		/// <summary>
		/// Runs when an item has been updated.
		/// If the first object is null and the second is not null, this is a first time add.
		/// If they're both set, it's an update.
		/// If the first is not null and the second is set, it's a removal of some kind.
		/// Note that you may wish to check if the object change is for a particular locale (via the given context object).
		/// An object with the same ID can pass through repeatedly, but with different translations.
		/// </summary>
		public Action<Context, T, T> OnChange;
	}
	
	/// <summary>
	/// Config indicating how a service should cache its content.
	/// </summary>
	public class CacheConfig
	{
		/// <summary>
		/// True if this type is created at a very low frequency and should use purely sequential IDs.
		/// This works by adding 1 to the highest ID in the cache.
		/// </summary>
		public bool LowFrequencySequentialIds { get; set; } = false;
		
		/// <summary>
		/// True if all content should be preloaded in the cache.
		/// </summary>
		public bool? Preload { get; set; } = true;

		/// <summary>
		/// True if all content should be retained in the cache.
		/// I.e. creates/ updates/ deletes are reflected in the cache across a cluster, 
		/// instead of those things just deleting the cache entry.
		/// </summary>
		public bool? Retain { get; set; } = false;

		/// <summary>
		/// If preloading, this is the event handler priority
		/// </summary>
		public int PreloadPriority { get; set; } = 10;

		/// <summary>
		/// An action which is triggered when the cache is loaded, if Preload is true.
		/// </summary>
		public Func<ValueTask> OnCacheLoaded;

		/// <summary>
		/// Very rare that this is false. If you are using the cache, you should almost always sync it across the cluster automatically.
		/// Only turn this off if your type is otherwise handled. For example, you have an in-memory only 
		/// type which is permitted to vary from server to server, or all servers know how to populate the cache anyway without it needing to be network synced.
		/// </summary>
		public bool ClusterSync = true;

		/// <summary>
		/// A new cache config with default settings.
		/// </summary>
		public CacheConfig(){
			
		}
	}
	
}