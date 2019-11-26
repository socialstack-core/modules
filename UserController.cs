using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Emails;
using Api.Contexts;
using Api.Uploads;
using Api.PasswordReset;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Users
{
    /// <summary>
    /// Handles user account endpoints.
    /// </summary>

    [Route("v1/user")]
	[ApiController]
	public partial class UserController : ControllerBase
    {
        private IDatabaseService _database;
        private IUserService _users;
        private IEmailService _email;
		private IContextService _contexts;
		private IUploadService _uploads;
		private IPasswordResetService _passwordReset;
		private readonly Query<User> listQuery;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public UserController(
            IDatabaseService database, IUserService users, IContextService contexts, 
            IEmailService email, IUploadService uploads, IPasswordResetService passwordReset
		)
        {
            _database = database;
            _users = users;
            _email = email;
			_contexts = contexts;
            _uploads = uploads;
			_passwordReset = passwordReset;

			listQuery = Query.List<User>();

		}

		/// <summary>
		/// GET /v1/user/list
		/// Lists all users visible to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<User>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/user/list
		/// Lists filtered users visible to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<User>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<User>(filters);

			filter = await Events.UserList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _users.List(context, filter);
			return new Set<User>() { Results = results };
		}

		/// <summary>
		/// Gets the currently logged in user, or null if there isn't one.
		/// </summary>
		/// <returns></returns>
		[HttpGet("self")]
		public async Task<User> Self()
		{
			var context = Request.GetContext();

			if (context == null || context.UserId == 0)
			{
				return null;
			}

			var user = await context.GetUser();
			return user;
		}

		/// <summary>
		/// Upload a file for this user.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="body"></param>
		/// <returns></returns>
		[HttpPost("{id}/upload")]
        public async Task<object> Upload([FromRoute] int id, [FromForm] UserImageUpload body)
        {
			var context = Request.GetContext();

			body = await Events.UserOnUpload.Dispatch(context, body, Response);

			// Upload the file:
			var upload = await _uploads.Create(
				context,
                typeof(User),
                id,
                body.File,
                new int[] {
                    400
                }
            );

			if (upload == null)
			{
				// It failed.
				return null;
			}

			var uploadRef = upload.Ref;
			var uploadType = body.Type.ToLower().Trim();
			
			// Update on the user row:
			await _users.UpdateFile(context, id, uploadType, uploadRef);
			
			return new
            {
                id = upload.Id,
				uploadRef = upload.Ref,
                publicUrl = upload.GetPublicUrl("original"),
                isImage = upload.IsImage
            };
        }

		/// <summary>
		/// GET /v1/user/2/profile
		/// Returns the profile user data for a single user.
		/// </summary>
		[HttpGet("{id}/profile")]
		public async Task<UserProfile> LoadProfile([FromRoute] int id)
		{
			var context = Request.GetContext();

			var result = await _users.GetProfile(context, id);

			if (result == null)
			{
				Response.StatusCode = 404;
				return null;
			}
			
			return result;
		}

		/// <summary>
		/// GET /v1/user/2/
		/// Returns the user data for a single user.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<User> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _users.Get(context, id);
			return await Events.UserLoad.Dispatch(context, result, Response);
        }

		/// <summary>
		/// DELETE /v1/user/2/
		/// Deletes a user user
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _users.Get(context, id);
			result = await Events.UserDelete.Dispatch(context, result, Response);

			if (result == null || !await _users.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// Logs out this user account.
		/// </summary>
		/// <returns></returns>
        [HttpGet("logout")]
        public Success Logout() {

			// var context = Request.GetContext();

			var expiry = DateTime.MinValue;

            Response.Cookies.Append(
                _contexts.CookieName,
                "",
                new Microsoft.AspNetCore.Http.CookieOptions()
                {
                    Path = "/",
                    Expires = expiry
                }
            );

            return new Success();
        }

		/// <summary>
		/// POST /v1/user/login/
		/// Attempts to login.
		/// </summary>
		[HttpPost("login")]
		public async Task<LoginResult> Login([FromBody] UserLogin body)
		{
			var context = Request.GetContext();

			var result = await _users.Authenticate(context, body);

			if (result != null && result.Token != null)
			{
				Response.Cookies.Append(
					result.CookieName,
					result.Token,
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Expires = result.Expiry
					}
				);
			}

			#warning todo
			// Locale cookie too (Added by translation system).

			return result;
        }

		/// <summary>
		/// POST /v1/user/
		/// Creates a new user. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<User> Create([FromBody] UserAutoForm form)
		{
			var context = Request.GetContext();

			int role = Roles.Member.Id;

			var user = new User()
			{
				Role = role,
				JoinedUtc = DateTime.UtcNow
			};

			if (!ModelState.Setup(form, user))
			{
				return null;
			}

			form = await Events.UserCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			await _users.Create(context, form.Result);

			return user;
		}

		/// <summary>
		/// POST /v1/user/{id}/
		/// Updates a user account
		/// </summary>
		[HttpPost("{id}")]
		public async Task<User> Update([FromRoute] int id, [FromBody] UserAutoForm form)
		{
			var context = Request.GetContext();
			
			// Get the user:
			var user = await _users.Get(context, id);

			if (!ModelState.Setup(form, user))
			{
				return null;
			}

			form = await Events.UserUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			/*
            // Can edit internal details and internal details are being submitted:
            if (body.Role != null)
            {
				user.Role = body.Role.Value;

				// Bump login ref and broadcast a revoke for prior values:
				_contexts.Revoke(user.Id, user.LoginRevokeCount);

				user.LoginRevokeCount++;
            }

			if (body.Password != null)
            {
                // Setting a new password:
                var newPasswordHash = PasswordStorage.CreateHash(body.Password);
				user.PasswordHash = newPasswordHash;
			}
			*/

			// Update now:
			await _users.Update(context, form.Result);

			return user;
        }
		
    }

}
