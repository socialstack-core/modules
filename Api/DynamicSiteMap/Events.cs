using Api.DynamicSiteMap;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Event group for the dynamic site map.
		/// </summary>
		public static DynamicSiteMapEventGroup SiteMap;
	}
}

namespace Api.DynamicSiteMap
{
	/// <summary>
	/// The group of events for services. See also Events.Service
	/// </summary>
	public partial class DynamicSiteMapEventGroup : Eventing.EventGroup
	{
		/// <summary>
		/// Called when the a Url is being validated as to be included in the sitemap
		/// </summary>
		public Api.Eventing.EventHandler<SiteMapEntry> ValidateLink;
    }
}
