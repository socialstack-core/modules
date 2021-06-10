using Api.Contexts;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.NavMenus
{
	/// <summary>
	/// Handles admin nav menu item endpoints.
	/// </summary>
	[Route("v1/adminnavmenuitem")]
	public partial class AdminNavMenuItemController : AutoController<AdminNavMenuItem>
	{
    }
}