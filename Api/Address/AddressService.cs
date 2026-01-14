
using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using Api.PasswordResetRequests;
using System.Threading.Tasks;

namespace Api.Addresses;

/// <summary>
/// Handles addresses.
/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
/// </summary>
public partial class AddressService : AutoService<Address>
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public AddressService(PageService pages) : base(Events.Address)
	{

        InstallAdminPages("Addresses", "fa:fa-address-book", ["id", "name", "line1"]);

        pages.Install(
			new PageBuilder()
			{
				Url="/address_book",
				Key = "address_book",
				Title = "My addresses",
				BuildBody = (PageBuilder builder) =>
				{
					return builder.AddTemplate(
						new CanvasRenderer.CanvasNode("UI/Address/Book")
                        .With("addressType", 0)
                    );
				}
			}
		);

		Events.Address.BeforeCreate.AddEventListener(async (Context context, Address address) =>
		{
			//Add an anonymous key to the address
			Address matchingAddress = null;
			while(address.AnonKey == null || matchingAddress != null)
			{
				address.AnonKey = RandomToken.Generate(16);
				matchingAddress = await Where("AnonKey = ?", DataOptions.IgnorePermissions).Bind(address.AnonKey).First(context);
			}

			return address;
		});

	}

	/// <summary>
	/// Gets the ISO 3166 tax jurisdiction of the given address by ID.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="addressId"></param>
	/// <returns></returns>
	public async ValueTask<string> GetTaxJurisdiction(Context context, uint addressId)
	{
		var addr = await Get(context, addressId);
		return await GetTaxJurisdiction(context, addr);
	}

	/// <summary>
	/// Gets the ISO 3166 tax jurisdiction of the given address.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="address"></param>
	/// <returns></returns>
	public async ValueTask<string> GetTaxJurisdiction(Context context, Address address)
	{
		if (address == null)
		{
			var locale = await context.GetLocale();
			return locale.DefaultTaxJurisdiction;
		}

		// Todo! Can be e.g. specific US states etc.
		return "GB";
	}
}
