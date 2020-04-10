using Api.Contexts;
using System.Collections.Generic;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		
		/// <summary>
		/// Called when a public context is being returned.
		/// </summary>
		public static EventHandler<PublicContext> PubliccontextOnSetup;

	}
	
}