using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.CustomContentTypes
{
	/// <summary>
	/// Handles customContentTypeSelectOptions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CustomContentTypeSelectOptionService : AutoService<CustomContentTypeSelectOption>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CustomContentTypeSelectOptionService() : base(Events.CustomContentTypeSelectOption)
        {
			// Example admin page install:
			// InstallAdminPages("CustomContentTypeSelectOptions", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
