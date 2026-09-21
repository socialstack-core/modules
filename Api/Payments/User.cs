using Newtonsoft.Json;
namespace Api.Users;

public partial class User
{
	/// <summary>
	/// This user's shopping cart, or zero.
	/// </summary>
	[JsonIgnore]
	public uint ShoppingCartId;
}