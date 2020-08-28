using Api.Contexts;
using Api.Eventing;
using Api.PasswordAuth;
using Api.Permissions;
using Api.Users;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Api.PasswordResetRequests
{
    /// <summary>Handles passwordResetRequest endpoints.</summary>
    [Route("v1/passwordResetRequest")]
	public partial class PasswordResetRequestController : AutoController<PasswordResetRequest>
    {
		private IUserService _users;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		/// <param name="users"></param>
		public PasswordResetRequestController(IUserService users)
		{
			_users = users;
		}

		/// <summary>
		/// Check if token exists and has not expired yet.
		/// </summary>
		[HttpGet("token/{token}")]
		public async Task<object> CheckTokenExists(string token)
		{
			var context = Request.GetContext();
			
			if (context == null)
			{
				return null;
			}
			
			var svc = (_service as IPasswordResetRequestService);
			
			var request = await svc.Get(context, token);
			
			if(request == null)
			{
				Response.StatusCode = 404;
				return null;
			}
			
			// Has it expired?
			if(svc.HasExpired(request))
			{
				Response.StatusCode = 400;
				return null;
			}
			
			return new {
				token
			};
		}
		
		/// <summary>
		/// Attempts to login with a submitted new password.
		/// </summary>
		[HttpPost("login/{token}")]
		public async Task<object> LoginWithToken(string token, [FromBody] NewPassword newPassword)
		{
			var context = Request.GetContext();
			
			if (context == null || newPassword == null || string.IsNullOrWhiteSpace(newPassword.Password))
			{
				return null;
			}
			
			var svc = (_service as IPasswordResetRequestService);
			
			var request = await svc.Get(context, token);
			
			if(request == null)
			{
				Response.StatusCode = 404;
				return null;
			}
			
			// Has it expired?
			if(svc.HasExpired(request))
			{
				Response.StatusCode = 400;
				return null;
			}
			
			// Get the target user account:
            var targetUser = await _users.Get(context, request.UserId);
			
            if (targetUser == null)
            {
				// User doesn't exist.
                Response.StatusCode = 403;
                return null;
            }

			// Set the password on the user account:
			targetUser.PasswordHash = PasswordStorage.CreateHash(newPassword.Password.Trim());

			targetUser = await _users.Update(context, targetUser);

			if (targetUser == null)
			{
				// API forced a halt:
				Response.StatusCode = 403;
				return null;
			}

			// Burn the token:
			request.IsUsed = true;
			await _service.Update(context, request);

			// Set user:
			context.SetUser(targetUser);
			context.RoleId = targetUser.Role;
			
			await Events.PasswordResetRequestAfterSuccess.Dispatch(context, request);
			
            // Regenerate the contextual token:
            context.SendToken(Response);
			
            return await context.GetPublicContext();
		}
		
		/// <summary>
		/// Admin link generation.
		/// </summary>
		[HttpGet("{id}/generate")]
		public async Task<object> Generate(int id)
		{
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}
			
			// must be admin/ super admin. Nobody else can do this for very clear security reasons.
			if(context.Role != Roles.SuperAdmin && context.Role != Roles.Admin)
			{
				return null;
			}
			
			// Create token:
			var prr = await _service.Create(context, new PasswordResetRequest(){
				UserId = id
			});
			
			if(prr == null){
				return null;
			}
			
			return new {
				token = prr.Token,
				url = "/password/reset/" + prr.Token
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
}