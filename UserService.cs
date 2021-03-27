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

namespace Api.Users
{

	/// <summary>
	/// Manages user accounts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public class UserService : AutoService<User>
    {
        private readonly ContextService _contexts;
        private EmailTemplateService _emails;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserService(ContextService context) : base(Events.User)
		{
			_contexts = context;
			
			// Because of IHaveCreatorUser, User must be nestable:
			MakeNestable();

			SetupAutoUserFieldEvents();
			SetupProfileFieldTransfers();

			var config = GetConfig<UserServiceConfig>();
			
			InstallEmails(
				new EmailTemplate(){
					Name = "Verify email address",
					Subject = "Verify your email address",
					Key = "verify_email",
					BodyJson = "{\"module\":\"Email/Default\",\"content\":[{\"module\":\"Email/Centered\",\"data\":{}," +
					"\"content\":\"An account was recently created with us. If this was you, click the following link to proceed:\"},"+
					"{\"module\":\"Email/PrimaryButton\",\"data\":{\"label\":\"Verify my email address\",\"target\":\"/email-verify/{token}\"}}]}"
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
			
			Events.User.BeforeSettable.AddEventListener((Context ctx, JsonField<User> field) => {
				
				if (field == null)
				{
					return new ValueTask<JsonField<User>>(field);
				}
				
				if(field.Name == "Role")
				{
					// Only admins can update this field.
					// Will be permission system based in the future
					return new ValueTask<JsonField<User>>((field.ForRole == Roles.Admin || field.ForRole == Roles.SuperAdmin) ? field : null);
				}
				else if(field.Name == "JoinedUtc" || field.Name == "PrivateVerify")
				{
					// Not settable
					field = null;
				}

				return new ValueTask<JsonField<User>>(field);
			});
			
			Events.User.BeforeCreate.AddEventListener((Context ctx, User user) => {
				
				if(user == null){
					return new ValueTask<User>(user);
				}
				
				if(user.Role == 0 || (ctx.Role != Roles.SuperAdmin && ctx.Role != Roles.Admin))
				{
					// Default role is Member:
					user.Role = config.VerifyEmails ? Roles.Guest.Id : Roles.Member.Id;
				}
				
				// Generate a private verify value:
				user.PrivateVerify = RandomLong();
				
				// Join date:
				user.JoinedUtc = DateTime.UtcNow;

				return new ValueTask<User>(user);
			});

			Events.User.AfterCreate.AddEventListener(async (Context ctx, User user) => {
				
				if(user == null){
					return user;
				}
				
				if(config.VerifyEmails)
				{
					// Send email now. The key is a hash of the user ID + registration date + verify.
					var recipient = new Recipient(user)
					{
						CustomData = new EmailVerifyCustomData()
						{
							Token = EmailVerificationHash(user)
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
				}
				
				return user;
			});
			
			Events.User.BeforeCreate.AddEventListener(async (Context ctx, User user) =>
			{
				if (user == null || string.IsNullOrEmpty(user.Username))
				{
					return user;
				}

				// Let's see if the username is ok.
				if (config.UniqueUsernames)
				{
					// Let's make sure the username is not in use.
					var usersWithUsername = await List(ctx, new Filter<User>().Equals("Username", user.Username), DataOptions.IgnorePermissions);

					if (usersWithUsername.Count > 0)
					{
						throw new PublicException("This username is already in use.", "username_used");
					}
				}

				if (config.UniqueEmails)
				{
					// Let's make sure the username is not in use.
					var usersWithEmail = await List(ctx, new Filter<User>().Equals("Email", user.Email), DataOptions.IgnorePermissions);

					if (usersWithEmail.Count > 0)
					{
						throw new PublicException("This email is already in use.", "email_used");
					}
				}

				return user;
			});

			Events.User.BeforeUpdate.AddEventListener(async (Context ctx, User user) =>
			{
				if (user == null || string.IsNullOrEmpty(user.Username))
                {
					return user;
                }

				if (config.UniqueUsernames)
				{
					// Let's make sure the username is not in use by anyone besides this user (in case they didn't change it!).
					var usersWithUsername = await List(ctx, new Filter<User>().Equals("Username", user.Username).And().Not().Equals("Id", user.Id), DataOptions.IgnorePermissions);

					if (usersWithUsername.Count > 0)
					{
						throw new PublicException("This username is already in use.", "username_used");
					}
				}

				if (config.UniqueEmails)
				{
					// Let's make sure the username is not in use by anyone besides this user (in case they didn't change it!).
					var usersWithEmail = await List(ctx, new Filter<User>().Equals("Email", user.Email).And().Not().Equals("Id", user.Id), DataOptions.IgnorePermissions);

					if (usersWithEmail.Count > 0)
					{
						throw new PublicException("This email is already in use.", "email_used");
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
			return CreateMD5(user.Id + "" + user.JoinedUtc.Ticks + "" + user.PrivateVerify);
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
		/// Public fields of the UserProfile object which will be auto transferred.
		/// </summary>
		private List<ProfileFieldTransfer> _profileFields;

		/// <summary>
		/// Sets up the auto transfers for profile fields.
		/// </summary>
		private void SetupProfileFieldTransfers()
		{
			var publicUserProfileFields = typeof(UserProfile).GetFields(BindingFlags.Instance | BindingFlags.Public);
			_profileFields = new List<ProfileFieldTransfer>();

			for (var i = 0; i < publicUserProfileFields.Length; i++)
			{
				// Attempt to get the field in the user object:
				var targetField = publicUserProfileFields[i];

				if (targetField.GetCustomAttribute<Newtonsoft.Json.JsonIgnoreAttribute>() != null)
				{
					continue;
				}

				var sourceField = typeof(User).GetField(targetField.Name, BindingFlags.Instance | BindingFlags.Public);

				if (sourceField != null)
				{
					_profileFields.Add(new ProfileFieldTransfer()
					{
						From = sourceField,
						To = targetField
					});

					continue;
				}
					
				// is it a property?
				var property = typeof(User).GetProperty(targetField.Name, BindingFlags.Instance | BindingFlags.Public);

				if (property == null)
				{
					Console.WriteLine("Warning: You've got a public field in the UserProfile object called '" + targetField.Name + "' but it's not in the User object. Its value will never be set.");
					continue;
				}

				_profileFields.Add(new ProfileFieldTransfer()
				{
					FromProperty = property.GetGetMethod(),
					To = targetField
				});
			}
		}

		/// <summary>
		/// Sets a particular type with CreatorUser handlers. Used via reflection.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="evtGroup"></param>
		public void SetupForCreatorUser<T>(EventGroup<T> evtGroup) where T : IHaveCreatorUser, new()
		{
			// Invoked by reflection

			evtGroup.AfterLoad.AddEventListener(async (Context context, T content) =>
			{
				if (content == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return content;
				}

				content.CreatorUser = await GetProfile(context, content.GetCreatorUserId());
				return content;
			});

			evtGroup.AfterCreate.AddEventListener(async (Context context, T content) =>
			{
				if (content == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return content;
				}

				content.CreatorUser = await GetProfile(context, content.GetCreatorUserId());
				return content;
			});

			evtGroup.AfterList.AddEventListener(async (Context context, List<T> content) =>
			{
				// First we'll collect all their IDs so we can do a single bulk lookup.
				// ASSUMPTION: The list is not excessively long!
				// FUTURE IMPROVEMENT: Do this in chunks of ~50k entries.
				// (applies to at least categories/ tags).

				var uniqueUsers = new Dictionary<int, UserProfile>();

				for (var i = 0; i < content.Count; i++)
				{
					// Add to content lookup so we can map the tags to it shortly:
					var creatorId = content[i].GetCreatorUserId();

					if (creatorId != 0)
					{
						uniqueUsers[creatorId] = null;
					}
				}

				if (uniqueUsers.Count == 0)
				{
					// Nothing to do - just return here:
					return content;
				}

				// Create the filter and run the query now:
				var userIds = new object[uniqueUsers.Count];
				var index = 0;
				foreach (var kvp in uniqueUsers)
				{
					userIds[index++] = kvp.Key;
				}

				var filter = new Filter<User>();
				filter.EqualsSet("Id", userIds);

				// Use the regular list method here:
				var allUsers = await List(context, filter, DataOptions.IgnorePermissions);

				foreach (var user in allUsers)
				{
					// Get as the public profile and hook up the mapping:
					var profile = GetProfile(user);
					uniqueUsers[user.Id] = profile;
				}

				for (var i = 0; i < content.Count; i++)
				{
					// Get as IHaveCreatorUser objects (must be valid because of the above check):
					var ihc = content[i];

					// Add to content lookup so we can map the tags to it shortly:
					var creatorId = ihc.GetCreatorUserId();

					// Note that this user object might be null.
					var userProfile = creatorId == 0 ? null : uniqueUsers[creatorId];

					// Get it as the public profile object next:
					ihc.CreatorUser = userProfile;
				}

				return content;
			});

		}

		/// <summary>
		/// Hooks up List/Load events for all types which user our auto setup interfaces.
		/// </summary>
		private void SetupAutoUserFieldEvents()
		{
			var methodInfo = GetType().GetMethod("SetupForCreatorUser");

			Events.Service.AfterCreate.AddEventListener((Context ctx, AutoService svc) => {

				if (svc == null || svc.ServicedType == null)
				{
					return new ValueTask<AutoService>(svc);
				}

				var eventGroup = svc.GetEventGroup();

				if (eventGroup == null)
				{
					return new ValueTask<AutoService>(svc);
				}

				if (typeof(IHaveCreatorUser).IsAssignableFrom(svc.ServicedType))
				{
					// Invoke setup for type:
					var setupType = methodInfo.MakeGenericMethod(new Type[] {
						svc.ServicedType
					});

					setupType.Invoke(this, new object[] {
						eventGroup
					});
				}

				return new ValueTask<AutoService>(svc);
			});
		}

		/// <summary>
		/// List a filtered set of users, mapped to their public profile.
		/// </summary>
		/// <returns></returns>
		public async Task<List<UserProfile>> ListProfiles(Context context, Filter<User> filter)
		{
			// Get the user list:
			var list = await List(context, filter, DataOptions.IgnorePermissions);

			if (list == null)
			{
				return new List<UserProfile>();
			}

			// Map through:
			var profileList = new List<UserProfile>(list.Count);
			
			for(var i=0;i<list.Count;i++){
				// (this doesn't hit the database):
				profileList.Add(GetProfile(list[i]));
			}
			
			return profileList;
		}

		/// <summary>
		/// List a filtered set of users, mapped to their public profile, with the total result count (i.e. for paginated results).
		/// </summary>
		/// <returns></returns>
		public async Task<ListWithTotal<UserProfile>> ListProfilesWithTotal(Context context, Filter<User> filter)
		{
			// Get the user list:
			var listAndTotal = await ListWithTotal(context, filter, DataOptions.IgnorePermissions);
			var list = listAndTotal.Results;

			if (list == null)
			{
				return new ListWithTotal<UserProfile>()
				{
					Results = new List<UserProfile>()
				};
			}

			// Map through:
			var profileList = new List<UserProfile>(list.Count);
			
			for(var i=0;i<list.Count;i++){
				// (this doesn't hit the database):
				profileList.Add(GetProfile(list[i]));
			}
			
			return new ListWithTotal<UserProfile>() {
				Results = profileList,
				Total = listAndTotal.Total
			};
		}
		
		/// <summary>
		/// Gets a public facing user profile.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<UserProfile> GetProfile(Context context, int id)
		{
			// First get the full user info:
			var result = await Get(context, id, DataOptions.IgnorePermissions);
			return GetProfile(result);
		}

		/// <summary>
		/// Gets a public facing user profile.
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		public UserProfile GetProfile(User result)
		{
			if (result == null)
			{
				return null;
			}

			// Create the profile:
			var profile = new UserProfile(result);

			// Transfer the auto fields:
			foreach (var field in _profileFields)
			{
				if (field.FromProperty == null)
				{
					// It's a regular field:
					field.To.SetValue(profile, field.From.GetValue(result));
				}
				else
				{
					// It's a property:
					field.To.SetValue(profile, field.FromProperty.Invoke(result, null));
				}
			}

			return profile;
		}

		/// <summary>
		/// Gets a user by the given email address or username.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="emailOrUsername"></param>
		/// <returns></returns>
		public async Task<User> Get(Context context, string emailOrUsername)
        {
			var results = await List(context, new Filter<User>().Equals("Email", emailOrUsername).Or().Equals("Username", emailOrUsername), DataOptions.IgnorePermissions);
			
			if(results == null || results.Count == 0)
			{
				return null;
			}
			
			// Latest one:
			return results[^1];
        }

		/// <summary>
		/// Gets a user by the given email.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="email"></param>
		/// <returns></returns>
		public async Task<User> GetByEmail(Context context, string email)
		{
			var results = await List(context, new Filter<User>().Equals("Email", email), DataOptions.IgnorePermissions);
			
			if(results == null || results.Count == 0)
			{
				return null;
			}
			
			// Latest one:
			return results[^1];
		}
		
		/// <summary>
		/// Gets a user by the given username.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="username"></param>
		/// <returns></returns>
		public async Task<User> GetByUsername(Context context, string username)
		{
			var results = await List(context, new Filter<User>().Equals("Username", username), DataOptions.IgnorePermissions);
			
			if(results == null || results.Count == 0)
			{
				return null;
			}
			
			// Latest one:
			return results[^1];
		}

		/// <summary>
		/// Attempt to auth a user now. If successful, returns an auth token to use.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="body"></param>
		/// <returns></returns>
		public async Task<LoginResult> Authenticate(Context context, UserLogin body)
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
				result.CookieName = _contexts.CookieName;

				// Create a new context token (basically a signed string which can identify a context with this user/ role/ locale etc):
				context.UserId = result.User.Id;
				context.UserRef = result.User.LoginRevokeCount;
				context.RoleId = result.User.Role;

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

	/// <summary>
	/// Defines the from/ to fields when setting up a UserProfile object.
	/// </summary>
	public class ProfileFieldTransfer
	{
		/// <summary>
		/// The source property in the User object. Either this or From is set.
		/// </summary>
		public MethodInfo FromProperty;

		/// <summary>
		/// The source field in the User object. Either this or FromProperty is set.
		/// </summary>
		public FieldInfo From;

		/// <summary>
		/// The target field in the UserProfile object.
		/// </summary>
		public FieldInfo To;
	}
}
