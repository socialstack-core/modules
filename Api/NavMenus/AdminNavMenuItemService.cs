using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Api.Eventing;
using Api.Contexts;
using Api.Pages;
using Api.Permissions;
using Api.Startup;

namespace Api.NavMenus
{
	/// <summary>
	/// Handles navigation menu items.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class AdminNavMenuItemService : AutoService<AdminNavMenuItem>
	{
		// <summary>
		// Cache to avoid repeated expensive lookups of content types by key.
		// Key is case-insensitive to be resilient to case variations in page keys.
		// </summary>
		// private readonly ConcurrentDictionary<string, Type> _keyContentTypes = new(StringComparer.OrdinalIgnoreCase);

		private bool _installed = false;

		/// <summary>
		/// The set of admin groups which are going to be created
		/// </summary>
		public static readonly List<AdminNavMenuItem> RequiredGroups = [
			new()
			{
				Title = "Content Management",
				Key = "content_management",
				IconRef = "fa:rocket",
				ParentId = 0
			},
			new()
			{
				Title = "Security",
				Key = "security",
				IconRef = "fa:lock",
				ParentId = 0
			}
		];

		/// <summary>
		/// Constructor injecting dependencies.
		/// Calls base constructor with appropriate event topic.
		/// Calls example admin page install to bootstrap initial state.
		/// </summary>
		public AdminNavMenuItemService() : base(Events.AdminNavMenuItem)
		{
			Events.Page.BeforePageInstall.AddEventListener(async(ctx, builder) =>
			{
				if (
					builder == null || 
					!builder.IsAdmin || 
					string.IsNullOrEmpty(builder.Url) ||
					builder.PageType != CommonPageType.AdminList
				)
				{
					// Non-admin list builder.
					return builder;
				}

				// Create an admin nav menu link to the target page.
				// You can disable this behaviour by ensuring both icon and title are blank.
				var title = builder.AdminNavMenuTitle;
				var icon = builder.AdminNavMenuIcon;

				if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(icon))
				{
					// Intentionally requesting no admin link
					return builder;
				}

				// Does this link exist? If yes, do nothing too.
				var existing = await Where("PageKey=?", DataOptions.IgnorePermissions)
					.Bind(builder.Key)
					.First(ctx);

				if (existing != null)
				{
					return builder;
				}

				var adminNavMenuItem = new AdminNavMenuItem()
				{
					Title = title,
					Url = builder.Url,
					IconRef = icon,
					PageKey = builder.Key,
					ParentId = builder.NavMenuParentId
				};
				
				await Create(ctx, adminNavMenuItem, DataOptions.IgnorePermissions);
				return builder;
			});
			
			// Install example admin pages for demonstration / default setup.
			InstallAdminPages(null, null,  ["id", "title", "target"]);
			Cache();
		}
		
		/// <summary>
		/// Ensures that all required groups are installed.
		/// Prevents duplicate installations by tracking state in <see cref="_installed"/>.
		/// </summary>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// </returns>
		public async ValueTask InstallGroups()
		{
			if (_installed)
			{
				return;
			}

			_installed = true;
			
			// Reason for change: 
			// prior was a foreach loop, 
			// every iteration called "GetOrCreate", 
			// but when it did exist, it didn't actually load
			// the existing one, it just kept the variant
			// pre-create/pre-load, this meant that the items
			// existed within this array, and in the database
			// but weren't reconciled, so modified this to 
			// overwrite the variant at its index.
			var ctx = new Context(1, 0, 1);
			
			for(var i = 0;i < RequiredGroups.Count;i++)
			{
				RequiredGroups[i] = await GetOrCreate(ctx, RequiredGroups[i]);
			}
		}

		/// <summary>
		/// Retrieves an existing navigation menu item by key or creates it if it does not exist.
		/// </summary>
		/// <param name="ctx">The database context.</param>
		/// <param name="adminNavMenuItem">The menu item to retrieve or create.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the existing or newly created <see cref="AdminNavMenuItem"/>.
		/// </returns>
		public async ValueTask<AdminNavMenuItem> GetOrCreate(Context ctx, AdminNavMenuItem adminNavMenuItem)
		{
			var existing = await GetByKey(ctx, adminNavMenuItem.Key) ?? await Create(ctx, adminNavMenuItem);

			return existing;
		}

