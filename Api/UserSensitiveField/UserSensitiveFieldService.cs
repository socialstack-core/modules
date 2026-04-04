using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.PasswordAuth;
using Api.PasswordResetRequests;
using Api.Startup;
using Api.Users;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.UserSensitiveField
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class UserSensitiveFieldService : AutoService
    {
        private const double hoursUntilNextChange = 0.1;

		/// <summary>
        /// The user service.
        /// </summary>
        public UserService _users;

		/// <summary>
        /// The email service.
        /// </summary>
        public Emails.EmailTemplateService _emails;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserSensitiveFieldService(UserService users, Emails.EmailTemplateService emails)
        {
			_users = users;
            _emails = emails;

            Events.PasswordResetRequest.BeforeSettable.AddEventListener((Context ctx, JsonField<PasswordResetRequest, uint> field) => {

				if (field == null)
				{
					return new ValueTask<JsonField<PasswordResetRequest, uint>>(field);
				}

				if (field.Name == "EmailRecoveryKey")
				{
					// Not settable.
					field = null;
				}

				return new ValueTask<JsonField<PasswordResetRequest, uint>>(field);
			});

            Events.User.BeforeSettable.AddEventListener((Context ctx, JsonField<User, uint> field) => {

				if (field == null)
				{
					return new ValueTask<JsonField<User, uint>>(field);
				}

                if (field.Name == "LastSensitiveFieldChangeUtc")
                {
                    // Not settable.
                    field = null;
                }
				else if (field.Name == "SensitiveFieldPassword")
				{
                    field.Hide = true;
                    field.Writeable = true;
				}

				return new ValueTask<JsonField<User, uint>>(field);
			});

			Events.User.BeforeGettable.AddEventListener((Context ctx, JsonField<User, uint> field) => {

				if (field == null)
				{
					return new ValueTask<JsonField<User, uint>>(field);
				}

				if (field.Name == "SensitiveFieldPassword")
				{
					// Not gettable.
					field = null;
				}

				return new ValueTask<JsonField<User, uint>>(field);
			});

			Events.User.BeforeUpdate.AddEventListener(async (Context ctx, User user, User orig) => {
				if (user == null || ctx.Role.CanViewAdmin)
                {
					return user;
                }

                bool sensitiveFieldsChanged = false;

                if (
                    ((!string.IsNullOrEmpty(orig.Email) && user.Email != orig.Email) || 
                    user.PasswordHash != orig.PasswordHash)
                    && !string.IsNullOrEmpty(orig.PasswordHash) // password must exist on the account for *either* of these sensitive triggers to occur.
				){
                    sensitiveFieldsChanged = true;
                }

				if (!sensitiveFieldsChanged)
				{
                    // Not a sensitive field change - Allow it.
                    return user;
                }

                var passwordResetService = Services.Get<PasswordResetRequestService>();

                if (!string.IsNullOrEmpty(user.EmailRecovery))
                {
                    // Change is done via an account recovery email - determine if the request is valid and process it.

                    var passwordResetRequest = await passwordResetService.Where("UserId = ? and EmailRecoveryKey = ?", DataOptions.IgnorePermissions)
                        .Bind(orig.Id)
                        .Bind(user.EmailRecovery)
                        .First(ctx);

                    if (passwordResetRequest != null && passwordResetRequest.IsUsed == false)
                    {
                        // Burn the token:
                        var reqToUpdate = passwordResetService.StartUpdate(ctx, passwordResetRequest, DataOptions.IgnorePermissions);

                        if(reqToUpdate != null) {
                            reqToUpdate.IsUsed = true;
                            await passwordResetService.FinishUpdate(ctx, reqToUpdate, passwordResetRequest, DataOptions.IgnorePermissions);
                        }

                        // Change has been made via account recovery email - Accept the change.
                        return user;
                    }
                    else
                    {
                        throw new PublicException("The password reset request doesn't exist or has expired.", "password_reset_request/invalid");
                    }
                } else if (!string.IsNullOrWhiteSpace(user.PasswordReset) && user.Email == orig.Email && user.PasswordHash != orig.PasswordHash) {
                    var passwordResetRequest = await passwordResetService.Where("Token = ?", DataOptions.IgnorePermissions)
                        .Bind(user.PasswordReset)
                        .First(ctx);

                    if (passwordResetRequest != null && !passwordResetService.HasExpired(passwordResetRequest) && passwordResetRequest.EmailRecoveryKey == null)
                    {
                        // This is a password reset request - Let it through.

                        // Burn the token, just in case the 'login/{token}' route was bypassed
                        // (it does the expiry check before the user update so it should still continue as normal)

                        var reqToUpdate = passwordResetService.StartUpdate(ctx, passwordResetRequest, DataOptions.IgnorePermissions);

                        if(reqToUpdate != null) {
                            reqToUpdate.IsUsed = true;
                            await passwordResetService.FinishUpdate(ctx, reqToUpdate, passwordResetRequest, DataOptions.IgnorePermissions);
                        }

                        return user;
                    }
                }

                // Change is done by editing the profile - Ensure the user can make the change and send a recovery email out.

                if (user.LastSensitiveFieldChangeUtc != null)
                {
                    var timeSinceLastChange = (DateTime.UtcNow - user.LastSensitiveFieldChangeUtc.Value);
                    double hoursSinceLastChange = timeSinceLastChange.TotalHours;

                    if (hoursSinceLastChange < hoursUntilNextChange) {
                        var hDiff = hoursUntilNextChange - hoursSinceLastChange;

                        if (hDiff >= 24.0)
                        {
                            int daysLeft = (int)(hDiff / 24.0);
                            daysLeft = (hDiff % 24.0 >= 18.0) ? daysLeft+1 : daysLeft; // Round up daysLeft if it's a lot closer to that amount
                            string dayOrDays = daysLeft == 1 ? "day" : "days";
                            throw new PublicException("You must wait " + daysLeft + " more " + dayOrDays + " to change sensitive fields again", "user_update/changed_recently", 403);
                        }
                        else if (hDiff >= 1.0)
                        {
                            int hoursLeft = (int)(hDiff);
                            string hourOrHours = hoursLeft == 1 ? "hour" : "hours";
                            throw new PublicException("You must wait " + hoursLeft + " more " + hourOrHours + " to change sensitive fields again", "user_update/changed_recently", 403);
                        }
                        else if (hDiff >= 1.0 / 60.0)
                        {
                            int minutesLeft = (int)(hDiff * 60.0);
                            string minOrMins = minutesLeft == 1 ? "minute" : "minutes";
                            throw new PublicException("You must wait " + minutesLeft + " more " + minOrMins + " to change sensitive fields again", "user_update/changed_recently", 403);
                        }
                        else
                        {
                            int secondsLeft = (int)(hDiff * 60.0 * 60.0);
                            string secOrSecs = secondsLeft == 1 ? "second" : "seconds";
                            throw new PublicException("You must wait " + secondsLeft + " more " + secOrSecs +" to change sensitive fields again", "user_update/changed_recently", 403);
                        }
                    }
                }

                if (string.IsNullOrEmpty(user.SensitiveFieldPassword))
                {
                    throw new PublicException("The current password must be provided to change sensitive fields.", "user_update/no_password", 403);
                }

                if (!PasswordStorage.VerifyPassword(user.SensitiveFieldPassword, orig.PasswordHash))
                {
                    throw new PublicException("Incorrect password entered.", "user_update/wrong_password", 403);
                }

                string emailRecoveryKey = RandomToken.Generate(10);

                var newPasswordResetRequest = await passwordResetService.Create(ctx, new PasswordResetRequest()
                {
                    UserId = user.Id,
                    EmailRecoveryKey = emailRecoveryKey
                });

                if (newPasswordResetRequest == null)
                {
                    throw new PublicException("Something went wrong.", "password_reset_request/not_created");
                }

                user.LastSensitiveFieldChangeUtc = DateTime.UtcNow;

                await SendFieldChangeMessage(ctx, orig, newPasswordResetRequest.Token, emailRecoveryKey);

                return user;
			}, 15);
		}

		/// <summary>
        /// Sends a email to the original email address of the user to inform them
        /// about sensitive field changes and provide a recovery link.
        /// </summary>
        public async ValueTask SendFieldChangeMessage(Context context, User orig, string resetToken, string emailRecoveryKey)
        {
            var recipientUser = orig;

            if (recipientUser == null)
            {
                throw new PublicException("User not found.", "user/not-found");
            }

			var sensitiveFieldData = new SensitiveFieldCustomPayloadData()
			{
				Token = resetToken,
				EmailRecoveryKey = emailRecoveryKey
			};

			var recipient = new Emails.Recipient(recipientUser, sensitiveFieldData);

            _emails.Send(
                recipient,
                "recover_account"
            );
        }

	
	}

	/// <summary>
    /// Custom data in sensitive field email
    /// </summary>
    public partial class SensitiveFieldCustomPayloadData
    {

        /// <summary>
        /// The password reset request token.
        /// </summary>
        public string Token;

        /// <summary>
        /// Token to use to recover the account.
        /// </summary>
        public string EmailRecoveryKey;
    }
}
