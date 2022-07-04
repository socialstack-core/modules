using Api.Contexts;
using Api.Database;
using Api.DatabaseDiff;
using Api.Eventing;
using Api.Startup;
using System;
using System.Threading.Tasks;
using Api.Permissions;
using Api.Videos;
using Api.Users;

namespace Api.UniqueEmail
{

	/// <summary>
	/// Listens out for user create attempts and restricts registering the same email twice.
	/// NB: does not check user update currently.
	/// </summary>
	[EventListener]
	public class ProfileEventListener
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ProfileEventListener()
		{
			
			UserService users = null;
			
			Events.User.BeforeCreate.AddEventListener(async (Context context, User user) => {
				
				if(context == null || user == null){
					return null;
				}
				
				if(users == null){
					users = Services.Get<UserService>();
				}

				if (!string.IsNullOrEmpty(user.Email))
				{
					var userByEmail = await users.GetByEmail(context, user.Email);

					if (userByEmail != null)
					{
						throw new PublicException("That email is already in use. Please try another.", "email_taken");
					}
				}
				return user;
			}, 15);
		}
	}
}