using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using Api.Contexts;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;
using Api.NavMenuItems;

namespace Api.NavMenus
{
    /// <summary>
    /// Handles nav menu endpoints.
    /// </summary>

    [Route("v1/navmenu")]
	[ApiController]
	public partial class NavMenuController : ControllerBase
    {
        private INavMenuService _navMenus;
        private INavMenuItemService _navMenuItems;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public NavMenuController(
			INavMenuService navMenus,
			INavMenuItemService navMenuItems
		)
        {
			_navMenus = navMenus;
			_navMenuItems = navMenuItems;

		}

		/// <summary>
		/// GET /v1/navmenu/2/
		/// Returns the navmenu data for a single menu.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<NavMenu> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _navMenus.Get(context, id);
			return await Events.NavMenuLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// GET /v1/navmenu/key/primary/
		/// Returns the navmenu items for a single menu by its key.
		/// </summary>
		[HttpGet("key/{key}")]
		public async Task<Set<NavMenuItem>> Load([FromRoute] string key)
		{
			var context = Request.GetContext();
			var result = await _navMenus.Get(context, key);

			if (result == null)
			{
				return null;
			}

			var results = await _navMenuItems.ListByMenu(context, result.Id);
			return new Set<NavMenuItem>() { Results = results };
		}

		/// <summary>
		/// DELETE /v1/navmenu/2/
		/// Deletes a navmenu
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _navMenus.Get(context, id);
			result = await Events.NavMenuDelete.Dispatch(context, result, Response);

			if (result == null || !await _navMenus.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/navmenu/list
		/// Lists all nav menus available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<NavMenu>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/navmenu/list
		/// Lists filtered nav menus available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<NavMenu>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<NavMenu>(filters);

			filter = await Events.NavMenuList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _navMenus.List(context, filter);
			return new Set<NavMenu>() { Results = results };
		}

		/// <summary>
		/// POST /v1/navmenu/
		/// Creates a new navmenu. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<NavMenu> Create([FromBody] NavMenuAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var navMenu = new NavMenu
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, navMenu))
			{
				return null;
			}

			form = await Events.NavMenuCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			navMenu = await _navMenus.Create(context, form.Result);

			if (navMenu == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return navMenu;
        }
		
		/// <summary>
		/// POST /v1/navmenu/1/
		/// Updates a nav menu with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<NavMenu> Update([FromRoute] int id, [FromBody] NavMenuAutoForm form)
		{
			var context = Request.GetContext();

			var navMenu = await _navMenus.Get(context, id);
			
			if (!ModelState.Setup(form, navMenu)) {
				return null;
			}

			form = await Events.NavMenuUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			navMenu = await _navMenus.Update(context, form.Result);

			if (navMenu == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return navMenu;
		}

    }

}
