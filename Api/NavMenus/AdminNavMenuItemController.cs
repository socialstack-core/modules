using Api.Contexts;
using Api.Permissions;
using Api.Startup;
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
		/// <param name="context"></param>
		/// <returns></returns>
		[HttpPost("list")]
		public override async ValueTask<ContentStream<AdminNavMenuItem, uint>?> List(Context context, [FromBody] ListFilter filters)
		{
			var service = _service as AdminNavMenuItemService;
			return new ContentStream<AdminNavMenuItem, uint>(await service!.ListUserAccessibleNavMenuItems(context), service);
		}
    }
}
