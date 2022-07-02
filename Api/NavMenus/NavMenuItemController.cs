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
    }
}