using Api.Contexts;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.NavMenus
{
	/// <summary>
	/// Handles nav menu endpoints.
	/// </summary>
	[Route("v1/navmenu")]
	public partial class NavMenuController : AutoController<NavMenu>
	{
    }
}