using Api.Contexts;
using System.Threading.Tasks;

namespace Api.Users;


public partial class UserService
{
	
	/// <summary>
	/// A true/false if a user exists by email.
	/// </summary>
	public async ValueTask<bool> EmailExists(Context context, string email)
	{
		if(string.IsNullOrEmpty(email))
		{
			return false;
		}
		
		var user = await Where("Email=?").Bind(email.Trim()).First(context);
		
		return (user != null);
	}
	
}