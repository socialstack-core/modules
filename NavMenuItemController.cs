using Microsoft.AspNetCore.Mvc;


namespace Api.NavMenuItems
{
	/// <summary>
	/// Handles nav menu item endpoints.
	/// </summary>
	[Route("v1/navmenu/item")]
	public partial class NavMenuItemController : AutoController<NavMenuItem>
	{
    }
}