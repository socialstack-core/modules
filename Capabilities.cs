using System.Collections.Generic;


namespace Api.Permissions
{
	/// <summary>
	/// This permissions system is roles/ capabilities based:
	/// * Users have one role.
	/// * A role is defined by a set of capabilities granted to it.
	/// * Functionality checks to see if a user has a particular capability.
	/// 
	/// Capabilities are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public static partial class Capabilities
    {
		/// <summary>
		/// Get all the current capabilities. If you need to know when capabilities are created or destroyed, use  Events.Service Create and Delete.
		/// </summary>
		public static IEnumerable<Capability> GetAllCurrent()
		{
			// For each service that is loaded, go through each capability in the event group.
			foreach (var kvp in Api.Startup.Services.AutoServices)
			{
				var evtGroup = kvp.Value.GetEventGroup();

				if (evtGroup == null || evtGroup.AllWithCapabilities == null)
				{
					continue;
				}

				foreach (var cap in evtGroup.AllWithCapabilities)
				{
					yield return cap.Capability;
				}
			}
		}
	}
}
