using Api.Database;

namespace Api.Users;


public partial class User
{

	/// <summary>
	/// The contact phone number for this user.
	/// </summary>
	[DatabaseField(Length = 20)]
	public string ContactNumber;

}