using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;


namespace Api.Automations
{
	/// <summary>
	/// Handles an endpoint which describes available automations.
	/// </summary>

	[Route("v1/automation")]
	[ApiController]
	public partial class AutomationController : ControllerBase
    {
        private AutomationService _automations;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public AutomationController(
			AutomationService automationService
		)
        {
			_automations = automationService;
        }
		
		/// <summary>
		/// GET /v1/automation/list
		/// Returns meta about automations available from this API. Includes endpoints and content types.
		/// </summary>
		[HttpGet("list")]
		public async ValueTask<AutomationStructure> Get()
        {
			var context = await Request.GetContext();
			
			if(context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("automation_list", context);
			}
			
			return _automations.GetStructure(context);
        }
		
		/// <summary>
		/// GET /v1/automation/{name}/run
		/// Runs the named automation and waits for it to complete.
		/// </summary>
		[HttpGet("{name}/run")]
		public async ValueTask Execute([FromRoute] string name)
        {
			var context = await Request.GetContext();
			
			if(string.IsNullOrEmpty(name) || context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("automation_list", context);
			}

			await Events.GetCronScheduler().Trigger(name);
		}
		
    }

}
