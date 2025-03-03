using System;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using Api.Permissions;

namespace Api.Layouts
{
	/// <summary>
	/// Handles layouts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class LayoutService : AutoService<Layout>
    {

		private PageService pageService;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public LayoutService(
			PageService pageService
		) : base(Events.Layout)
        {
			this.pageService = pageService;
			// Example admin page install:
			InstallAdminPages("Layouts / Templates", "fa:fa-rocket", ["id", "name"]);
			InitEvents();
		}

		private void InitEvents()
		{
			Events.Service.AfterStart.AddEventListener(async (context, source) => {
				await EnsureSetup(context);
				return this;
			});
			Events.Page.BeforeCreate.AddEventListener(async (context, page) => {

				var targetLayout = await Get(context, page.LayoutId);

				if (targetLayout == null)
				{
					targetLayout = await Where("Key = ?", DataOptions.IgnorePermissions).Bind("blank_template").First(context);
					page.LayoutId = targetLayout.Id;
				}

				page.BodyJson = targetLayout.LayoutJson;
				
				return page;
			});
		}

        private async ValueTask EnsureSetup(Context context)
        {
			var blankTemplate = await Where("Key = ?", DataOptions.IgnorePermissions).Bind("blank_template").First(context);

			if (blankTemplate == null)
			{
				blankTemplate = new Layout()
				{
					Id = 1,
					Key = "blank_template",
					Name = "Blank Template",
					LayoutJson = "{}"
				};

				await Create(context, blankTemplate, DataOptions.IgnorePermissions);
			}

			// make sure the admin page create has been created.
			var createPage = await pageService.Where("Url = ?", DataOptions.IgnorePermissions).Bind("/en-admin/page/add").First(context);

			if (createPage == null)
			{
				createPage = new Page()
				{
					Url = "/en-admin/page/add",
					Title = "Create Page",
					BodyJson = @"{
						""c"": {
							""t"": ""Admin/Page/Create"",
							""d"": {},
							""i"": 2
						},
						""i"": 3
					}"
				};

				await pageService.Create(context, createPage, DataOptions.IgnorePermissions);
			}
        }
    }
    
}
