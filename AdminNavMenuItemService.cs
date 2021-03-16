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
		private int Id = 1;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AdminNavMenuItemService() : base(Events.AdminNavMenuItem)
        {
			// In-memory only type:
			Cache(new CacheConfig() {
				ClusterSync = false
			});
		}

		/// <param name="targetUrl">The target page url, e.g. /en-admin/page</param>
		/// <param name="iconRef">The ref to use for the icon. Typically these are fontawesome refs, of the form fa:fa-thing</param>
		/// <param name="label">The text that appears on the menu</param>
		public async ValueTask InstallAdminEntry(string targetUrl, string iconRef, string label)
		{
			await Install(
				new AdminNavMenuItem
				{
					Target = targetUrl,
					Title = label,
					IconRef = iconRef
				}
			);
		}

		/// <summary>
		/// Installs an item (Creates it if it doesn't already exist). MenuKey is required, but MenuId is not.
		/// </summary>
		public async ValueTask Install(AdminNavMenuItem item)
		{
			var context = new Context();

			// Match by target URL of the item.
			var filter = new Filter<AdminNavMenuItem>();
			filter.Equals("Target", item.Target);

			var existingEntry = (await List(context, filter));

			if (existingEntry.Count == 0)
			{
				item.Id = Id++;
				await Create(context, item);
			}
		}

	}

}
