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
using Api.Startup;

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
		public PasswordResetRequestService(EmailTemplateService emails, UserService users, PageService pages) : base(Events.PasswordResetRequest)
        {
			
			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
			{
				if (builder.ContentType == typeof(User) && builder.PageType == CommonPageType.AdminEdit)
				{
					var buttons = new CanvasNode();
					buttons.AppendChild(new CanvasNode("Admin/ImpersonateButton")
							.WithPrimaryLink("content"));
					buttons.AppendChild(new CanvasNode("Admin/PasswordResetButton")
							.WithPrimaryLink("content"));

					builder.AddAdminTab(new AdminTab("Access management", "access")
					{
						Content = buttons
					});
				}

				return new ValueTask<PageBuilder>(builder);
			});

			pages.Install(
				new PageBuilder()
				{
					Url = "/password/reset/${token}",
					Key = "password_reset",
					Title = "Reset your password",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/PasswordReset")
								.WithLink("token", "url.token", false)
						);
					}
				},
				new PageBuilder()
				{
					Url = "/forgot",
					Key = "password_forgot",
					Title = "Account recovery",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/User/ForgotPassword")
						);
					}
				}
			);

			Events.PasswordResetRequest.BeforeCreate.AddEventListener(async (Context context, PasswordResetRequest reset) => {
				
				if(reset == null){
					return null;
				}
				
				// Generate a token, which will be hidden from the user:
				reset.Token = RandomToken.Generate(20);
				reset.CreatedUtc = DateTime.UtcNow;
				
				if(string.IsNullOrWhiteSpace(reset.Email))
				{
					if(reset.UserId == 0)
					{
						throw new PublicException("No user ID or email was provided - unable to generate a reset request.", "reset/unknown");
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
				
				await emails.SendAsync(
					new Recipient(resetUser, reset),
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
