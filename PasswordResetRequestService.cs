using System;
using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Emails;
using Api.Users;

namespace Api.PasswordResetRequests
{
	/// <summary>
	/// Handles passwordResetRequests.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PasswordResetRequestService : AutoService<PasswordResetRequest>, IPasswordResetRequestService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PasswordResetRequestService(IEmailTemplateService emails, IUserService users) : base(Events.PasswordResetRequest)
        {
			Events.PasswordResetRequest.BeforeCreate.AddEventListener(async (Context context, PasswordResetRequest reset) => {
				
				if(string.IsNullOrWhiteSpace(reset.Email)){
					return null;
				}
				
				reset.CreatedUtc = DateTime.UtcNow;
				
				// Get the user:
				var user = await users.GetByEmail(context, reset.Email.Trim());
				
				if(user == null){
					// Quietly stop here.
					// We let the creation go through so this doesn't leak information about which emails are registered.
					return reset;
				}
				
				// Generate a token, which will be hidden from the user:
				reset.Token = RandomToken.Generate(20);
				reset.UserId = user.Id;
				
				return reset;
			}, 5);
			
			Events.PasswordResetRequest.AfterCreate.AddEventListener(async (Context context, PasswordResetRequest reset) => {
				
				// Send the email (we'll specifically wait for this one):
				if(reset.UserId == 0){
					return reset;
				}
				
				var recipient = new Recipient(reset.UserId);
				
				recipient["reset"] = reset;
				recipient["token"] = reset.Token;
				
				var recipients = new List<Recipient>();
				recipients.Add(recipient);
				
				await emails.SendAsync(
					recipients,
					"forgot_password"
				);
				
				return reset;
				
			}, 100);
		}
	}
    
}
