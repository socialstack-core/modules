using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;


namespace Api.Pages
{
	/// <summary>
	/// Handles pages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PageService : AutoService<Page>, IPageService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PageService() : base(Events.Page)
        {
			// If you don't have a homepage or admin area, this'll create them:
			Task.Run(async () =>
			{
				await Install(
					new Page()
					{
						Url = "/",
						BodyJson = @"{
							""content"": ""Welcome to your new SocialStack instance. This text comes from the pages table in your database in a format called canvas JSON - you can read more about this format in the documentation.""
						}"
					},
					new Page()
					{
						Url = "/en-admin",
						BodyJson = @"{
							""module"": ""Admin/Pages/Default"",
							""content"": [
								{
									""module"": ""Admin/Tile"",
									""content"": [
										""Welcome to the administration area. Pick what you'd like to edit on the left.""
									]
								}
							]
						}"
					},
					new Page()
					{
						Url = "/en-admin/login",
						BodyJson = @"{
							""module"": ""Admin/Pages/Landing"",
							""content"": [
								{
									""module"": ""Admin/Tile"",
									""content"": [
										{
											""module"":""Admin/LoginForm""
										}
									]
								}
							]
						}"
					},
					new Page()
					{
						Url = "/en-admin/register",
						BodyJson = @"{
							""module"": ""Admin/Pages/Landing"",
							""content"": [
								{
									""module"": ""Admin/Tile"",
									""content"": [
										{
											""module"":""Admin/RegisterForm""
										}
									]
								}
							]
						}"
					}
				);
				
			});

			// Install the admin pages. Special case as it's the page service itself - we'll want to wait until it's at least finished
			// this/ its own constructor so we can say for sure that both it and anything else (like the navmenu service) are available.
			Events.ServicesAfterStart.AddEventListener((Context ctx, object src) => {
				
				InstallAdminPages("Pages", "fa:fa-paragraph", new string[] { "id", "url", "title" });

				return Task.FromResult(src);
			});

		}

		/// <summary>
		/// Installs generic admin pages using the given fields to display on the list page.
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="fields"></param>
		public async Task InstallAdminPages(string typeName, string[] fields)
		{
			var fieldString = Newtonsoft.Json.JsonConvert.SerializeObject(fields);
			typeName = typeName.ToLower();

			await Install(
				new Page{
					Url = "/en-admin/" + typeName,
					BodyJson = @"{
						""data"" : {
							""endpoint"" : """ + typeName + @""",
							""fields"" : " + fieldString + @"
						},
						""module"" : ""Admin/Pages/List""
					}"
				},
				new Page {
					Url = "/en-admin/" + typeName + "/:id",
					BodyJson = @"{
						""data"" : {
							""endpoint"" : """ + typeName + @""",
							""id"" : {
								""name"" : ""id"",
								""type"" : ""urlToken""
							}
						},
						""module"" : ""Admin/Pages/AutoEdit""
					}"
				}
			);
		}

		/// <summary>
		/// Installs the given page(s). It checks if they exist by their URL (or ID, if you provide that instead), and if not, creates them.
		/// </summary>
		/// <param name="pages"></param>
		public async Task Install(params Page[] pages)
		{
			var context = new Context();

			// Get the set of pages which we'll match by ID:
			var idSet = pages.Where(page => page.Id != 0);

			if (idSet.Any())
			{
				// Get the pages by those URLs:
				var filter = new Filter<Page>();
				filter.Id(idSet.Select(Page => Page.Url));
				var existingPages = (await List(context, filter)).ToDictionary(page => page.Id);

				// For each page to consider for install..
				foreach (var page in idSet)
				{
					// If it doesn't already exist, create it.
					if (!existingPages.ContainsKey(page.Id))
					{
						await Create(context, page);
					}
				}
			}
				
			// Get the set of pages which we'll match by URL:
			var urlSet = pages.Where(page => page.Id == 0);

			if (urlSet.Any())
			{
				// Get the pages by those URLs:
				var filter = new Filter<Page>();
				filter.EqualsSet("Url", urlSet.Select(Page => Page.Url));
					
				var existingPages = (await List(context, filter)).ToDictionary(page => page.Url);

				// For each page to consider for install..
				foreach (var page in urlSet)
				{
					// If it doesn't already exist, create it.
					if (!existingPages.ContainsKey(page.Url))
					{
						await Create(context, page);
					}
				}
			}
		}
	}
    
}
