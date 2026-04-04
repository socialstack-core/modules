using Api.CanvasRenderer;
using Api.Components;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Pages;
using Api.Permissions;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

			Events.NavMenu.BeforeCreate.AddEventListener((Context context, NavMenu menu) => {

				if (string.IsNullOrEmpty(menu.Name))
				{
					throw new PublicException("Name is required", "navmenu/name_required");
				}

				if (string.IsNullOrEmpty(menu.Key))
				{
					menu.Key = menu.Name.ToLower().Trim().Replace(" ", "_");
				}

				return new ValueTask<NavMenu>(menu);

			});

#if !DEBUG
			Cache();
#endif
		}

		/// <summary>
		/// Populates a NavMenu from a MenuBuilder.
		/// </summary>
		protected override ValueTask PopulateContent(Context context, NavMenu content, ContentBuilder builder)
		{
			if (builder is MenuBuilder menuBuilder)
			{
				content.Name = menuBuilder.Name;
				content.ContentJson = new Localized<JsonString>(menuBuilder.ContentJson);
			}
			return new ValueTask();
		}
	}
}
