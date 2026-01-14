using Api.Contexts;
using Api.Eventing;
using Api.PasswordAuth;
using Api.Permissions;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Api.PasswordResetRequests
{
	/// <summary>Handles passwordResetRequest endpoints.</summary>
	[Route("v1/passwordResetRequest")]
	public partial class PasswordResetRequestController : AutoController<PasswordResetRequest>
	{
		private UserService _users;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		/// <param name="users"></param>
		public PasswordResetRequestController(UserService users)
		{
			_users = users;
		}

		/// <summary>
		/// Check if token exists and has not expired yet.
		/// </summary>
		[HttpGet("token/{token}")]
		public async ValueTask<object> CheckTokenExists(Context context, [FromRoute] string token)
		{
			var svc = (_service as PasswordResetRequestService);

			var request = await svc.Get(context, token);

			if (request == null)
			{
				return null;
			}

			// Has it been used?
			if (svc.IsUsed(request))
			{
				throw new PublicException("Token already used", "already_used");
			}

			// Has it expired?
			if (svc.HasExpired(request))
			{
				return null;
			}

			return new
			{
				token
			};
		}

		/// <summary>
		/// Attempts to login with a submitted new password.
		/// </summary>
		[HttpPost("login/{token}")]
		public async ValueTask<Context> LoginWithToken(HttpContext httpContext, Context context, [FromRoute] string token, [FromBody] NewPassword newPassword)
		{
			var svc = (_service as PasswordResetRequestService);

			if (context == null || newPassword == null || string.IsNullOrWhiteSpace(newPassword.Password))
			{
				return null;
			}

			var request = await svc.Get(context, token);

			if (request == null)
			{
				return null;
			}

			// Has it expired?
			if (svc.HasExpired(request))
			{
				return null;
			}

			// Get the target user account:
			var targetUser = await _users.Get(context, request.UserId, DataOptions.IgnorePermissions);

			if (targetUser == null)
			{
				// User doesn't exist.
				return null;
			}

			// Set the password on the user account:
			var authService = Services.Get<PasswordAuthService>();

			await authService.EnforcePolicy(newPassword.Password);

			// allow other services to handle the password storage/update
			var updatedPassword = false;
			if (Events.UserOnPasswordUpdate.HasListeners())
			{
				updatedPassword = await Events.UserOnPasswordUpdate.Dispatch(context, updatedPassword, targetUser, request, newPassword);

				if (targetUser == null)
				{
					// API forced a halt:
					return null;
				}
			}
			
			if (!updatedPassword)
			{
				var userToUpdate = _users.StartUpdate(context, targetUser, DataOptions.IgnorePermissions);

				if (userToUpdate != null)
				{
					userToUpdate.PasswordReset = token;
					userToUpdate.PasswordHash = PasswordStorage.CreateHash(newPassword.Password);

					// This also effectively validates the user's email address, so if they were still a guest, elevate them to member.
					if (userToUpdate.Role == Roles.Guest.Id)
					{
						userToUpdate.Role = Roles.Member.Id;
					}

					targetUser = await _users.FinishUpdate(context, userToUpdate, targetUser, DataOptions.IgnorePermissions);
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
			}
			
			// Burn the token:
			var reqToUpdate = _service.StartUpdate(context, request, DataOptions.IgnorePermissions);

			if (reqToUpdate != null)
			{
				reqToUpdate.IsUsed = true;
				await _service.FinishUpdate(context, reqToUpdate, request, DataOptions.IgnorePermissions);
			}

			// Set user:
			context.User = targetUser;
			
			await Events.Context.OnLoad.Dispatch(context, httpContext.Request);

			await Events.PasswordResetRequestAfterSuccess.Dispatch(context, request);

			return context;
		}

		/// <summary>
		/// Admin link generation.
		/// </summary>
		[HttpGet("{id}/generate")]
		public async ValueTask<ResetToken?> Generate(Context context, [FromRoute] uint id)
		{
			// must be admin/ super admin. Nobody else can do this for very clear security reasons.
			if (context.Role != Roles.Developer && context.Role != Roles.Admin)
			{
				return null;
			}

			// Create token:
			var prr = await _service.Create(context, new PasswordResetRequest()
			{
				UserId = id
			});

			if (prr == null)
			{
				return null;
			}

			return new ResetToken()
			{
				Token = prr.Token,
				Url = "/password/reset/" + prr.Token
			};
		}

	}

	/// <summary>
	/// Used when setting a new password.
	/// </summary>
	public class NewPassword
	{
		/// <summary>
		/// The new password.
		/// </summary>
		public string Password;
	}

	/// <summary>
	/// A password reset token.
	/// </summary>
	public struct ResetToken
	{
		/// <summary>
		/// The token itself.
		/// </summary>
		public string Token;

		/// <summary>
		/// A url containing the token.
		/// </summary>
		public string Url;
	}
}