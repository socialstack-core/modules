using Api.Database;
using Api.GuestUsers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Api.Payments;

public partial class ShoppingCart
{

	/// <summary>
	/// The guest user created for this purchase
	/// </summary>
	public uint? GuestUserId;

	/// <summary>
	/// JsonString containing the guest details for the cart (used for generating address etc)
	/// </summary>
	public JsonString GuestDetailsJson;

	private GuestDetails _guestDetails;

	/// <summary>
	/// The guest details for this cart
	/// </summary>
	[JsonIgnore]
	public GuestDetails GuestDetails
	{
		get
		{
			if (_guestDetails == null && GuestDetailsJson.HasValue)
			{
				_guestDetails = JsonConvert.DeserializeObject<GuestDetails>(GuestDetailsJson.ToString());
			}
			return _guestDetails;
		}
		set => _guestDetails = value;
	}

	/// <summary>
	/// Update the GuestDetails json for this cart.
	/// </summary>
	public void UpdateGuestDetailsJson()
	{
		if (_guestDetails == null)
		{
			GuestDetailsJson = new JsonString(null);
			return;
		}

		GuestDetailsJson = new JsonString(JsonConvert.SerializeObject(_guestDetails, jsonSettings));

		//reset _guestDetails in the cache
		_guestDetails = null;
	}

	/// <summary>
	/// Json serialization settings for canvases
	/// </summary>
	private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
	{
		ContractResolver = new DefaultContractResolver
		{
			NamingStrategy = new CamelCaseNamingStrategy()
		},
		Formatting = Formatting.None
	};


}