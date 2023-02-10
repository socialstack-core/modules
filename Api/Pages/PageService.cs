using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Api.Startup;
using Api.CanvasRenderer;
using Api.Users;

namespace Api.Pages
{
	/// <summary>
	/// Handles pages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(9)]
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
					new Page()
					{
						Url = "/",
						Title = "Homepage",
						BodyJson = @"{
							""c"": {
								""t"": ""p"",
								""c"": {
									""s"": ""Welcome to your new SocialStack instance. This text comes from the pages table in your database in a format called canvas JSON - you can read more about this format in the documentation.""
								}
							}
						}"
					},
					new Page()
					{
						Url = "/en-admin",
						Title = "Welcome to the admin area",
						BodyJson = @"{
							""c"": {
								""t"": ""Admin/Layouts/Dashboard""
							}
						}"
					},
					new Page()
					{
						Url = "/en-admin/login",
						Title = "Login to the admin area",
						BodyJson = @"{
							""c"": {
								""t"": ""Admin/Layouts/Landing"",
								""c"": {
									""t"": ""Admin/Tile"",
									""c"": {
										""t"": ""Admin/LoginForm"",
						                ""i"": 2
									},
									""i"": 3
								},
								""i"": 4
							},
							""i"": 5
						}"
					},
					new Page()
					{
						Url = "/en-admin/stdout",
						Title = "Server log monitoring",
						BodyJson = @"{
							""c"": {
								""t"": ""Admin/Layouts/Default"",
								""c"": {
									""t"": ""Admin/Dashboards/Stdout""
								}
							}
						}"
					},
					new Page()
					{
						Url = "/en-admin/stress-test",
						Title = "Stress testing the API",
						BodyJson = @"{
							""c"": {
								""t"": ""Admin/Layouts/Default"",
								""c"": {
									""t"": ""Admin/Dashboards/StressTest""
								}
							}
						}"
					},
					new Page()
					{
						Url = "/en-admin/database",
						Title = "Developer Database Access",
						BodyJson = @"{
							""c"": {
								""t"": ""Admin/Layouts/Default"",
								""c"": {
									""t"": ""Admin/Dashboards/Database""
								}
							}
						}"
					},
					new Page()
					{
						Url = "/en-admin/register",
						Title = "Create a new account",
						BodyJson = @"{
							""c"": {
								""t"": ""Admin/Layouts/Landing"",
								""c"": {
									""t"": ""Admin/Tile"",
									""c"": {
										""t"": ""Admin/RegisterForm"",
						                ""i"": 2
									},
									""i"": 3
								},
								""i"": 4
							},
							""i"": 5
						}"
					},
					new Page()
					{
						Url = "/en-admin/permissions",
						Title = "Permissions",
						BodyJson = @"{
							""c"": {
								""t"": ""Admin/Layouts/Default"",
								""c"": {
									""t"": ""Admin/PermissionGrid""
								}
							}
						}"
					},
					new Page()
					{
						Url = "/404",
						Title = "Page not found",
						BodyJson = @"{
							""c"": {
								""t"": ""p"",
								""c"": {
									""s"": ""The page you were looking for wasn't found here."",
									""i"": 2
								}
							}
						}"
					}
				);
			}

			// Install the admin pages.
			InstallAdminPages("Pages", "fa:fa-paragraph", new string[] { "id", "url", "title" });

			Events.Page.BeforeAdminPageInstall.AddEventListener((Context context, Page page, CanvasNode canvas, Type contentType, AdminPageType pageType) =>
			{
				// Note: Some sites are completely headless and don't have the pages module, so this can't go in upload module.
				// We use .Name here rather than typeof(Upload) to avoid coupling with uploads. Essentially, both modules are optional this way.
				if (contentType != null && contentType.Name == "Upload")
				{
					if (pageType == AdminPageType.List)
					{
						// Installing admin page for the list of uploads.
						// The create button is actually an uploader.
						canvas.Module = "Admin/Layouts/MediaCenter";
						canvas.Data.Clear();
					}
				} else if (contentType == typeof(Page) && pageType == AdminPageType.List)
				{
					// Installing the list of pages.
					// This will instead use the sitemap component.
					canvas.Module = "Admin/Layouts/Sitemap";
				}

				return new ValueTask<Page>(page);
			});

			Events.Page.AfterUpdate.AddEventListener((Context context, Page page) =>
			{
				if (_urlGenerationCache != null)
				{
					// Need to update the two caches. We'll just wipe them for now:
					_urlGenerationCache = null;
					_urlLookupCache = null;
				}

				return new ValueTask<Page>(page);
			});

			Events.Page.AfterDelete.AddEventListener((Context context, Page page) =>
			{
				if (_urlGenerationCache != null)
				{
					// Need to update the two caches. We'll just wipe them for now:
					_urlGenerationCache = null;
					_urlLookupCache = null;
				}

				return new ValueTask<Page>(page);
			});

			Events.Page.AfterCreate.AddEventListener((Context context, Page page) =>
			{
				if (_urlGenerationCache != null)
				{
					// Need to update the two caches. We'll just completely wipe them for now:
					_urlGenerationCache = null;
					_urlLookupCache = null;
				}

				return new ValueTask<Page>(page);
			});

			Events.Page.Received.AddEventListener((Context context, Page page, int mode) => {

				// Doesn't matter what the change was - we'll wipe the caches.
				if (_urlGenerationCache != null)
				{
					_urlGenerationCache = null;
					_urlLookupCache = null;
				}

				return new ValueTask<Page>(page);
			});

			// Pages must always have the cache on for any release site.
			// That's because the HtmlService has a release only cache which depends on the sync messages for pages, as well as e.g. the url gen cache.
