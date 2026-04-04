using Api.Contexts;
using Api.Eventing;
using Api.NavMenus;
using Api.Startup;

namespace Api.Payments;

/// <summary>
/// Used to handle payments.
/// </summary>
[LoadPriority(2)]
public class PaymentsService : AutoService
{
	/// <summary>
	/// Instanced automatically.
	/// </summary>
	/// <param name="adminNavMenuItem"></param>
	public PaymentsService(AdminNavMenuItemService adminNavMenuItem)
	{
		AdminNavMenuItemService.RequiredGroups.Add(new()
		{
			Key = "ecommerce",
			Title = "E-Commerce",
			IconRef = "fa:fa-shopping-cart",
			ParentId = 0
		});
	}
}