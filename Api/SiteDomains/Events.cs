using Api.SiteDomains;
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
		/// Set of events for a siteDomain.
		/// </summary>
		public static EventGroup<SiteDomain> SiteDomain;
	}
}