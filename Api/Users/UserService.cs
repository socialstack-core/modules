using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Api.Eventing;
using Api.Startup;
using Org.BouncyCastle.Security;


namespace Api.Users
{

	/// <summary>
	/// Manages user accounts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class UserService : AutoService<User>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserService() : base(Events.User)
		{
			var config = GetConfig<UserServiceConfig>();
			
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
				
				return user;
			});

			Events.User.BeforeUpdate.AddEventListener(async (Context ctx, User user, User originalUser) =>
			{
				if (user == null)
                {
					return user;
                }

				if (config.UniqueUsernames && !string.IsNullOrEmpty(user.Username) && user.Username != originalUser.Username)
				{
					// Let's make sure the username is not in use by anyone besides this user (in case they didn't change it!).
					var usersWithUsername = await Where("Username=? and Id!=?", DataOptions.IgnorePermissions).Bind(user.Username).Bind(user.Id).Any(ctx);

					if (usersWithUsername)
					{
						throw new PublicException(config.UniqueUsernameMessage, "username_used");
					}
				}
				
				return user;
			});
			
			InstallAdminPages("Users", "fa:fa-user", new string[] { "id", "email", "username", "role" });
		}

		private readonly SecureRandom secureRandom = new SecureRandom();
		
		private long RandomLong() {
			return secureRandom.NextLong();
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
	}

}
