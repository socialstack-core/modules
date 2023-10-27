using System;
using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Emails;
using Api.Users;
using Api.Pages;
using Api.CanvasRenderer;

namespace Api.PasswordResetRequests
{
	/// <summary>
	/// Handles passwordResetRequests.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PasswordResetRequestService : AutoService<PasswordResetRequest>
    {
		/// <summary>
		/// Request expiry time, in hours.
		/// </summary>
		public const int DefaultExpiryTime = 48;
		
		/// <summary>
		/// The request expiry time, in hours.
		/// </summary>
		public int ExpiryTime = DefaultExpiryTime;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PasswordResetRequestService(EmailTemplateService emails, UserService users) : base(Events.PasswordResetRequest)
        {
			
			Events.Page.BeforeAdminPageInstall.AddEventListener((Context context, Pages.Page page, CanvasRenderer.CanvasNode canvas, Type contentType, AdminPageType pageType) =>
			{
				if (contentType == typeof(User) && pageType == AdminPageType.Single)
				{
					// Installing user admin page for a particular user.
					// Add the reset box into the user admin page (as a child of the autoform):
					var tile = new CanvasNode("Admin/Tile");
					tile.AppendChild(new CanvasNode("Admin/PasswordResetButton").With("userId", new {
						name = "user.id",
						type = "urlToken"
					}));
					
					canvas.AppendChild(
						tile
					);
				}

				return new ValueTask<Pages.Page>(page);
			});

			Events.PasswordResetRequest.BeforeCreate.AddEventListener(async (Context context, PasswordResetRequest reset) => {
				
				if(reset == null){
					return null;
				}
				
				// Generate a token, which will be hidden from the user:
				reset.Token = RandomToken.Generate(20);
				reset.CreatedUtc = DateTime.UtcNow;
				
				if(string.IsNullOrWhiteSpace(reset.Email))
				{
					if(reset.UserId == 0){
						return null;
					}
					
					// Admins can provide a user ID.
					// In this situation, an email isn't sent out.
				}
				else
				{
					// Get the user:
					var user = await users.GetByEmail(context, reset.Email.Trim());
					
					if(user == null){
						// Quietly stop here.
						// We let the creation go through so this doesn't leak information about which emails are registered.
						return reset;
					}
					
					reset.UserId = user.Id;
				}
				
				return reset;
			}, 5);
			
			Events.PasswordResetRequest.AfterCreate.AddEventListener(async (Context context, PasswordResetRequest reset) => {
				
				// Send the email (we'll specifically wait for this one):
				if(reset == null || reset.UserId == 0 || reset.Email == null){
					return reset;
				}

				var resetUser = await users.Get(context, reset.UserId, DataOptions.IgnorePermissions);
				var recipient = new Recipient(resetUser);

				recipient.CustomData = new PasswordResetCustomEmailData()
				{
					Reset = reset,
					Token = reset.Token
				};

				var recipients = new List<Recipient>();
				recipients.Add(recipient);
				
				await emails.SendAsync(
					recipients,
					"forgot_password"
				);
				
				return reset;
				
			}, 100);
		}
		
		/// <summary>
		/// True if given req has expired.
		/// </summary>
		public bool HasExpired(PasswordResetRequest req)
		{
			// Either doesn't exist, or its created time + expiry is in the past:
			return (req == null || req.IsUsed || req.CreatedUtc.AddHours(ExpiryTime) < DateTime.UtcNow);
		}

		/// <summary>
		/// True if given req has been used already.
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		public bool IsUsed(PasswordResetRequest req)
        {
			return (req == null || req.IsUsed);
        }
		
		/// <summary>
		/// Gets a reset request by the given token. This overload is always permitted (be careful!).
		/// </summary>
		/// <param name="context"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<PasswordResetRequest> Get(Context context, string token)
        {
			return await Where("Token=?", DataOptions.IgnorePermissions).Bind(token).Last(context);
        }

	}
    
}
