using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Users;
using Api.Eventing;
using Api.Contexts;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

namespace Api.PasswordAuth
{
	/// <summary>
	/// The default password based authentication scheme. Note that other variants of this exist such as one which uses
	/// the same password hash format as Wordpress for easy porting.
	/// You can either add additional schemes or just outright replace this one if you want something else.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PasswordAuthService : IPasswordAuthService
    {
		private IUserService _users;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PasswordAuthService(IUserService users)
        {
			_users = users;

			// Hook up to the UserOnAuthenticate event:
			Events.UserOnAuthenticate.AddEventListener(async (Context context, LoginResult result, UserLogin loginDetails) => {

				if (result != null)
				{
					// Some other auth handler has already authed this user.
					return result;
				}

				// Email/ Password combination here.

				if (string.IsNullOrEmpty(loginDetails.Password))
				{
					// This request probably isn't meant for us.
					return null;
				}

				// First, get the user by the email address:
				var user = await _users.Get(context, loginDetails.EmailOrUsername);
				
				if (user == null)
				{
					return null;
				}

				var hashToCheck = user.PasswordHash;

				if (string.IsNullOrEmpty(hashToCheck))
				{
					// Disabled for this account
					return null;
				}

				if (!PasswordStorage.VerifyPassword(loginDetails.Password, hashToCheck))
				{
					// Nope!
					return null;
				}

				// Successful login - return the user:
				return new LoginResult() {
					User = user
				};
			});

			// Handle user creation too:
			Events.UserCreate.AddEventListener((Context context, UserAutoForm form, HttpResponse resonse) => {

				// Hash the password now:
				form.Result.PasswordHash = PasswordStorage.CreateHash(form.Password);

				return Task.FromResult(form);
			});
			


		}

	}
	
}

namespace Api.Users {

	public partial class UserLogin {
		/// <summary>
		/// The email or username to use.
		/// </summary>
		public string EmailOrUsername;

		/// <summary>
		/// The password to use.
		/// </summary>
		public string Password;
	}

	public partial class User {

		/// <summary>
		/// Seeded password hash
		/// </summary>
		[DatabaseField(Length = 80)]
		[JsonIgnore]
		public string PasswordHash;

	}

	public partial class UserAutoForm {

		/// <summary>
		/// The user's password.
		/// </summary>
		public string Password;

		// Optionally add repetition

		/// <summary>
		/// The user's password again.
		/// </summary>
		// public string PasswordRepeat { get; set; }
	}

}
