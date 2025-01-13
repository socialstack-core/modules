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
			return await Where("Email=? or Username=?", DataOptions.IgnorePermissions)
				.Bind(emailOrUsername)
				.Bind(emailOrUsername)
				.Last(context);
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
		public static string EmailVerificationHash(User user)
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
			if (await Events.User.OnSendVerificationEmail.Dispatch(context, user) == null)
			{
				return null;
			}

			var token = EmailVerificationHash(user);

			if (IsTestSuite())
			{
				return token;
			}

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

			_emails ??= Services.Get<EmailTemplateService>();

			await _emails.SendAsync(
				recipients,
				"verify_email"
			);

			await Update(context, user, (Context ctx, User u, User orig) => {

				u.EmailVerifyToken = token;

			}, DataOptions.IgnorePermissions);

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
			var userToUpdate = await StartUpdate(context, user, DataOptions.IgnorePermissions);

			if(userToUpdate != null)
			{
				if (!string.IsNullOrWhiteSpace(newPassword))
				{
					var authService = Services.Get<PasswordAuthService>();

					await authService.EnforcePolicy(newPassword);

					userToUpdate.PasswordHash = PasswordStorage.CreateHash(newPassword);
				}

				if (userToUpdate.Role == Roles.Guest.Id)
				{
					userToUpdate.Role = Roles.Member.Id;
				}

				if (OnVerify != null)
				{
					OnVerify(context, userToUpdate, user);
				}

				var loginResult = new LoginResult();
				loginResult.CookieName = CookieName;
				loginResult.User = userToUpdate;
				context.User = userToUpdate;

				// Act like a login:
				await Events.UserOnLogin.Dispatch(context, loginResult);

				userToUpdate = await FinishUpdate(new Context(context.LocaleId, context.User, 1), userToUpdate, user, DataOptions.IgnorePermissions);
			}
			
			return userToUpdate;
		}
	}

}
