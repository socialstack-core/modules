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
		/// Called when roles are being setup. Use this to define new roles. Occurs before capabilities are granted.
		/// </summary>
		public static EventHandler<object> RoleOnSetup;
		
		/// <summary>
		/// Called when capabilities are being granted to roles.
		/// </summary>
		public static EventHandler<object> CapabilityOnSetup;

	}
	
}