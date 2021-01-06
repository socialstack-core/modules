using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Users;
using Api.Eventing;
using Api.Contexts;
using Newtonsoft.Json;
using Api.Configuration;
using Api.Permissions;
using System;
using System.Text;
using Microsoft.Extensions.Configuration;


namespace Api.TwoFactorGoogleAuth
{
	/// <summary>
	/// Google authenticator based 2FA.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class TwoFactorGoogleAuthService
	{
		private readonly GoogleAuthenticator _ga;
		private readonly TwoFactorAuthConfig config;
		private readonly string siteUrl;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TwoFactorGoogleAuthService(UserService _users)
        {
			_ga = new GoogleAuthenticator();
			config = AppSettings.GetSection("TwoFactorAuth").Get<TwoFactorAuthConfig>();

			if (config == null)
			{
				config = new TwoFactorAuthConfig();
			}


			siteUrl = AppSettings.Configuration["PublicUrl"].Replace("https:", "").Replace("http:", "").Replace("/", "");

			// Hook up to the UserOnAuthenticate event:
			Events.UserOnAuthenticate.AddEventListener(async (Context context, LoginResult result, UserLogin loginDetails) => {

				if (result == null || result.User == null || result.MoreDetailRequired != null)
				{
					// We only trigger if something has already logged in and nothing before us requires more detail.
					return result;
				}

				// 2FA enabled?
				if (result.User.TwoFactorSecret == null || result.User.TwoFactorSecret.Length != 10)
				{
					// If it's required for this account and the setup meta has not been submitted, require setup.
					var user = result.User;
					var role = Roles.Get(user.Role);
					
					if(config.Required || (config.RequiredForAdmin && role != null && role.CanViewAdmin))
					{
						if (string.IsNullOrEmpty(loginDetails.Google2FAPin) || result.User.TwoFactorSecretPending == null)
						{
							// Setup required. Create a key if there isn't one:
							if(user.TwoFactorSecretPending == null)
							{
								var key = GenerateKey();
								user.TwoFactorSecretPending = key;
								user = await _users.Update(context, user);
								result.User = user;
							}
							
							result.MoreDetailRequired = new {
								module = "UI/TwoFactorGoogleSetup",
								data = new {
									loginForm = true,
									setupUrl = _ga.GetProvisionUrl(siteUrl, result.User.TwoFactorSecretPending)
								}
							};
						}
						else
						{
							// Complete the setup now:
							if(Validate(result.User.TwoFactorSecretPending, loginDetails.Google2FAPin))
							{
								// Apply pending -> active right now.
								user.TwoFactorSecret = user.TwoFactorSecretPending;
								user.TwoFactorSecretPending = null;
								user = await _users.Update(context, user);
								result.User = user;
							}
							else
							{
								// Bad pin.
								return null;
							}
						}
					}
					
					// 2FA not enabled, or being setup. Allow proceed.
					return result;
				}

				// It's enabled - pin submitted or do we require it?
				if (string.IsNullOrEmpty(loginDetails.Google2FAPin)) {

					// It's required. This blocks the complete token from being generated.
					result.MoreDetailRequired = new {
						module = "UI/TwoFactorGoogleSetup",
						data = new {
							loginForm = true
						}
					};
					return result;
				}

				if(!Validate(result.User.TwoFactorSecret, loginDetails.Google2FAPin))
				{
					// Bad pin.
					return null;
				}

				return result;
			}, 20);

			// Note: the 20 priority is important. It means we'll run this event always after the default auth handlers (10).
			// I.e. some other handler auths the user, we then 2FA them.
			
		}
		
		/// <summary>
		/// Checks if the given pin is acceptable for the given hex encoded secret.
		/// </summary>
		public byte[] GenerateProvisioningImage(string secret)
		{
			var bytes = new byte[secret.Length / 2];
			for (var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = Convert.ToByte(secret.Substring(i * 2, 2), 16);
			}
			
			return _ga.GenerateProvisioningImage(siteUrl, bytes);
		}
		
		/// <summary>
		/// Checks if the given pin is acceptable for the given secret.
		/// </summary>
		/// <param name="secret"></param>
		/// <param name="pin"></param>
		/// <returns></returns>
		public bool Validate(byte[] secret, string pin)
		{
			if(string.IsNullOrEmpty(pin))
			{
				return false;
			}
			
			pin = pin.Trim();

			// Submitted a pin - does it match the one we want?
			var currentInterval = _ga.CurrentInterval;

			if (pin != _ga.GeneratePin(secret, currentInterval))
			{
				// One more chance - they might've been a bit slow, so try the previous pin too:
				if (pin != _ga.GeneratePin(secret, currentInterval - 1))
				{
					// Definitely a bad pin.
					return false;
				}
			}
			
			return true;
		}

		/// <summary>
		/// Generates an image for the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public byte[] GenerateProvisioningImage(byte[] key)
		{
			return _ga.GenerateProvisioningImage(siteUrl, key);
		}


		/// <summary>
		/// Generates a new secret key.
		/// </summary>
		/// <returns></returns>
		public byte[] GenerateKey()
		{
			var result = new byte[10];
			_ga.GenerateKey(result);
			return result;
		}
	}
	
}

namespace Api.Users {

	public partial class UserLogin {
		/// <summary>
		/// The 2FA pin the user is submitting.
		/// </summary>
		public string Google2FAPin { get; set; }
	}

	public partial class User {

		/// <summary>
		/// The Google authenticator 2FA secret for this user.
		/// </summary>
		[DatabaseField(Length = 10)]
		[JsonIgnore]
		public byte[] TwoFactorSecret;
		
		/// <summary>
		/// Set when setting up 2FA.
		/// </summary>
		[DatabaseField(Length = 10)]
		[JsonIgnore]
		public byte[] TwoFactorSecretPending;

	}
	
}