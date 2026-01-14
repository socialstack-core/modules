using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using System.Linq;
using Api.Eventing;
using Api.Contexts;
using Api.Startup;
using Api.CanvasRenderer;
using System;
using Api.Pages;

namespace Api.NavMenus
{
	/// <summary>
	/// Handles navigation menus.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class NavMenuService : AutoService<NavMenu>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NavMenuService() : base(Events.NavMenu)
        {
			InstallAdminPages("Nav menus", "fa:fa-map-signs", ["id", "name", "key"]);

			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) => {

				if (builder == null)
				{
					return new ValueTask<PageBuilder>(builder);
				}

				if (builder.ContentType == typeof(NavMenu) && builder.PageType == CommonPageType.AdminEdit)
				{
					// This is likely obsoleted: favour more customised pages instead.
					builder.GetContentRoot().AppendChild(
						new CanvasNode("Admin/AutoList")
						.With("contentType", "NavMenuItem")
						.With("filterField", "NavMenuId")
						.With("create", true)
						.With("searchFields", null)
						.With("filterValue", "${primary.id}")
						.With("fields", new string[] { "bodyJson" })
					);
				}

				return new ValueTask<PageBuilder>(builder);
			});
		}
	}
    
}
