using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Addresses;

public partial class AddressController
{

	/// <summary>
	/// GET /v1/address/cart_addresses
	/// Gets the users billing and delivery saved addresses.
	/// </summary>
	/// <returns></returns>
	[HttpGet("cart_addresses")]
	public virtual ValueTask<ContentStream<Address, uint>?> GetCartAddresses(Context context)
	{
		return (_service as AddressService).GetCartAddressStream(context);
	}

}