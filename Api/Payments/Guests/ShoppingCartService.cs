using Api.Contexts;
using Api.GuestUsers;
using Api.Startup;
using System.Threading.Tasks;
namespace Api.Payments;

public partial class ShoppingCartService
{

	/// <summary>
	/// Update a cart with guest details
	/// </summary>
	/// <param name="context"></param>
	/// <param name="cartId"></param>
	/// <param name="anonKey"></param>
	/// <param name="guestDetails"></param>
	/// <returns></returns>
	public async ValueTask<ShoppingCart> UpdateGuestDetails(Context context, uint cartId, string anonKey, GuestDetails guestDetails)
	{
		// Get the cart:
		ShoppingCart cart = null;

		if (cartId != 0)
		{
			// Get the cart:
			cart = await Get(context, cartId, DataOptions.IgnorePermissions);

			if (cart == null || cart.AnonymousCartKey != anonKey || cart.CheckedOut)
			{
				cart = null;
			}
		}

		if (cart == null)
		{
			throw new PublicException("Cart was not found", "purchase/not_found");
		}

		cart = await Update(context, cart, (Context context, ShoppingCart toUpdate, ShoppingCart original) => {

			toUpdate.GuestDetails = guestDetails;
			toUpdate.UpdateGuestDetailsJson();

		}, DataOptions.IgnorePermissions);


		return cart;
	}



}

