using Api.Translate;
using Microsoft.AspNetCore.Http;


namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Set of events for a context.
		/// </summary>
		public static ContextEventGroup Context;
	}
	
	/// <summary>
	/// Events relating to contexts.
	/// </summary>
	public partial class ContextEventGroup : EventGroup
	{
		/// <summary>
		/// Called during GetContext.
		/// </summary>
		public EventHandler<HttpRequest> OnLoad;

		/// <summary>
		/// Called if the context is able to use the page cache.
		/// The given value is the current cache access (true means it can use the cache).
		/// </summary>
		public EventHandler<bool> CanUseCache;
	}
}