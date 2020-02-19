using Api.Contexts;
using Api.NavMenuItems;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.NavMenus
{
	/// <summary>
	/// Handles nav menu endpoints.
	/// </summary>
	[Route("v1/navmenu")]
	public partial class NavMenuController : AutoController<NavMenu, NavMenuAutoForm>
	{
        private INavMenuItemService _navMenuItems;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public NavMenuController(
			INavMenuItemService navMenuItems
		)
        {
			_navMenuItems = navMenuItems;
		}
		
		/// <summary>
		/// GET /v1/navmenu/key/primary/
		/// Returns the navmenu items for a single menu by its key.
		/// </summary>
		[HttpGet("key/{key}")]
		public async Task<Set<NavMenuItem>> Load([FromRoute] string key)
		{
			var context = Request.GetContext();
			var result = await (_service as INavMenuService).Get(context, key);

			if (result == null)
			{
				return null;
			}

			var results = await _navMenuItems.ListByMenu(context, result.Id);
			return new Set<NavMenuItem>() { Results = results };
		}

    }
}