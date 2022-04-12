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
	public partial class UserService : AutoService<User>
    {
        private EmailTemplateService _emails;
		
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
		/// Generates the email verification hash for the given user.
		/// </summary>
		public string EmailVerificationHash(User user)
		{
			return CreateMD5(user.Id + "" + user.CreatedUtc.Ticks + "" + user.PrivateVerify);
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

				if (user.Role == Roles.Guest.Id)
				{
					user.Role = Roles.Member.Id;
				}

				user.MarkChanged(userChangeFields);

				var loginResult = new LoginResult();
				loginResult.CookieName = Context.CookieName;
				loginResult.User = user;
				context.User = user;

				// Act like a login:
				await Events.UserOnLogin.Dispatch(context, loginResult);

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
