using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using System.Collections;
using Newtonsoft.Json.Linq;
using Api.Startup;
using System;

namespace Api.Categories
{
	/// <summary>
	/// Handles categories - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CategoryService : AutoService<Category>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CategoryService() : base(Events.Category)
		{
			InstallAdminPages("Categories", "fa:fa-folder", new string[] { "id", "name" });

			Cache();
		}

	}
    
}
