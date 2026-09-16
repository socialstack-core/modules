using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Forms
{
	/// <summary>
	/// Handles formResponses.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class FormResponseService : AutoService<FormResponse>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public FormResponseService() : base(Events.FormResponse)
        {
			InstallAdminPages("Form Responses", "fa:fa-th-list", new string[] { "id", "formId" });
		}
	}
    
}
