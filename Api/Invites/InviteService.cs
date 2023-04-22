using System;
using System.Threading.Tasks;
using Api.Contexts;
using System.Collections.Generic;
using Api.Eventing;
using Api.Startup;
using Api.Users;
using Api.PasswordResetRequests;


namespace Api.Invites
{

	/// <summary>
	/// Manages user roles.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class InviteService : AutoService<Invite>
    {
		/// <summary>
		/// Custom function to select the user to use from the user locator (email address, phone number etc).
		/// </summary>
		public Func<Context, string, ValueTask<User>> OnSelectUser;

		/// <summary>
		/// Custom function to determine if a user is registered
		/// </summary>
		public Func<User, bool> IsRegistered;

		/// <summary>
		/// The user service.
		/// </summary>
		public UserService _users;

		/// <summary>
		/// The email service.
		/// </summary>
		public Emails.EmailTemplateService _emails;
		
		/// <summary>
		/// The SMS service.
		/// </summary>
		public SmsMessages.SmsTemplateService _sms;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public InviteService(UserService users, Emails.EmailTemplateService emails, SmsMessages.SmsTemplateService sms) : base(Events.Invite)
		{
			_users = users;
			_emails = emails;
			_sms = sms;

			var config = GetConfig<InviteServiceConfig>();

			Events.Invite.BeforeUpdate.AddEventListener((Context context, Invite updated, Invite orig) => {

				// Block changing the invite type. This prevents the invited user from potentially elevating themselves to a company team
				// member if they were invited as a client and switch the type to the "team member" type instead.
				if (updated.InviteType != orig.InviteType && context.UserId != orig.UserId)
				{
					throw new PublicException("Invite type can only be changed by the invite creator.", "invite_type_changed");
				}

				return new ValueTask<Invite>(updated);
			});

			Events.Invite.BeforeCreate.AddEventListener(async (Context context, Invite invite) => {
				
				if(invite == null)
				{
					return invite;
				}
				
				// Generate a token, which will be hidden from the user except in the email/ whatever sends the invite:
				invite.Token = RandomToken.Generate(20);

				if (!string.IsNullOrEmpty(invite.UserLocator))
				{
					// First, get the user. They may already exist.
					User user = null;
					var locator = invite.UserLocator.Trim();

					var isEmail = (locator.IndexOf('@') != -1);

					if (OnSelectUser != null)
					{
						user = await OnSelectUser(context, locator);
					}
					else if (isEmail)
					{
						user = await users.Get(context, locator);
					}
					else
					{
						// Phone number if this feature is enabled.
						if (config.CanSendViaSms)
						{
							user = await users.Where("ContactNumber=?").Bind(locator).First(context);
						}
					}
					
					if (!config.CanSendIfAlreadyExists && user != null)
					{
						// Throw an exception if the user has completed registration
						if ((IsRegistered != null && IsRegistered(user)) || (IsRegistered == null && user.PasswordHash != null))
                        {
							throw new PublicException("A user with that information has an account already.", "user_exists");
						}
					}

					var fullName = (invite.FirstName != null && invite.LastName != null) ? invite.FirstName + " " + invite.LastName : null;

					if(user == null)
					{
						// Create the user now, as a member:
						user = await users.Create(context,
							new User() {
								Email = isEmail ? locator : null,
								FirstName = invite.FirstName,
								LastName = invite.LastName,
								FullName = fullName,
								ContactNumber = !isEmail ? locator : null
							}, DataOptions.IgnorePermissions);
					}
					
					// Set user ID:
					invite.InvitedUserId = user.Id;
				}

				return invite;
			});
			
			Events.Invite.AfterCreate.AddEventListener(async (Context context, Invite invite) => {
				
				// Send the email/ SMS (we'll specifically wait for this one):
				if(invite == null || !invite.InvitedUserId.HasValue || invite.InvitedUserId == 0 || string.IsNullOrEmpty(invite.UserLocator)){
					return invite;
				}

				var recipientUser = await _users.Get(context, invite.InvitedUserId.Value, DataOptions.IgnorePermissions);

				await SendInvite(context, recipientUser, invite);

				return invite;
				
			}, 100);
		}
		
		/// <summary>
		/// Redeem an invite by its token.
		/// </summary>
		public async ValueTask<Invite> Redeem(Context context, InviteData inviteData)
		{
			// Get the invite:
			var result = await Where("Token=?", DataOptions.IgnorePermissions).Bind(inviteData.Token).First(context);
			
			if(result == null)
			{
				throw new PublicException("This invite has expired", "invite_expired");
			}
			
			// Check expiry. They last 48 hours by default:
			if(DateTime.UtcNow > result.ExpiryUtc)
			{
				await Delete(context, result.Id, DataOptions.IgnorePermissions);
				throw new PublicException("This invite has expired", "invite_expired");
			}

			// Valid invite. If it's a user invite, it is set into the context:
			if (result.InvitedUserId.HasValue && result.InvitedUserId != 0)
			{
				var user = await _users.Get(context, result.InvitedUserId.Value, DataOptions.IgnorePermissions);

				if (user == null)
				{
					// The user account was deleted, which translates to an expired invite:
					throw new PublicException("This invite has expired", "invite_expired");
				}
				
				if(user.Role == 3){
					user = await _users.Update(context, user, (Context c, User u, User orig) => {
						// Elevate from guest to member:
						u.Role = 4;
					}, DataOptions.IgnorePermissions);
				}
				
				var loginResult = new LoginResult();
				loginResult.CookieName = Context.CookieName;
				loginResult.User = user;
				context.User = user;
				loginResult.LoginData = inviteData;

				// Act like a login:
				await Events.UserOnLogin.Dispatch(context, loginResult);

			}

			return result;
		}

		private async ValueTask SendInvite(Context context, User recipientUser, Invite invite)
		{
			string token = invite.Token;
			uint inviteType = invite.InviteType;

			// Potential support for multiple invite types.
			var templateName = inviteType == 0 ? "invited_join" : "invited_join_" + inviteType;

			if (string.IsNullOrEmpty(recipientUser.Email))
			{
				if (string.IsNullOrEmpty(recipientUser.ContactNumber))
				{
					// nope!
					return;
				}

				// Send an SMS.
				var recipient = new SmsMessages.Recipient(recipientUser);

				var inviteData = new InviteCustomPayloadData()
				{
					Token = token,
					Invite = invite
				};

				inviteData = await Events.Invite.BeforeSend.Dispatch(context, inviteData, invite);
				recipient.CustomData = inviteData;

				var recipients = new List<SmsMessages.Recipient>();
				recipients.Add(recipient);

				await _sms.SendAsync(
					recipients,
					templateName
				);
			}
			else
			{
				// If an email address exists, use that as priority.
				var recipient = new Emails.Recipient(recipientUser);

				var inviteData = new InviteCustomPayloadData()
				{
					Token = token,
					Invite = invite
				};

				inviteData = await Events.Invite.BeforeSend.Dispatch(context, inviteData, invite);
				recipient.CustomData = inviteData;

				var recipients = new List<Emails.Recipient>();
				recipients.Add(recipient);

				await _emails.SendAsync(
					recipients,
					templateName
				);
			}
		}

	}

	/// <summary>
	/// Custom data in invite emails/ SMS
	/// </summary>
	public partial class InviteCustomPayloadData
	{

		/// <summary>
		/// Token to use
		/// </summary>
		public string Token;

		/// <summary>
		/// The complete invite.
		/// </summary>
		public Invite Invite;
	}
}
