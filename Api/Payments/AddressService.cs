using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using System.Threading.Tasks;

namespace Api.Addresses;

public partial class AddressService
{
	/// <summary>
	/// Gets the address stream of addresses for the current context's billing and delivery addresses.
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	public async ValueTask<ContentStream<Address, uint>?> GetCartAddressStream(Context context)
	{
		if (context.UserId == 0)
		{
			return null;
		}

		Filter<Address, uint> filter = null;
		filter = await Events.Address.GetCartAddressFilter.Dispatch(context, filter);

		if (filter == null)
		{
			filter = Where("UserId=? and AddressType=?").Bind(context.UserId).Bind((uint)0);
		}

		var streamer = GetResults(filter);
		return streamer;
	}
	
}
