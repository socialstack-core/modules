using Api.AvailableEndpoints;
using Api.Pages;
using Api.Permissions;
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
		/// All available endpoint events.
		/// </summary>
		public static EventGroup<ApiStructure> AvailableEndpoints;
	}
	
}