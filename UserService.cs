using System;
using System.Threading.Tasks;
using Api.Database;
using Api.Emails;
using Microsoft.AspNetCore.Http;
using Api.Contexts;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using System.Collections;
using System.Reflection;
using Api.Startup;
using Org.BouncyCastle.Security;
using System.Text;
using Api.PasswordAuth;

namespace Api.Users
{

	/// <summary>
	/// Manages user accounts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public class UserService : AutoService<User>
    {
        private EmailTemplateService _emails;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserService() : base(Events.User)
		{
			var config = GetConfig<UserServiceConfig>();
			
			InstallEmails(
				new EmailTemplate(){
					Name = "Verify email address",
					Subject = "Verify your email address",
					Key = "verify_email",
					BodyJson = "{\"module\":\"Email/Default\",\"content\":[{\"module\":\"Email/Centered\",\"data\":{}," +
					"\"content\":\"An account was recently created with us. If this was you, click the following link to proceed:\"},"+
					"{\"module\":\"Email/PrimaryButton\",\"data\":{\"label\":\"Verify my email address\",\"target\":\"/email-verify/${customData.userId}/${customData.token}\"}}]}"
				},
				new EmailTemplate()
                {
					Name = "Password reset",
					Subject = "Password reset",
					Key = "forgot_password",
					BodyJson = "{\"module\":\"Email/Default\",\"content\":[{\"module\":\"Email/Centered\",\"data\":{}," +
					"\"content\":\"A password reset request was recently created with us for this email. If this was you, click the following link to proceed:\"}," +
					"{\"module\":\"Email/PrimaryButton\",\"data\":{\"label\":\"Verify my email address\",\"target\":\"/password/reset/{token}\"}}]}"
				}
			);
			
			Events.User.BeforeSettable.AddEventListener((Context ctx, JsonField<User, uint> field) => {
				
				if (field == null)
				{
					return new ValueTask<JsonField<User, uint>>(field);
				}
				
				if(field.Name == "Role")
				{
					// Only admins can update this field.
					// Will be permission system based in the future
					return new ValueTask<JsonField<User, uint>>((field.ForRole == Roles.Admin || field.ForRole == Roles.Developer) ? field : null);
				}
				else if(field.Name == "JoinedUtc" || field.Name == "PrivateVerify")
				{
					// Not settable
					field = null;
				}

				return new ValueTask<JsonField<User, uint>>(field);
			});

			Events.User.BeforeGettable.AddEventListener((Context ctx, JsonField<User, uint> field) => {

				if (field == null)
				{
					return new ValueTask<JsonField<User, uint>>(field);
				}

				if (field.ForRole == Roles.Admin || field.ForRole == Roles.Developer)
				{
					// This is readable by default:
					field.Readable = true;
				}

				return new ValueTask<JsonField<User, uint>>(field);
			});

			Events.User.BeforeCreate.AddEventListener((Context ctx, User user) => {
				
				if(user == null){
					return new ValueTask<User>(user);
				}
				
				if(user.Role == 0 || (ctx.Role != Roles.Developer && ctx.Role != Roles.Admin))
				{
					// Default role is Member:
					user.Role = config.VerifyEmails ? Roles.Guest.Id : Roles.Member.Id;
				}
				
				// Generate a private verify value:
				user.PrivateVerify = RandomLong();
				
				return new ValueTask<User>(user);
			});

			Events.User.AfterCreate.AddEventListener(async (Context ctx, User user) => {
				
				if(user == null){
					return user;
				}
				
				if(config.VerifyEmails)
				{
					var token = await SendVerificationEmail(ctx, user);

					user.EmailVerifyToken = token;
				}
				
				return user;
			});
			
			Events.User.BeforeCreate.AddEventListener(async (Context ctx, User user) =>
			{
				if (user == null)
				{
					return user;
				}

				// Let's see if the username is ok.
				if (config.UniqueUsernames && !string.IsNullOrEmpty(user.Username))
				{
					// Let's make sure the username is not in use.
					var usersWithUsername = await Where("Username=?", DataOptions.IgnorePermissions).Bind(user.Username).Any(ctx);

					if (usersWithUsername)
					{
						throw new PublicException(config.UniqueUsernameMessage, "username_used");
					}
				}

				if (config.UniqueEmails && !string.IsNullOrEmpty(user.Email))
				{
					// Let's make sure the username is not in use.
					var usersWithEmail = await Where("Email=?", DataOptions.IgnorePermissions).Bind(user.Email).Any(ctx);

					if (usersWithEmail)
					{
						throw new PublicException(config.UniqueEmailMessage, "email_used");
					}
				}

				return user;
			});

			var emailField = GetChangeField("Email");
			var usernameField = GetChangeField("Username");

			Events.User.BeforeUpdate.AddEventListener(async (Context ctx, User user) =>
			{
				if (user == null)
                {
					return user;
                }

				if (config.UniqueUsernames && !string.IsNullOrEmpty(user.Username) && user.HasChanged(usernameField))
				{
					// Let's make sure the username is not in use by anyone besides this user (in case they didn't change it!).
					var usersWithUsername = await Where("Username=? and Id!=?", DataOptions.IgnorePermissions).Bind(user.Username).Bind(user.Id).Any(ctx);

					if (usersWithUsername)
					{
						throw new PublicException(config.UniqueUsernameMessage, "username_used");
					}
				}

				if (config.UniqueEmails && !string.IsNullOrEmpty(user.Email) && user.HasChanged(emailField))
			{		
					// Let's make sure the username is not in use by anyone besides this user (in case they didn't change it!).
					var usersWithEmail = await Where("Email=? and Id!=?", DataOptions.IgnorePermissions).Bind(user.Email).Bind(user.Id).Any(ctx);

					if (usersWithEmail)
					{
						throw new PublicException(config.UniqueEmailMessage, "email_used");
					}
				}

				return user;
			});

			InstallAdminPages("Users", "fa:fa-user", new string[] { "id", "email", "username" });
		}

		/// <summary>
		/// Generates the email verification hash for the given user.
		/// </summary>
		public string EmailVerificationHash(User user)
		{
			return CreateMD5(user.Id + "" + user.CreatedUtc.Ticks + "" + user.PrivateVerify);
		}
		
		private readonly SecureRandom secureRandom = new SecureRandom();
		
		private long RandomLong() {
			return secureRandom.NextLong();
		}
		
		/// <summary>
		/// Gets a hash of the given input.
		/// </summary>
		private static string CreateMD5(string input)
		{
			// Use input string to calculate MD5 hash
			using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
			byte[] hashBytes = md5.ComputeHash(inputBytes);

			// Convert the byte array to hexadecimal string
			var sb = new StringBuilder();
			for (int i = 0; i < hashBytes.Length; i++)
			{
				sb.Append(hashBytes[i].ToString("X2"));
			}
			return sb.ToString();
		}
		
		/// <summary>
		/// Gets a user by the given email address or username.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="emailOrUsername"></param>
		/// <returns></returns>
		public async ValueTask<User> Get(Context context, string emailOrUsername)
        {
			return await Where("Email=? or Username=?", DataOptions.IgnorePermissions).Bind(emailOrUsername).Bind(emailOrUsername).Last(context);
        }

		/// <summary>
		/// Gets a user by the given email.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="email"></param>
		/// <returns></returns>
		public async ValueTask<User> GetByEmail(Context context, string email)
		{
			return await Where("Email=?", DataOptions.IgnorePermissions).Bind(email).Last(context);
		}
		
		/// <summary>
		/// Gets a user by the given username.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="username"></param>
		/// <returns></returns>
		public async ValueTask<User> GetByUsername(Context context, string username)
		{
			return await Where("Username=?", DataOptions.IgnorePermissions).Bind(username).Last(context);
		}

		/// <summary>
		/// Attempt to auth a user now. If successful, returns an auth token to use.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="body"></param>
		/// <returns></returns>
		public async ValueTask<LoginResult> Authenticate(Context context, UserLogin body)
		{
			body = await Events.UserBeforeAuthenticate.Dispatch(context, body);
			
			if (body == null)
			{
				// Something rejected this request entirely.
				return null;
			}

			// Fire the user on auth event next. An authentication handler must pick up this request.

			LoginResult result = null;
			result = await Events.UserOnAuthenticate.Dispatch(context, result, body);

			if (result == null || result.User == null)
			{
				// Details were probably wrong.
				return null;
			}

			if (result.MoreDetailRequired == null)
			{
				result.CookieName = Context.CookieName;

				// Create a new context token (basically a signed string which can identify a context with this user/ role/ locale etc):
				context.User = result.User;
				result.LoginData = body;

				await Events.UserOnLogin.Dispatch(context, result);

				result.Success = true;
			}
			else
			{
				// Clear result.User to avoid leaking anything in this partial success.
				result.User = null;
			}

			return result;
		}

		/// <summary>
		/// Send a verification email to the user.
		/// </summary>
		/// <param name="context"></param>
		/// // <param name="user"></param>
		/// <returns>The token sent to the user</returns>
		public async ValueTask<string> SendVerificationEmail(Context context, User user)
        {
			var token = EmailVerificationHash(user);

			// Send email now. The key is a hash of the user ID + registration date + verify.
			var recipient = new Recipient(user)
			{
				CustomData = new EmailVerifyCustomData()
				{
					Token = token,
					UserId = user.Id
				}
			};

			var recipients = new List<Recipient>
			{
				recipient
			};

			if (_emails == null)
			{
				_emails = Services.Get<EmailTemplateService>();
			}

			await _emails.SendAsync(
				recipients,
				"verify_email"
			);

			var userChangeFields = GetChangeField("EmailVerifyToken");

			if (await StartUpdate(context, user, DataOptions.IgnorePermissions))
            {
				user.EmailVerifyToken = token;

				user.MarkChanged(userChangeFields);

				user = await FinishUpdate(context, user);
            }

			return token;
        }

		/// <summary>
		/// Verify the users email. If a password is supplied, the users password is also set.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="user"></param>
		/// <param name="newPassword"></param>
		/// <returns>The user</returns>
		public async ValueTask<User> VerifyEmail(Context context, User user, string newPassword)
        {
			var userChangeFields = GetChangeField("Role");

			if (await StartUpdate(context, user, DataOptions.IgnorePermissions))
			{
				if (!string.IsNullOrWhiteSpace(newPassword))
				{
					userChangeFields = userChangeFields.And("PasswordHash");
				
					var authService = Services.Get<PasswordAuthService>();
				
					await authService.EnforcePolicy(newPassword);

					user.PasswordHash = PasswordStorage.CreateHash(newPassword);
				}
				
				user.Role = Roles.Member.Id;

				user.MarkChanged(userChangeFields);

				user = await FinishUpdate(context, user);
			}
			else
			{
				user = null;
			}

			return user;
        }
	}

}
