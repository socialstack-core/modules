using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Pages;
using Api.Permissions;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Tags
{
	/// <summary>
	/// Handles tags - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class TagService : AutoService<Tag>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TagService() : base(Events.Tag)
		{
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.

			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
			{
				if (builder.PageType == CommonPageType.AdminEdit)
				{
					// Add taxonomy tab:
					builder.AddAdminTab(new AdminTab("Tags and categories", "tags_categories"));
				}

				return new ValueTask<PageBuilder>(builder);
			}, 15); // Such that tab order isn't randomly flip-flopping based on service startup order

			InstallAdminPages("Tags", "fa:fa-tags", ["id", "name"]);
		}

	}
    
}
