using Api.Contexts;
using Api.Eventing;
using Api.PasswordAuth;
using Api.PasswordResetRequests;
using Api.Permissions;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Api.UserSensitiveField
{
    /// <summary>Handles userSensitiveField endpoints.</summary>
    [Route("v1/userSensitiveField")]
    public partial class UserSensitiveFieldController : AutoController
    {
		private UserService _users;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		/// <param name="users"></param>
		public UserSensitiveFieldController(UserService users)
		{
			_users = users;
		}
		
		/// <summary>
		/// Attempts to login with a submitted new email and password.
		/// </summary>
		[HttpPost("login/{token}")]
		public async ValueTask<Context> LoginWithToken(Context context, [FromRoute] string token, [FromBody] NewDetails newDetails)
		{
			var svc = Services.Get<PasswordResetRequestService>();
			
			if (
				context == null ||
				newDetails == null ||
				string.IsNullOrWhiteSpace(newDetails.Email) ||
				string.IsNullOrWhiteSpace(newDetails.Password) ||
				string.IsNullOrWhiteSpace(newDetails.EmailRecovery)
			) {
				return null;
			}
				
			var request = await svc.Get(context, token);
				
			if(request == null)
			{
				return null;
			}
				
			// Has it expired?
			if(svc.HasExpired(request))
			{
				throw new PublicException("This request has expired", "request/expired");
			}
	
			// Get the target user account:
			var targetUser = await _users.Get(context, request.UserId, DataOptions.IgnorePermissions);
				
			if (targetUser == null)
			{
				// User doesn't exist.
				return null;
			}

			targetUser.EmailRecovery = newDetails.EmailRecovery;

			// Set the password on the user account:
			var authService = Services.Get<PasswordAuthService>();
				
			await authService.EnforcePolicy(newDetails.Password);

			var userToUpdate = _users.StartUpdate(context, targetUser, DataOptions.IgnorePermissions);

			if (userToUpdate != null)
			{
				userToUpdate.Email = newDetails.Email;
				userToUpdate.PasswordHash = PasswordStorage.CreateHash(newDetails.Password);

				// This also effectively validates the user's email address, so if they were still a guest, elevate them to member.
				if (userToUpdate.Role == Roles.Guest.Id)
				{
					userToUpdate.Role = Roles.Member.Id;
				}

				targetUser = await _users.FinishUpdate(new Context(context.LocaleId, context.User, 1), userToUpdate, targetUser, DataOptions.IgnorePermissions);
			}
			else
			{
				targetUser = null;
			}

			if (targetUser == null)
			{
				// API forced a halt:
				return null;
			}

			// Burn the token:
			var reqToUpdate = svc.StartUpdate(context, request, DataOptions.IgnorePermissions);

			if(reqToUpdate != null){
				reqToUpdate.IsUsed = true;
				await svc.FinishUpdate(context, reqToUpdate, request, DataOptions.IgnorePermissions);
			}

			// Set user:
			context.User = targetUser;

			await Events.PasswordResetRequestAfterSuccess.Dispatch(context, request);

			// Output context:
			return context;
		}
    }

	/// <summary>
	/// Used when setting a new email and password.
	/// </summary>
	public class NewDetails
	{
		/// <summary>
		/// The new email.
		/// </summary>
		public string Email;

		/// <summary>
		/// The new password.
		/// </summary>
		public string Password;

		/// <summary>
		/// The email recovery key.
		/// </summary>
		public string EmailRecovery;
	}
}
