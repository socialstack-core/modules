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
	public partial class ContextEventGroup
	{
		/// <summary>
		/// Called during GetContext.
		/// </summary>
		public EventHandler<HttpRequest> OnLoad;
		
	}
}