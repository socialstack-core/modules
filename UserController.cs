using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Emails;
using Api.Contexts;
using Api.Uploader;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;
using Api.TwoFactorGoogleAuth;
using Api.Startup;
using System.IO;

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
		[HttpGet("setup2fa/{pin}")]
		public async Task<object> TwoFactorSetup(string pin)
		{
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}
			
			var user = await context.GetUser();
			
			if(user.TwoFactorSecretPending == null)
			{
				return null;
			}
			
			// Attempt to validate the pin:
			if(Services.Get<ITwoFactorGoogleAuthService>().Validate(user.TwoFactorSecretPending, pin))
			{
				// Ok! Successful setup.
				// Apply pending -> active right now.
				user.TwoFactorSecret = user.TwoFactorSecretPending;
				user.TwoFactorSecretPending = null;
				await _service.Update(context, user);
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
		public async Task<IActionResult> TwoFactorNewKey()
		{
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}
			
			// Get ctx user:
			var user = await context.GetUser();
			
			if(user == null)
			{
				return null;
			}
			
			var twoFA = Services.Get<ITwoFactorGoogleAuthService>();
			
			// Generate a key and apply to pending:
			var key = twoFA.GenerateKey();
			
			user.TwoFactorSecretPending = key;
			user = await _service.Update(context, user);
			
			var imageBytes = twoFA.GenerateProvisioningImage(key);
			
			return File(new MemoryStream(imageBytes), "image/jpeg");
		}
		
		
    }

}
