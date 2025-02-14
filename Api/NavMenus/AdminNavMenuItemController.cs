using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.NavMenus
{
	/// <summary>
	/// Handles admin nav menu item endpoints.
	/// </summary>
	[Route("v1/adminnavmenuitem")]
	public partial class AdminNavMenuItemController : AutoController<AdminNavMenuItem>
	{
		/// <summary>
		/// Overriden endpoint 
		/// </summary>
		/// <param name="filters"></param>
		/// <param name="includes"></param>
		/// <returns></returns>
		[HttpPost("list")]
		public override async ValueTask List([FromBody] JObject filters, [FromQuery] string includes = null)
		{
			var service = _service as AdminNavMenuItemService;
			var context = await Request.GetContext();
			
			var allItems = await service.Where().ListAll(context);
			var userCanAccess = new List<AdminNavMenuItem>();

			var role = context.Role;

			foreach(var item in allItems)
			{
				if (item.VisibilityRuleJson is null) {
					// No specific rule, so allow access
					userCanAccess.Add(item);
					continue;
				}

				// parse the JSON using Newtonsoft, iterate rules
				var permissionRule = JsonConvert.DeserializeObject<AdminNavPermissions>(item.VisibilityRuleJson);
				var capabilities = Capabilities.GetAllCurrent();
				var isGranted = true;

				foreach(var permission in permissionRule.RequiredPermissions) 
				{
					isGranted = await role.IsGranted(
						capabilities.First(
							capability => capability.Name == permission
						),
						context,
						null, 
						false
					);

					if (!isGranted) {
						break;
					}
				}

				if (isGranted) {
					userCanAccess.Add(item);
				}
			}

			await OutputJson(context, userCanAccess, includes);
			
		}
    }
}
