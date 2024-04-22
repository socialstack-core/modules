using Api.Contexts;
using Api.Eventing;
using Api.PasswordAuth;
using Api.Permissions;
using Api.Startup;
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
        public async ValueTask<object> CheckTokenExists(string token)
        {
            var context = await Request.GetContext();

            if (context == null)
            {
                return null;
            }

            var svc = (_service as PasswordResetRequestService);

            var request = await svc.Get(context, token);

            if (request == null)
            {
                Response.StatusCode = 404;
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
                Response.StatusCode = 400;
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
        public async ValueTask LoginWithToken(string token, [FromBody] NewPassword newPassword)
        {
            var svc = (_service as PasswordResetRequestService);

            var context = await Request.GetContext();

            if (context == null || newPassword == null || string.IsNullOrWhiteSpace(newPassword.Password))
            {
                if (Response.StatusCode == 200)
                {
                    Response.StatusCode = 404;
                }
                return;
            }

            var request = await svc.Get(context, token);

            if (request == null)
            {
                Response.StatusCode = 404;
                return;
            }

            // Has it expired?
            if (svc.HasExpired(request))
            {
                Response.StatusCode = 400;
                return;
            }

            // Get the target user account:
            var targetUser = await _users.Get(context, request.UserId, DataOptions.IgnorePermissions);

            if (targetUser == null)
            {
                // User doesn't exist.
                Response.StatusCode = 403;
                return;
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
                    Response.StatusCode = 403;
                    return;
                }
            }
            
            if (!updatedPassword)
            {
                var userToUpdate = await _users.StartUpdate(context, targetUser, DataOptions.IgnorePermissions);

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
                    Response.StatusCode = 403;
                    return;
                }
            }
            
            // Burn the token:
            var reqToUpdate = await _service.StartUpdate(context, request, DataOptions.IgnorePermissions);

            if (reqToUpdate != null)
            {
                reqToUpdate.IsUsed = true;
                await _service.FinishUpdate(context, reqToUpdate, request, DataOptions.IgnorePermissions);
            }

            // Set user:
            context.User = targetUser;

            await Events.PasswordResetRequestAfterSuccess.Dispatch(context, request);

            // Output context:
            await OutputContext(context);
        }

        /// <summary>
        /// Admin link generation.
        /// </summary>
        [HttpGet("{id}/generate")]
        public async ValueTask<object> Generate(uint id)
        {
            var context = await Request.GetContext();

            if (context == null)
            {
                return null;
            }

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

            return new
            {
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