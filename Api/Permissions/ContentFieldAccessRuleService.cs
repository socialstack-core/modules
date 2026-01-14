using Api.Eventing;

namespace Api.Permissions
{
	/// <summary>
	/// Handles contentFieldAccessRules.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ContentFieldAccessRuleService : AutoService<ContentFieldAccessRule>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ContentFieldAccessRuleService() : base(Events.ContentFieldAccessRule)
        {
			// Example admin page install:
			// InstallAdminPages("ContentFieldAccessRules", "fa:fa-rocket", new string[] { "id", "name" });
			Cache();
		}
	}
    
}
