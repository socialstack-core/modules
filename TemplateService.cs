using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Api.Templates
{
	/// <summary>
	/// Handles templates.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class TemplateService : AutoService<Template>, ITemplateService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TemplateService() : base(Events.Template)
        {
			InstallAdminPages("Templates", "fa:fa-file-medical", new string[] { "id", "title", "key" });
		}
		
	}
    
}
