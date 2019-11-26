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

namespace Api.NavMenuItems
{
    /// <summary>
    /// Handles nav menu item endpoints.
    /// </summary>

    [Route("v1/navmenu/item")]
	[ApiController]
	public partial class NavMenuItemController : ControllerBase
    {
        private INavMenuItemService _navMenuItems;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public NavMenuItemController(
			INavMenuItemService navMenuItems
        )
        {
			_navMenuItems = navMenuItems;
        }

		/// <summary>
		/// GET /v1/navmenu/item/2/
		/// Returns the navmenu item data for a single item.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<NavMenuItem> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _navMenuItems.Get(context, id);
			return await Events.NavMenuItemLoad.Dispatch(context, result, Response);
		}
		
		/// <summary>
		/// DELETE /v1/navmenu/item/2/
		/// Deletes a navmenu
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _navMenuItems.Get(context, id);
			result = await Events.NavMenuItemDelete.Dispatch(context, result, Response);

			if (result == null || !await _navMenuItems.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/navmenu/item/list
		/// Lists all nav menus items available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<NavMenuItem>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/navmenu/item/list
		/// Lists filtered nav menus available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<NavMenuItem>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<NavMenuItem>(filters);

			filter = await Events.NavMenuItemList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _navMenuItems.List(context, filter);
			return new Set<NavMenuItem>() { Results = results };
		}

		/// <summary>
		/// POST /v1/navmenu/item/
		/// Creates a new navmenu item. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<NavMenuItem> Create([FromBody] NavMenuItemAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var navMenu = new NavMenuItem
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, navMenu))
			{
				return null;
			}

			form = await Events.NavMenuItemCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			navMenu = await _navMenuItems.Create(context, form.Result);

			if (navMenu == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return navMenu;
        }
		
		/// <summary>
		/// POST /v1/navmenu/item/1/
		/// Updates a nav menu item with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<NavMenuItem> Update([FromRoute] int id, [FromBody] NavMenuItemAutoForm form)
		{
			var context = Request.GetContext();

			var navMenu = await _navMenuItems.Get(context, id);
			
			if (!ModelState.Setup(form, navMenu)) {
				return null;
			}

			form = await Events.NavMenuItemUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			navMenu = await _navMenuItems.Update(context, form.Result);

			if (navMenu == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return navMenu;
		}

    }

}
