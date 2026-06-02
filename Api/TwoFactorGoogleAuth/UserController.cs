using Api.Contexts;
using Api.Database;
using Api.Emails;
using Api.Permissions;
using Api.Startup;
using Api.Startup.Routing;
using Api.TwoFactorGoogleAuth;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Api.Users
{
    /// <summary>
    /// Handles user account endpoints.
    /// </summary>
	public partial class UserController
    {
		/// <summary>
		/// Sets up 2FA. Can only do this for yourself.
		/// </summary>
		/// <returns></returns>
		[HttpPost("setup2fa/confirm")]
		public async ValueTask<object> TwoFactorSetup(Context context, [FromBody] PinCarrierModel pinCarrier)
		{
			if (context == null || pinCarrier == null)
			{
				return null;
			}
			
			var user = context.User;
			
			if(user.TwoFactorSecretPending == null)
			{
				return null;
			}
			
			// Attempt to validate the pin:
			if(Services.Get<TwoFactorGoogleAuthService>().Validate(user.TwoFactorSecretPending, pinCarrier.Pin))
			{
				// Ok! Successful setup.
				// Apply pending -> active right now.
				
				await _service.Update(context, user, (Context c, User u, User originalUser) => {
					u.TwoFactorSecret = u.TwoFactorSecretPending;
					u.TwoFactorSecretPending = null;
				});
			}
			
			return new {
				success = true
			};
		}
		
		/// <summary>
		/// Sets up 2FA. Can only do this for yourself.
		/// </summary>
		/// <returns></returns>
		[HttpGet("setup2fa/newkey")]
		public async ValueTask<FileContent?> TwoFactorNewKey(Context context)
		{
			if (context == null)
			{
				return null;
			}
			
			// Get ctx user:
			var user = context.User;
			
			if(user == null)
			{
				return null;
			}
			
			var twoFA = Services.Get<TwoFactorGoogleAuthService>();
			
			// Generate a key and apply to pending:
			var key = twoFA.GenerateKey();
			
			user = await _service.Update(context, user, (Context c, User u, User originalUser) => {
				user.TwoFactorSecretPending = key;
			});
			
			var imageBytes = await twoFA.GenerateProvisioningImage(context, key);
			return new FileContent(imageBytes, "image/jpeg");
		}
    }
	
	/// <summary>
	/// Carries a 2FA pin.
	/// </summary>
	public class PinCarrierModel{
		
		/// <summary>
		/// The users first pin.
		/// </summary>
		public string Pin;
		
	}
	
}
