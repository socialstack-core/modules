using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.CustomContentTypes
{
	/// <summary>
	/// Handles customContentTypeFields.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CustomContentTypeFieldService : AutoService<CustomContentTypeField>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CustomContentTypeFieldService() : base(Events.CustomContentTypeField)
        {
			// Example admin page install:
			// InstallAdminPages("CustomContentTypeFields", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
