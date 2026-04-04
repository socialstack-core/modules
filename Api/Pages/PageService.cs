using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.NavMenus;
using Api.Startup;
using Api.Translate;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Pages
{
	/// <summary>
	/// Handles pages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(9)]
	[HostType("web")]
	public partial class PageService : AutoService<Page>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PageService() : base(Events.Page)
		{

			var config = GetConfig<PageServiceConfig>();

			if (config.InstallDefaultPages)
			{
				// If you don't have a homepage or admin area, this'll create them:
				Install(
					new PageBuilder()
					{
						Url = "/",
						Key = "home",
						Title = "Homepage",
						BuildBody = (PageBuilder builder) => {
							return builder.AddTemplate(
								new CanvasNode("p")
								.AppendChild(new CanvasNode()
								{
									StringContent = "Welcome to your new SocialStack instance. This text comes from the pages table in your database in a format called canvas JSON - you can read more about this format in the documentation."
								})
							);
						}
					},
					new PageBuilder()
					{
						Url = "/en-admin/",
						Key = "admin",
						Title = "Welcome to the admin area",
						BuildBody = (PageBuilder builder) => {
							return builder.AddTemplate(
								new CanvasNode("Admin/Layouts/Dashboard")
							);
						}
					},
					new PageBuilder()
					{
						Url = "/en-admin/login",
						Key = "admin_login",
						Title = "Login to the admin area",
						BuildBody = (PageBuilder builder) => {
							return new CanvasNode("Admin/Layouts/Landing")
							.AppendChild(
								new CanvasNode("Admin/Tile")
								.AppendChild(
									new CanvasNode("Admin/LoginForm")
								)
							);
						}
					},
					new PageBuilder()
					{
						Url = "/en-admin/stdout",
						Key = "admin_stdout",
						Title = "Server log monitoring",
						BuildBody = (PageBuilder builder) =>
						{
							return builder.AddTemplate(
								new CanvasNode("Admin/Dashboards/Stdout")
							);
						}
					},
					new PageBuilder()
					{
						Url = "/en-admin/stress",
						Key = "admin_stress",
						Title = "Stress testing the API",
						BuildBody = (PageBuilder builder) =>
						{
							return builder.AddTemplate(
								new CanvasNode("Admin/Dashboards/StressTest")
							);
						}
					},
					new PageBuilder()
					{
						Url = "/en-admin/database",
						Key = "admin_database",
						Title = "Developer Database Access",
						BuildBody = (PageBuilder builder) =>
						{
							return builder.AddTemplate(
								new CanvasNode("Admin/Dashboards/Database")
							);
						}
					},
					new PageBuilder()
					{
						Url = "/en-admin/register",
						Key = "admin_register",
						Title = "Create a new account",
						BuildBody = (PageBuilder builder) =>
						{
							return new CanvasNode("Admin/Layouts/Landing")
							.AppendChild(
								new CanvasNode("Admin/Tile")
								.AppendChild(
									new CanvasNode("Admin/RegisterForm")
								)
							);
						}
					},
					new PageBuilder()
					{
						Url = "/en-admin/permissions",
						Key = "admin_permissions",
						Title = "Permissions",
						BuildBody = (PageBuilder builder) =>
						{
							return builder.AddTemplate(
								new CanvasNode("Admin/PermissionGrid")
							);
						}
					},
					new PageBuilder()
					{
						Key = "404",
						Title = "Page not found",
						BuildBody = (PageBuilder builder) =>
						{
							return builder.AddTemplate(
								new CanvasNode("p").AppendChild(
									new CanvasNode() {
										StringContent = "The page you were looking for wasn't found here."
									}
								)
							);
						}
					}
				);
			}

			// Install the admin pages.
			InstallAdminPages(new AdminPageOptions()
			{
				NavMenuLabel = new Localized<string>("Pages"),
				NavMenuIcon = "fa:fa-paragraph",
				NavMenuParentKey = "content_management",
				ListFields = ["id", "title"],
				Tabs = [
					new AdminTab("Design", "design"),
					new AdminTab("Details", "details")
				]
			});

			Events.Page.BeforePageInstall.AddEventListener((context, builder) => {

				if (builder == null || builder.ContentType != typeof(Page))
				{
					return ValueTask.FromResult(builder);
				}

				if (builder.PageType == CommonPageType.AdminEdit)
				{
					builder.GetContentRoot()
						.Empty()
						.AppendChild(
							new CanvasNode("Admin/Page/Single")
							.WithPrimaryLink("content")
							.With("tabs", builder.AdminPageOptions.Tabs)
						);
				}
				else if (builder.PageType == CommonPageType.AdminAdd)
				{
					builder.GetContentRoot()
						.Empty()
						.AppendChild(new CanvasNode("Admin/Page/Single"));
				}
				else if (builder.PageType == CommonPageType.AdminList)
				{
					builder.GetContentRoot()
						.Empty()
						.AppendChild(new CanvasNode("Admin/Layouts/Sitemap"));
				}

				return ValueTask.FromResult(builder);
			}, 5);

			Cache();
		}

		/// <summary>
		/// Installs generic admin pages using the given fields to display on the list page. 
		/// Use the base InstallAdminPages on AutoService instead of this one directly.
		/// </summary>
		/// <param name="type">The content type that is being installed (Page, Blog etc)</param>
		/// <param name="options">
		/// The config options for the admin pages, such as the nav menu label and which fields appear on the list page.
		/// </param>
		public void InstallAdminPagesInt(Type type, AdminPageOptions options)
		{
			var navMenuLabel = options.NavMenuLabel;
			var navMenuIcon = options.NavMenuIcon;
			var typeName = type.Name;
			var typeNameLowercase = type.Name.ToLower();

			// "BlogPost" -> "Blog Post".
			var tidySingularName = Api.Startup.Pluralise.NiceName(type.Name);
			var tidyPluralName = Api.Startup.Pluralise.Apply(tidySingularName);

			var adminNavMenuItemService = Services.Get<AdminNavMenuItemService>();
			
			var parentId = 0u;
			
			if (!string.IsNullOrEmpty(options.NavMenuParentKey))
			{
				var group = adminNavMenuItemService.GetByKey(new Context(1,1,1), options.NavMenuParentKey);

				var parentGroup = group.GetAwaiter().GetResult();

				if (parentGroup is not null)
				{
					parentId = parentGroup.Id;
				}
			}

			// First install the list page:
			Install(new PageBuilder
			{
				ContentType = type,
				PageType = CommonPageType.AdminList,
				Url = "/en-admin/" + typeNameLowercase,
				Key = "admin_list:" + typeNameLowercase,
				Title = "Edit or create " + tidyPluralName,
				AdminNavMenuIcon = navMenuIcon,
				AdminPageOptions = options,
				NavMenuParentId = parentId,
				AdminNavMenuTitle = navMenuLabel.GetFallback(),
				PrimaryContentIncludes = options.ListIncludes,
				BuildBody = (PageBuilder builder) => {
					return builder.AddTemplate(
						new CanvasNode("Admin/Layouts/List")
						.With("contentType", typeName)
						.With("singular", tidySingularName)
						.With("plural", tidyPluralName)
						.With("columns", options.ListColumns)
					);
				}
			});

			// Install the edit page:
			var incl = options == null || options.EditIncludes == null ? "*,primaryUrl" : options.EditIncludes;
			InstallSingleAdminPage(type, true, incl, options);

			// And the add page (includes not relevant here):
			InstallSingleAdminPage(type, false, null, options);
		}

		private void InstallSingleAdminPage(Type type, bool isEdit, string includes, AdminPageOptions options)
		{
			var typeName = type.Name;
			var typeNameLowercase = type.Name.ToLower();

			// "BlogPost" -> "Blog Post".
			var tidySingularName = Api.Startup.Pluralise.NiceName(type.Name);
			var tidyPluralName = Api.Startup.Pluralise.Apply(tidySingularName);

			var singlePage = new PageBuilder
			{
				ContentType = type,
				PageType = isEdit ? CommonPageType.AdminEdit : CommonPageType.AdminAdd,
				PrimaryContentIncludes = includes,
				AdminPageOptions = options,
				Url = "/en-admin/" + typeNameLowercase + "/" + (isEdit ? "${" + typeNameLowercase + ".id}" : "add"),
				Key = isEdit ? ("admin_primary:" + typeNameLowercase) : "admin_" + typeNameLowercase + "_add",
				Title = isEdit ? "Editing " + tidySingularName.ToLower() + " #${" + typeNameLowercase + ".id}" : "Creating " + tidySingularName.ToLower(),
				BuildBody = (PageBuilder builder) =>
				{
					var singlePageCanvas = new CanvasNode("Admin/AutoForm")
						.With("contentType", typeName)
						.With("singular", tidySingularName)
						.With("plural", tidyPluralName);

					if (!string.IsNullOrEmpty(options.EditTitleToken))
					{
						singlePageCanvas.With("editTitleToken", options.EditTitleToken);
					}

					if (options.Tabs != null && options.Tabs.Count > 0)
					{
						singlePageCanvas.With("tabs", options.Tabs);
					}

					if (isEdit)
					{
						singlePageCanvas.WithPrimaryLink("content");
					}

					return builder.AddTemplate(
						singlePageCanvas
					);
				}
			};

			Install(singlePage);
		}

		/// <summary>
		/// The built up list of pages to install when services have started.
		/// </summary>
		private List<PageBuilder> _toInstall;
		private object _installLocker = new object();

		/// <summary>
		/// Installs the given page. It checks if they exist by their URL (or ID, if you provide that instead), and if not, creates them.
		/// </summary>
		/// <param name="builders">
		/// Constructs the base page content.
		/// This will then be passed through the InstallPage function where other modules can manipulate it if needed.
		/// You can ask the provided PageInstaller for a templated root node too, and it will 
		/// generate one based on if your page is an admin one or not (established from its key starting with "admin_").
		/// </param>
		public void Install(params PageBuilder[] builders)
		{
			bool scheduleStart = false;

			lock (_installLocker)
			{
				if (_toInstall == null)
				{
					_toInstall = new List<PageBuilder>();
					scheduleStart = true;
				}

				_toInstall.AddRange(builders);
			}

			if (scheduleStart)
			{
				if (Services.Started)
				{
					Task.Run(async () =>
					{
						List<PageBuilder> set;

						lock (_installLocker)
						{
							set = _toInstall;
							_toInstall = null;
						}
						await InstallInternal(new Context(), set);
					});
				}
				else
				{
					Events.Service.AfterStart.AddEventListener(async (Context ctx, object src) =>
					{
						List<PageBuilder> set;

						lock (_installLocker)
						{
							set = _toInstall;
							_toInstall = null;
						}
						await InstallInternal(ctx, set);
						return src;
					});
				}
			}
		}

		/// <summary>
		/// Compares the given localized JsonString values which can vary in textual value 
		/// (e.g. because one came from the DB and contains specific spacing patterns) but actually be structurally the same.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		private bool DeepJsonEquals(Localized<JsonString> a, Localized<JsonString> b)
		{
			if (a.Count != b.Count)
			{
				return false;
			}

			foreach (var kvp in a.Values)
			{
				var strA = kvp.Value;
				if (!b.TryGet(kvp.Key, out JsonString strB))
				{
					// Key not present in B - quit.
					return false;
				}

				// Get the json strings:
				var jsonA = strA.ValueOf();
				var jsonB = strB.ValueOf();

				var aEmpty = string.IsNullOrEmpty(jsonA);

				if (aEmpty != string.IsNullOrEmpty(jsonB))
				{
					// One empty and the other is not.
					return false;
				}

				if (aEmpty)
				{
					// They're both empty.
					continue;
				}

				// Both not empty
				var tokenA = JToken.Parse(jsonA);
				var tokenB = JToken.Parse(jsonB);

				if (!JToken.DeepEquals(tokenA, tokenB))
				{
					return false;
				}
			}

			// All keys passed the deepEquals check.
			return true;
		}

		/// <summary>
		/// Installs the given page(s). It checks if they exist by their InstallKey, and if not, creates them.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="builders"></param>
		private async ValueTask InstallInternal(Context context, List<PageBuilder> builders)
		{
			if (builders == null)
			{
				return;
			}

			foreach (var builder in builders)
			{
				if (string.IsNullOrEmpty(builder.Key))
				{
					throw new ArgumentException("A Key is required when installing a page.");
				}
			}

			// Get the pages by those keys:
			var existingPages = (await Where("Key=[?]", DataOptions.NoCacheIgnorePermissions)
					.Bind(builders.Select(page => page.Key))
					.ListAll(context));

			var existingPagesLookup = new Dictionary<string, Page>();

			foreach (var pg in existingPages)
			{
				existingPagesLookup[pg.Key] = pg;
			}

			#if DEBUG
			var buildTime = DateTime.UtcNow;
			#else
			var buildTime = new DateTime(Services.Get<FrontendCodeService>().Version);
			#endif

			// For each page to consider for install..
			foreach (var builder in builders)
			{
				// If it already exists, consider updating it.
				if (existingPagesLookup.TryGetValue(builder.Key, out Page existingPage))
				{
					if (existingPage.LastInstallBuildTimeUtc >= buildTime)
					{
						continue;
					}
				}

				// Start building:
				builder.Build();

				await Events.Page.BeforePageInstall.Dispatch(context, builder);
				builder.Page.BodyJson = new Localized<JsonString>(new JsonString(builder.Body.ToJson()));

				if (existingPage != null)
				{
					// Has it changed?
					if (
						existingPage.PrimaryContentIncludes == builder.Page.PrimaryContentIncludes && 
						existingPage.PrimaryContentType == builder.Page.PrimaryContentType && 
						DeepJsonEquals(builder.Page.BodyJson, existingPage.BodyJson))
					{
						// Nope!
						continue;
					}

					// Are there any revisions of the page since the last time it was checked?
					var revId = existingPage.LastInstallRevisionId.HasValue ? existingPage.LastInstallRevisionId.Value : 0;

					var pageRevisions = await Revisions
						.Where("Id>=? and ContentId=?", DataOptions.IgnorePermissions)
						.Bind(revId)
						.Bind((ulong)existingPage.Id)
						.ListAll(context);

					// Sort by ID just to be sure:
					pageRevisions.Sort((a, b) => a.Id.CompareTo(b.Id));

					var highestRevisionId = pageRevisions.Count > 0 ? pageRevisions[pageRevisions.Count - 1].Id : 0;

					// In most instances there should be 1 or 2 revisions in the set.
					// The first = the one where Id==LastInstallRevisionId
					// The second = the one created by calling Update or Create itself
					int skipRevisions = 0;

					if (pageRevisions.Count > 0)
					{
						if (pageRevisions[0].Id == revId)
						{
							// Skip the next revision.
							skipRevisions = 2;
						}
						else
						{
							// Skip the first revision only.
							skipRevisions = 1;
						}
					}

					var hasAdditionalRevisions = (pageRevisions.Count - skipRevisions) > 0;

					if (hasAdditionalRevisions)
					{
						// User edits identified.
						// This effectively permanently blocks the installer from running currently.
						continue;
					}

					// The code retains control over the page and it can now be updated.
					Log.Info(LogTag, "Updated page '" + existingPage.Key + "'");

					try
					{
						_ = await Update(context, existingPage, (Context ctx, Page toUpdate, Page original) =>
						{

							toUpdate.PrimaryContentIncludes = builder.Page.PrimaryContentIncludes;
							toUpdate.BodyJson = builder.Page.BodyJson;
							toUpdate.PrimaryContentType = builder.Page.PrimaryContentType;

							// Might be just the body/ includes/ both.
							toUpdate.LastInstallRevisionId = highestRevisionId;
							toUpdate.LastInstallBuildTimeUtc = buildTime;

						}, DataOptions.IgnorePermissions);
					}
					catch (AggregateException aggregate)
					{
						Log.Error("pages/subscriber/update-error", aggregate);
					}
				}
				else
				{
					builder.Page.LastInstallBuildTimeUtc = buildTime;
					builder.Page.LastInstallRevisionId = 0;
					_ = await Create(context, builder.Page, DataOptions.IgnorePermissions);
				}
			}
		}

	}
}


namespace Api.CanvasRenderer
{
	public partial class CanvasDetails
	{
		/// <summary>
		/// The page it is a part of.
		/// </summary>
		public Api.Pages.Page Page;
	}
}