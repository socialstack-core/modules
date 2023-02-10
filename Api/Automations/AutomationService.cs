using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json;
using System;
using Api.Contexts;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Pages;
using Api.CanvasRenderer;
using Api.NavMenus;

namespace Api.Automations
{
	/// <summary>
	/// Indicates the set of available automations.
	/// </summary>

	public partial class AutomationService : AutoService<AutomationStructure, uint>
	{

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AutomationService(PageService pages, AdminNavMenuItemService adminNav) : base(Events.AvailableAutomations)
		{

			// Install custom admin page which lists automations. This install mechanism allows it to be modified if needed.
			Task.Run(async () => {

				var automationsPageCanvas = new CanvasNode("Admin/Layouts/Automations");

				var listPage = new Page
				{
					Url = "/en-admin/automations",
					BodyJson = "",
					Title = "Automations"
				};

				// Trigger an event to state that an admin page is being installed:
				// - Use this event to inject additional nodes into the page, or change it however you'd like.
				listPage = await Events.Page.BeforeAdminPageInstall.Dispatch(new Context(), listPage, automationsPageCanvas, typeof(AutomationStructure), AdminPageType.List);
				listPage.BodyJson = automationsPageCanvas.ToJson();

				pages.Install(listPage);

				// Add nav menu item too.
				await adminNav.InstallAdminEntry("/en-admin/automations", "fa:fa-clock", "Automations");

			});
		}

		private DateTime _cacheTime;
		private AutomationStructure _structure;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public AutomationStructure GetStructure(Context context)
		{
			var cronScheduler = Events.GetCronScheduler();
			var latestUpdate = cronScheduler.LastUpdated;

			if (_structure != null)
			{
				// Cache time ok?
				if (_cacheTime == latestUpdate)
				{
					// yep!
					return _structure;
				}

			}

			_cacheTime = latestUpdate;
			var structure = new AutomationStructure();
			_structure = structure;

			structure.Results = new List<Automation>();

			// For each automation in the scheduler..
			foreach (var kvp in cronScheduler.AutomationsByName)
			{
				var automation = kvp.Value;
				structure.Results.Add(
					new Automation(automation) {
						Name = automation.Name,
						CronDescription = ExpressionDescriptor.GetDescription(automation.Cron),
						Cron = automation.Cron,
					}
				);
			}

			return structure;
		}

	}
    
}
