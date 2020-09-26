using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Users;
using Api.Eventing;
using Api.Contexts;
using Newtonsoft.Json;
using Api.Configuration;

namespace Api.TwoFactorGoogleAuth
{
	/// <summary>
	/// Google authenticator based 2FA.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class TwoFactorGoogleAuthService : ITwoFactorGoogleAuthService
	{
		private GoogleAuthenticator _ga;

		private string siteUrl;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TwoFactorGoogleAuthService()
        {
			_ga = new GoogleAuthenticator();

			siteUrl = AppSettings.Configuration["PublicUrl"].Replace("https:", "").Replace("http:", "").Replace("/", "");

			// Hook up to the UserOnAuthenticate event:
			Events.UserOnAuthenticate.AddEventListener((Context context, LoginResult result, UserLogin loginDetails) => {

				if (result == null || result.User == null || result.MoreDetailRequired != null)
				{
					// We only trigger if something has already logged in and nothing before us requires more detail.
					return new ValueTask<LoginResult>(result);
				}

				// 2FA enabled?
				if (result.User.TwoFactorSecret == null || result.User.TwoFactorSecret.Length != 10)
				{
					// Nope.
					return new ValueTask<LoginResult>(result);
				}

				// It's enabled - pin submitted or do we require it?
				if (string.IsNullOrEmpty(loginDetails.Google2FAPin)) {

					// It's required. This blocks the complete token from being generated.
					result.MoreDetailRequired = "2fa";

					return new ValueTask<LoginResult>(result);
				}

				if(!Validate(result.User.TwoFactorSecret, loginDetails.Google2FAPin))
				{
					// Bad pin.
					return new ValueTask<LoginResult>((LoginResult)null);
				}

				return new ValueTask<LoginResult>(result);
			}, 20);

			// Note: the 20 priority is important. It means we'll run this event always after the default auth handlers (10).
			// I.e. some other handler auths the user, we then 2FA them.
			
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