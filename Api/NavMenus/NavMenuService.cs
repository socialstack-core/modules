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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

			Events.NavMenu.BeforeUpdate.AddEventListener(async (Context context, NavMenu menu, NavMenu original) =>
			{
				if (!string.IsNullOrEmpty(menu.GeneratedMenuConfigJson.ValueOf()))
				{
					var json = await GenerateMenu(context, menu);
					if (json != null)
					{
						menu.ContentJson = new Localized<JsonString>(new JsonString(json));
					}
				}
				return menu;
			});

			Events.NavMenu.BeforeCreate.AddEventListener(async (Context context, NavMenu menu) =>
			{
				if (!string.IsNullOrEmpty(menu.GeneratedMenuConfigJson.ValueOf()))
				{
					var json = await GenerateMenu(context, menu);
					if (json != null)
					{
						menu.ContentJson = new Localized<JsonString>(new JsonString(json));
					}
				}
				return menu;
			});

			Events.NavMenu.BeforeCreate.AddEventListener(async (Context context, NavMenu menu) => {

				if (string.IsNullOrEmpty(menu.Name))
				{
					throw new PublicException("Name is required", "navmenu/name_required");
				}

				if (string.IsNullOrEmpty(menu.Key))
				{
					menu.Key = menu.Name.ToLower().Trim().Replace(" ", "_");

					var baseKey = menu.Key;
					var candidate = baseKey;
					var suffix = 1;

					while (await Where("Key=?", DataOptions.NoCacheIgnorePermissions).Bind(candidate).First(context) != null)
					{
						candidate = baseKey + "_" + suffix;
						suffix++;
					}

					menu.Key = candidate;
				}

				return menu;
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

		/// <summary>
		/// Generates a nav menu JSON string from the generative config on the given menu.
		/// Returns null if the menu is not generative.
		/// </summary>
		public async ValueTask<string> GenerateMenu(Context context, NavMenu menu)
		{
			var config = JObject.Parse(menu.GeneratedMenuConfigJson.ValueOf());
			var contentType = config["contentType"]?.Value<string>();

			if (string.IsNullOrEmpty(contentType))
			{
				return null;
			}

			var svc = Services.Get(contentType + "Service");

			if (svc == null)
			{
				return null;
			}

			var method = typeof(NavMenuService).GetMethod(nameof(GenerateMenuForType), BindingFlags.NonPublic | BindingFlags.Instance);

			if (method == null)
			{
				return null;
			}

			var genericMethod = method.MakeGenericMethod(svc.ServicedType, svc.IdType);

			return await (ValueTask<string>)genericMethod.Invoke(this, new object[] { context, config, svc });
		}

		private async ValueTask<string> GenerateMenuForType<T, ID>(Context context, JObject config, AutoService<T, ID> svc)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var filterObj = config["filter"] as JObject;
			var filter = (Filter<T, ID>)svc.LoadFilter(filterObj);
			var results = await filter.ListAll(context);
			filter.Release();

			var navItems = new List<Dictionary<string, object>>();

			foreach (var item in results)
			{
				var id = item.Id;
				var name = (await svc.GetMetaFieldValue(context, "title", item) as string) ?? id.ToString() ?? "Untitled";
				var url = svc.GetPrimaryUrl(context, item);

				navItems.Add(new Dictionary<string, object>
				{
					["id"] = id,
					["label"] = name,
					["target"] = url
				});
			}

			return JsonConvert.SerializeObject(new Dictionary<string, object> { ["items"] = navItems });
		}
	}
}