		/// <summary>
		/// Retrieves a navigation menu item by its unique key.
		/// </summary>
		/// <param name="ctx">The database context.</param>
		/// <param name="key">The unique key identifying the navigation menu item.</param>
		/// <returns>
		/// A task that represents the asynchronous operation.
		/// The task result contains the matching <see cref="AdminNavMenuItem"/>,
		/// or <c>null</c> if no item with the specified key exists.
		/// </returns>
		public async ValueTask<AdminNavMenuItem> GetByKey(Context ctx, string key)
		{
			return await Where("Key = ?", DataOptions.IgnorePermissions)
				.Bind(key)
				.First(ctx);
		}

		/// <summary>
		/// Retrieves a navigation menu item by its unique key synchronously.
		/// This is a blocking call and should be avoided in async flows.
		/// </summary>
		/// <param name="ctx">The database context.</param>
		/// <param name="key">The unique key identifying the navigation menu item.</param>
		/// <returns>
		/// The matching <see cref="AdminNavMenuItem"/>,
		/// or <c>null</c> if no item with the specified key exists.
		/// </returns>
		public AdminNavMenuItem GetByKeySync(Context ctx, string key)
		{
			return GetByKey(ctx, key).GetAwaiter().GetResult();
		}

		
		/// <summary>
		/// Retrieves a list of admin navigation menu items that the current user is authorized to access,
		/// based on their granted capabilities for each item's content type.
		/// </summary>
		/// <param name="context">The execution context containing the user role and permission scope.</param>
		/// <returns>A list of <see cref="AdminNavMenuItem"/> the user has access to.</returns>
		/// <remarks>
		/// Only one matching granted capability is required per menu item. 
		/// Capability-to-content-type mapping is cached for performance.
		/// </remarks>
		public async ValueTask<List<AdminNavMenuItem>> ListUserAccessibleNavMenuItems(Context context)
		{
			// Menu items are cached during construction for fast access
			var allMenuItems = await Where(DataOptions.IgnorePermissions).ListAll(context);

			// Group the capabilities in a dictionary by their content type, saves the nested foreach lookups.
			var capabilitiesByType = Capabilities.GetAllCurrent()
				//  Grab associated **load** and **edit** capabilities for the targeted content type
				.Where(c => 
					c.Name.EndsWith("_load", StringComparison.OrdinalIgnoreCase) ||
					c.Name.EndsWith("_update", StringComparison.OrdinalIgnoreCase))
				.GroupBy(c => c.ContentType)
				.ToDictionary(g => g.Key, g => g.AsEnumerable());

			var grantedMenuItems = new List<AdminNavMenuItem>(capacity: allMenuItems.Count); // Pre-allocate max capacity

			foreach (var item in allMenuItems)
			{
				if (string.IsNullOrEmpty(item.Url))
				{
					// represents a group
					grantedMenuItems.Add(item);
					continue;
				}
				var contentService = item.PageContentService;

				if (contentService is null)
				{
					continue;
				}

				var contentType = contentService.ServicedType;

				if (!capabilitiesByType.TryGetValue(contentType, out var relevantCapabilities))
				{
					continue;
				}

				var hasAll = true;

				// Using a basic ID=0 test object:
				var testObject = Activator.CreateInstance(contentService.InstanceType);

				foreach (var capability in relevantCapabilities)
				{
					// Check if any single capability grants access — early exit on first match
					if (!context.Role.IsGranted(capability, context, testObject, ContextFlags.None))
					{
						// All required - update and load
						hasAll = false;
						break;
					}
				}

				if (hasAll)
				{
					grantedMenuItems.Add(item);
				}
			}

			grantedMenuItems.Sort((a, b) => a.Title[0] - b.Title[0]);

			return grantedMenuItems;
		}


	}
}
