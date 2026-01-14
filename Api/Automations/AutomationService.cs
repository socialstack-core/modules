using System;
using Api.Contexts;
using System.Collections.Generic;
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
    public partial class AutomationService : AutoService
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AutomationService(PageService pages, AdminNavMenuItemService adminNav)
		{

			// Install custom admin page which lists automations.
			// This install mechanism allows it to be modified if needed.
			pages.Install(new PageBuilder()
			{
				Title = "Automations",
				AdminRelativeUrl = "automations",
				AdminNavMenuIcon = "fa:fa-clock",
				BuildBody = (PageBuilder builder) => {
					return builder.AddTemplate(
						new CanvasNode("Admin/Layouts/Automations")
					);
				}
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

			if (cronScheduler == null)
			{
				var blankStructure = new AutomationStructure();
				blankStructure.Results = new List<Automation>();
				return blankStructure;
			}

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
						Description = automation.Description,
						CronDescription = ExpressionDescriptor.GetDescription(automation.Cron),
						Cron = automation.Cron,
					}
				);
			}

			return structure;
		}

	}
    
}
