using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Users;
using Api.Eventing;
using Api.Contexts;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;

namespace Api.PasswordAuth
{
	/// <summary>
	/// The default password based authentication scheme. Note that other variants of this exist such as one which uses
	/// the same password hash format as Wordpress for easy porting.
	/// You can either add additional schemes or just outright replace this one if you want something else.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PasswordAuthService
    {
		
		/// <summary>
		/// Min password length.
		/// </summary>
		public int MinLength = 10;
		
		/// <summary>
		/// True if new passwords should be checked for public exposure.
		/// </summary>
		public bool CheckIfExposed = true;
		
		private UserService _users;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PasswordAuthService(UserService users)
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

				// Lockout check.
				var now = DateTime.UtcNow;

				if (user.FailedLoginTimeUtc.HasValue && user.LoginAttempts>=5 && (now - user.FailedLoginTimeUtc.Value).TotalMinutes < 20)
				{
					// Locked
					throw new PublicException("Locked account - try again in 20 minutes", "locked_account");
				}
				
				if (!PasswordStorage.VerifyPassword(loginDetails.Password, hashToCheck))
				{
					// Update login attempts:
					if (!user.FailedLoginTimeUtc.HasValue || (now - user.FailedLoginTimeUtc.Value).TotalMinutes >= 20)
					{
						user.FailedLoginTimeUtc = DateTime.UtcNow;
						user.LoginAttempts = 1;
						await _users.Update(context, user);
					}
					else
					{
						user.LoginAttempts++;
						await _users.Update(context, user);
					}
					
					// Nope!
					return null;
				}

				// Successful login - return the user:
				return new LoginResult() {
					User = user
				};
			});

			// Handle user creation too.
			// Add a mapper for a field called Password -> PasswordHash.
			Events.User.BeforeSettable.AddEventListener((Context context, JsonField<User> field) =>
			{
				if(field == null)
				{
					// Something else doesn't want this field to show.
					return new ValueTask<JsonField<User>>(field);
				}
				
				if (field.Name == "PasswordHash")
				{
					// Use this name in the JSON:
					field.Name = "Password";

					// Set value method:
					field.OnSetValue.AddEventListener(async (Context ctx, object value, User target, JToken token) => {
						
						// Get the pwd and attempt to enforce it (throws if it fails):
						var pwd = token.ToObject<string>();
						await EnforcePolicy(pwd);
						
						// Ok:
						value = PasswordStorage.CreateHash(pwd);
						return value;

					});

				}

				return new ValueTask<JsonField<User>>(field);
			});
			
		}
		
		/// <summary>
		/// Enforces pwd policy on the given password.
		/// </summary>
		public async Task EnforcePolicy(string pwd)
		{
			if(pwd == null)
			{
				throw new PublicException("Password must be longer than " + MinLength + " characters.", "password_length");
			}

			pwd = pwd.Trim();

			if(pwd.Length < MinLength)
			{
				throw new PublicException("Password must be longer than " + MinLength + " characters.", "password_length");
			}

			if (CheckIfExposed)
			{
				var isExposed = false;
				try
				{
					isExposed = await PwnedPasswords.IsPasswordPwned(pwd, System.Threading.CancellationToken.None);
				}
				catch(Exception e)
				{
					// Unavailable - we'll let it through.
					Console.WriteLine(e.ToString());
				}

				if (isExposed)
				{
					throw new PublicException("Your password is weak as it has been seen in public data leaks.", "password_public");
				}
			}
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
		
		/// <summary>
		/// Failed login attempt counter.
		/// </summary>
		[JsonIgnore]
		public int LoginAttempts;
		
		/// <summary>
		/// Time of first failed login which updated the login attempt counter.
		/// </summary>
		[JsonIgnore]
		public DateTime? FailedLoginTimeUtc;
		
	}

}
