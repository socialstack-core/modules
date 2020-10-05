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
		
		private readonly Query<PasswordResetRequest> selectByTokenQuery;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PasswordResetRequestService(EmailTemplateService emails, UserService users) : base(Events.PasswordResetRequest)
        {
			selectByTokenQuery = Query.Select<PasswordResetRequest>();
			selectByTokenQuery.Where().EqualsArg("Token", 0);
			
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
		/// Gets a reset request by the given token.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<PasswordResetRequest> Get(Context context, string token)
        {
			var item = await _database.Select(context, selectByTokenQuery, token);
			context.NestedTypes |= NestableAddMask;
			item = await EventGroup.AfterLoad.Dispatch(context, item);
			context.NestedTypes &= NestableRemoveMask;
			return item;
        }

	}
    
}
