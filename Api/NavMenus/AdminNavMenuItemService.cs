using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using System.Linq;
using Api.Eventing;
using Api.Contexts;
using Api.Startup;

namespace Api.NavMenus
{
	/// <summary>
	/// Handles navigation menu items.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class AdminNavMenuItemService : AutoService<AdminNavMenuItem>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AdminNavMenuItemService() : base(Events.AdminNavMenuItem)
        {
			// Example admin page install:
			InstallAdminPages("Admin Nav Menu Item", "fa:fa-child", new string[] { "id", "title" , "target" });
		}

		/// <param name="targetUrl">The target page url, e.g. /en-admin/page</param>
		/// <param name="iconRef">The ref to use for the icon. Typically these are fontawesome refs, of the form fa:fa-thing</param>
		/// <param name="label">The text that appears on the menu</param>
		/// <param name="visibilityJson">Any visibility rules</param>
		public async ValueTask InstallAdminEntry(string targetUrl, string iconRef, string label, string visibilityJson = null)
		{
			await Install(
				new AdminNavMenuItem
				{
					Target = targetUrl,
					Title = label,
					IconRef = iconRef,
					VisibilityRuleJson = visibilityJson
				}
			);
		}

		/// <summary>
		/// Installs an item (Creates it if it doesn't already exist). MenuKey is required, but MenuId is not.
		/// </summary>
		public async ValueTask Install(AdminNavMenuItem item)
		{
			var context = new Context();

			var existingEntry = await Where("Target=?", DataOptions.IgnorePermissions).Bind(item.Target).Any(context);

			if (!existingEntry)
			{
				await Create(context, item, DataOptions.IgnorePermissions);
			}
		}

	}

}
