using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.NavMenus
{
	/// <summary>
	/// Handles nav menu item endpoints.
	/// </summary>
	[Route("v1/navmenuitem")]
	public partial class NavMenuItemController : AutoController<NavMenuItem>
	{
		
		/// <summary>
		/// GET /v1/navmenuitem/key/primary/
		/// Returns the navmenu items for a single menu by its key.
		/// </summary>
		[HttpGet("key/{key}")]
		public async Task<ListWithTotal<NavMenuItem>> Load([FromRoute] string key)
		{
			var context = Request.GetContext();
			var menu = await Services.Get<INavMenuService>().Get(context, key);

			if (menu == null)
			{
				return null;
			}
			
			var filter = new Filter<NavMenuItem>();
			filter.Equals("NavMenuId", menu.Id);
			filter = await _service.EventGroup.List.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			ListWithTotal<NavMenuItem> response;
				
			// Not paginated or requsetor doesn't care about the total.
			var results = await _service.List(context, filter);

			response = new ListWithTotal<NavMenuItem>()
			{
				Results = results
			};
				
			if (filter.PageSize == 0)
			{
				// Trivial instance - pagination is off so the total is just the result set length.
				response.Total = results == null ? 0 : results.Count;
			}

			response.Results = await _service.EventGroup.Listed.Dispatch(context, response.Results, Response);

			return response;
		}
		
    }
}