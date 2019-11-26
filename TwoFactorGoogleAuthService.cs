using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Users;
using Api.Eventing;
using Api.Contexts;
using Newtonsoft.Json;

namespace Api.TwoFactorGoogleAuth
{
	/// <summary>
	/// Google authenticator based 2FA.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class TwoFactorGoogleAuthService : ITwoFactorGoogleAuthService
	{
		private GoogleAuthenticator _ga;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TwoFactorGoogleAuthService()
        {
			_ga = new GoogleAuthenticator();

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
					// Nope.
					return result;
				}

				// It's enabled - pin submitted or do we require it?
				if (string.IsNullOrEmpty(loginDetails.Google2FAPin)) {

					// It's required. This blocks the complete token from being generated.
					result.MoreDetailRequired = "2fa";

					return result;
				}

				var pin = loginDetails.Google2FAPin.Trim();

				// Submitted a pin - does it match the one we want?
				var currentInterval = _ga.CurrentInterval;

				if (pin != _ga.GeneratePin(result.User.TwoFactorSecret, currentInterval))
				{
					// One more chance - they might've been a bit slow, so try the previous pin too:
					if (pin != _ga.GeneratePin(result.User.TwoFactorSecret, currentInterval - 1))
					{
						// Definitely a bad pin.
						return null;
					}
				}

				return result;
			}, 20);

			// Note: the 20 priority is important. It means we'll run this event always after the default auth handlers (10).
			// I.e. some other handler auths the user, we then 2FA them.
			
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

	}
	
}