#if !DEBUG
			Cache();
#endif

		}

		/// <summary>
		/// A cache used to identify which pages on the site are the canonical pages for each content type. Not locale sensitive.
		/// </summary>
		private UrlGenerationCache _urlGenerationCache;

		/// <summary>
		/// A cache used to identify which page to use for a particular URL, per locale.
		/// </summary>
		private UrlLookupCache[] _urlLookupCache;

		/// <summary>
		/// Get the page to use for the given URL.
		/// </summary>
		public async ValueTask<PageWithTokens> GetPage(Context context, string url, Microsoft.AspNetCore.Http.QueryString searchQuery, bool return404IfNotFound = true)
		{
			var urlInfo = new UrlInfo() // Struct
			{
				Url = url,
				Length = url.Length,
				Start = 0
			};

			var max = urlInfo.Length + urlInfo.Start;

			// Trim end:
			for (var i = max - 1; i >= urlInfo.Start; i--)
			{
				if (urlInfo.Url[i] == ' ')
				{
					urlInfo.Length--;
					max--;
				}
				else
				{
					break;
				}
			}

			// Trim start:
			for (var i = urlInfo.Start; i < max; i++)
			{
				if (urlInfo.Url[i] == ' ')
				{
					urlInfo.Start++;
					urlInfo.Length--;
				}
				else
				{
					break;
				}
			}

			if (urlInfo.Length > 0 && urlInfo.Url[urlInfo.Start] == '/')
			{
				urlInfo.Start++;
				urlInfo.Length--;
			}

			if (urlInfo.Length > 0 && urlInfo.Url[urlInfo.Start + urlInfo.Length - 1] == '/')
			{
				urlInfo.Length--;
			}

			// It won't contain a ? but just in case:
			max = urlInfo.Length + urlInfo.Start;
			for (var i = urlInfo.Start; i < max; i++)
			{
				if (urlInfo.Url[i] == '?')
				{
					urlInfo.Length = i - urlInfo.Start;
					break;
				}
			}

			// BeforeParseUrl is able to change the context, including the locale:
			urlInfo = await Events.Page.BeforeParseUrl.Dispatch(context, urlInfo, searchQuery);

			if (urlInfo.RedirectTo != null)
			{
				return new PageWithTokens() {
					RedirectTo = urlInfo.RedirectTo
				};
			}

			if (_urlLookupCache == null || _urlLookupCache.Length < context.LocaleId || _urlLookupCache[context.LocaleId - 1] == null)
			{
				await LoadCaches(context);
			}

			var cache = _urlLookupCache[context.LocaleId - 1];

			var pageInfo = await cache.GetPage(context, urlInfo, searchQuery);

			pageInfo = await Events.Page.BeforeResolveUrl.Dispatch(context, pageInfo, url, searchQuery);

			if (pageInfo.Page == null && return404IfNotFound)
			{
				pageInfo.Page = cache.NotFoundPage;
			}

			return pageInfo;
		}

		private async Task LoadCaches(Context context)
		{
			// Get all pages for this locale:
			var allPages = await Where(DataOptions.IgnorePermissions).ListAll(context);

			if (_urlGenerationCache == null)
			{
				// This cache is not locale sensitive as it exclusively uses the Url field which is not localised.

				// Instance and wait for it to be created:
				_urlGenerationCache = new UrlGenerationCache();

				// Load now:
				_urlGenerationCache.Load(allPages);
			}

			// Setup url lookup cache as well:
			var cache = new UrlLookupCache();

			if (_urlLookupCache == null)
			{
				// Create the cache:
				_urlLookupCache = new UrlLookupCache[context.LocaleId];
			}
			else if (_urlLookupCache.Length < context.LocaleId)
			{
				// Resize the cache:
				Array.Resize(ref _urlLookupCache, (int)context.LocaleId);
			}

			// Add cache to lookup:
			_urlLookupCache[context.LocaleId - 1] = cache;

			cache.Load(allPages);

			// Next, indicate that the cache has loaded. This is the event you'd use to add in things like custom redirect functions.
			await Events.Page.AfterLookupReady.Dispatch(context, cache);
		}

		/// <summary>
		/// Gets the tree of raw pages for the given context. Don't modify the response.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async ValueTask<UrlLookupCache> GetPageTree(Context context){

			// Load the tree:
			if (_urlLookupCache == null || _urlLookupCache.Length < context.LocaleId || _urlLookupCache[context.LocaleId - 1] == null)
			{
				await LoadCaches(context);
			}

			return _urlLookupCache[context.LocaleId - 1];
		}

		/// <summary>
		/// Gets the URL for the given piece of generic content. Pages are very often cached so this usually returns instantly.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="contentObject"></param>
		/// <param name="scope">State which type of URL you want - either a frontend URL or admin panel. Default is frontend if not specified.</param>
		/// <returns>A url which is relative to the site root.</returns>
		public async ValueTask<string> GetUrl(Context context, object contentObject, UrlGenerationScope scope = null)
		{
			if (_urlGenerationCache == null)
			{
				await LoadCaches(context);
			}

			var lookup = _urlGenerationCache.GetLookup(scope);

			if (!lookup.TryGetValue(contentObject.GetType(), out UrlGenerationMeta meta))
			{
				// URL is unknown.
				return "/";
			}

			return meta.Generate(contentObject);
		}

		/// <summary>
		/// Used as a temporary piece of JSON when setting up admin pages to help avoid people setting the bodyJson field incorrectly.
		/// </summary>
		private readonly string TemporaryBodyJson = "{\"content\":\"Don't set this field - its about to be overwritten by the contents of the Canvas object that you've been given.\"}";

		/// <summary>
		/// Installs generic admin pages using the given fields to display on the list page.
		/// </summary>
		/// <param name="type">The content type that is being installed (Page, Blog etc)</param>
		/// <param name="fields"></param>
		/// <param name="childAdminPage">
		/// A shortcut for specifying that your type has some kind of sub-type.
		/// For example, the NavMenu admin page specifies a child type of NavMenuItem, meaning each NavMenu ends up with a list of NavMenuItems.
		/// Make sure you specify the fields that'll be visible from the child type in the list on the parent type.
		/// For example, if you'd like each child entry to show its Id and Title fields, specify new string[]{"id", "title"}.
		/// </param>
		public async ValueTask InstallAdminPages(Type type, string[] fields, ChildAdminPageOptions childAdminPage)
		{
			var typeName = type.Name.ToLower();

			// "BlogPost" -> "Blog Post".
			var tidySingularName = Api.Startup.Pluralise.NiceName(type.Name);
			var tidyPluralName = Api.Startup.Pluralise.Apply(tidySingularName);
			
			var listPageCanvas = new CanvasNode("Admin/Layouts/List")
				.With("endpoint", typeName)
				.With("fields", fields)
				.With("singular", tidySingularName)
				.With("plural", tidyPluralName);
			
			var listPage = new Page
			{
				Url = "/en-admin/" + typeName,
				BodyJson = TemporaryBodyJson,
				Title = "Edit or create " + tidyPluralName
			};
			
			// Trigger an event to state that an admin page is being installed:
			// - Use this event to inject additional nodes into the page, or change it however you'd like.
			listPage = await Events.Page.BeforeAdminPageInstall.Dispatch(new Context(), listPage, listPageCanvas, type, AdminPageType.List);
			listPage.BodyJson = listPageCanvas.ToJson();

			var singlePageCanvas = new CanvasNode("Admin/Layouts/AutoEdit")
					.With("endpoint", typeName)
					.With("singular", tidySingularName)
					.With("id", "${primary.id}")
					.With("plural", tidyPluralName);

			if (childAdminPage != null && childAdminPage.ChildType != null)
			{
				singlePageCanvas.AppendChild(
					new CanvasNode("Admin/AutoList")
					.With("endpoint", childAdminPage.ChildType.ToLower())
					.With("filterField", type.Name + "Id")
					.With("create", childAdminPage.CreateButton)
					.With("searchFields", childAdminPage.SearchFields)
                    .With("filterValue", "${primary.id}")
                    .With("fields", childAdminPage.Fields ?? (new string[] { "id" }))
				);
			}

			var singlePage = new Page
			{
				Url = "/en-admin/" + typeName + "/{" + typeName + ".id}",
				BodyJson = TemporaryBodyJson,
				Title = "Editing " + tidySingularName.ToLower()
			};

			// Trigger an event to state that an admin page is being installed:
			// - Use this event to inject additional nodes into the page, or change it however you'd like.
			singlePage = await Events.Page.BeforeAdminPageInstall.Dispatch(new Context(), singlePage, singlePageCanvas, type, AdminPageType.Single);
			singlePage.BodyJson = singlePageCanvas.ToJson();

			// Future todo - If the admin page is "pure" (it's not been edited by an actual person) then compare BodyJson as well.
			// This is why we'll always generate the bodyJson with the event.

			await InstallInternal(
				listPage,
				singlePage
			);

			await DeleteOldInternal(typeName);
		}

		/// <summary>
		/// Installs the given page(s). It checks if they exist by their URL (or ID, if you provide that instead), and if not, creates them.
		/// </summary>
		/// <param name="pages"></param>
		public void Install(params Page[] pages)
		{
			if (Services.Started)
			{
				Task.Run(async () =>
				{
					await InstallInternal(pages);
				});
			}
			else
			{
				Events.Service.AfterStart.AddEventListener(async (Context ctx, object src) =>
				{
					await InstallInternal(pages);
					return src;
				});
			}
		}
			
		/// <summary>
		/// Used to uninstall internal pages that were once in use such as /en-admin/typeName/:id or /en-admin/typeName/:adminId
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		private async ValueTask DeleteOldInternal(string typeName)
        {
			var context = new Context();

			// We need to look for both old types.
			var adminIdUrl = "/en-admin/" + typeName + "/:adminId";
			var oldIdUrl = "/en-admin/" + typeName + "/:id";

			// Get any pages by those URLs:
			var pages = await Where("Url=? or Url=?", DataOptions.NoCacheIgnorePermissions).Bind(adminIdUrl).Bind(oldIdUrl).ListAll(context);

			foreach (var page in pages)
            {
				// If we have any pages from the previous hit, time to delete them!
				await Delete(context, page.Id, DataOptions.IgnorePermissions);
            }
        }

		/// <summary>
		/// Installs the given page(s). It checks if they exist by their URL (or ID, if you provide that instead), and if not, creates them.
		/// </summary>
		/// <param name="pages"></param>
		private async ValueTask InstallInternal(params Page[] pages)
		{
			var context = new Context();

			// Get the set of pages which we'll match by ID:
			var idSet = pages.Where(page => page.Id != 0);

			if (idSet.Any())
			{
				IEnumerable<uint> ids = idSet.Select(Page => Page.Id);

				// Get the pages by those IDs:
				var existingPages = (await Where("Id=[?]", DataOptions.NoCacheIgnorePermissions)
						.Bind(ids)
						.ListAll(context)).ToDictionary(page => page.Id);

				// For each page to consider for install..
				foreach (var page in idSet)
				{
					// If it doesn't already exist, create it.
					if (!existingPages.ContainsKey(page.Id))
					{
						await Create(context, page, DataOptions.IgnorePermissions);
					}
				}
			}
				
			// Get the set of pages which we'll match by URL:
			var urlSet = pages.Where(page => page.Id == 0);

			if (urlSet.Any())
			{
				IEnumerable<string> urls = urlSet.Select(Page => Page.Url);

				// Get the pages by those URLs:
				var existingPages = (await Where("Url=[?]", DataOptions.NoCacheIgnorePermissions)
						.Bind(urls)
						.ListAll(context));

				var existingPagesLookup = new Dictionary<string, Page>();

				foreach (var pg in existingPages)
				{
					existingPagesLookup[pg.Url] = pg;
				}

				// For each page to consider for install..
				foreach (var page in urlSet)
				{
					// If it doesn't already exist, create it.
					if (!existingPagesLookup.ContainsKey(page.Url))
					{
						await Create(context, page, DataOptions.IgnorePermissions);
					}
				}
			}
		}
	}
    
}
