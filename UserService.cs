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

namespace Api.Users
{

	/// <summary>
	/// Manages user accounts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public class UserService : AutoService<User>, IUserService
    {
        private IContextService _contexts;
		private readonly Query<User> selectByEmailOrUsernameQuery;
		private readonly Query<User> selectByUsernameQuery;
		private readonly Query<User> selectByEmailQuery;
		private readonly Query<User> updateAvatarQuery;
		private readonly Query<User> updateFeatureQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserService(IContextService context) : base(Events.User)
		{
			_contexts = context;
			updateAvatarQuery = Query.Update<User>().RemoveAllBut("Id", "AvatarRef");
			updateFeatureQuery = Query.Update<User>().RemoveAllBut("Id", "FeatureRef");
			selectByEmailOrUsernameQuery = Query.Select<User>();
			selectByEmailOrUsernameQuery.Where().EqualsArg("Email", 0).Or().EqualsArg("Username", 0);

			selectByUsernameQuery = Query.Select<User>();
			selectByUsernameQuery.Where().EqualsArg("Username", 0);

			selectByEmailQuery = Query.Select<User>();
			selectByEmailQuery.Where().EqualsArg("Email", 0);

			// Because of IHaveCreatorUser, User must be nestable:
			MakeNestable();

			SetupAutoUserFieldEvents();
			SetupProfileFieldTransfers();
			
			Events.User.BeforeSettable.AddEventListener((Context ctx, JsonField<User> field) => {
				
				if (field == null)
				{
					return Task.FromResult(field);
				}
				
				if(field.Name == "Role")
				{
					// Only admins can update this field.
					// Will be permission system based in the future
					return Task.FromResult((field.ForRole == Roles.Admin || field.ForRole == Roles.SuperAdmin) ? field : null);
				}
				else if(field.Name == "JoinedUtc")
				{
					// Not settable
					field = null;
				}

				return Task.FromResult(field);
			});
			
			Events.User.BeforeCreate.AddEventListener((Context ctx, User user) => {
				
				if(user == null){
					return Task.FromResult(user);
				}
				
				// Default role is Member:
				user.Role = Roles.Member.Id;
				
				// Join date:
				user.JoinedUtc = DateTime.UtcNow;
				
				return Task.FromResult(user);
			});
			
			InstallAdminPages("Users", "fa:fa-user", new string[] { "id", "email", "username" });
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
					Console.WriteLine("Warning: You've got a public field in the UserProfile object called '" + targetField.Name + "' but it's not in a User object. It's value will be null.");
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
		/// Hooks up List/Load events for all types which user our auto setup interfaces.
		/// </summary>
		private void SetupAutoUserFieldEvents()
		{
			var loadEvents = Events.FindByType(typeof(IHaveCreatorUser), "Load", EventPlacement.After);

			foreach (var loadEvent in loadEvents)
			{
				loadEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// The primary object is always the first arg which should be an IHaveCreatorUser type:
					if (!(args[0] is IHaveCreatorUser userObject))
					{
						// Due to the way how event chains work, the primary object can be null.
						// Safely ignore this.
						return null;
					}

					userObject.CreatorUser = await GetProfile(context, userObject.GetCreatorUserId());

					return userObject;
				});

			}

			// Create events:
			var createEvents = Events.FindByType(typeof(IHaveCreatorUser), "Create", EventPlacement.After);

			foreach (var createEvent in createEvents)
			{
				createEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// The primary object is always the first arg which should be an IHaveCreatorUser type:
					if (!(args[0] is IHaveCreatorUser userObject))
					{
						// Due to the way how event chains work, the primary object can be null.
						// Safely ignore this.
						return null;
					}

					userObject.CreatorUser = await GetProfile(context, userObject.GetCreatorUserId());

					return userObject;
				});

			}


			// Next the List events:
			var listEvents = Events.FindByType(typeof(IHaveCreatorUser), "List", EventPlacement.After);

			foreach (var listEvent in listEvents)
			{
				listEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// args[0] is a List of IHaveCreatorUser implementors.
					if (!(args[0] is IList list))
					{
						// Can't handle this (or it was null anyway):
						return args[0];
					}

					// First we'll collect all their IDs so we can do a single bulk lookup.
					// ASSUMPTION: The list is not excessively long!
					// FUTURE IMPROVEMENT: Do this in chunks of ~50k entries.
					// (applies to at least categories/ tags).
					
					var uniqueUsers = new Dictionary<int, UserProfile>();

					for (var i = 0; i < list.Count; i++)
					{
						// Get as IHaveCreatorUser objects:
						if (!(list[i] is IHaveCreatorUser ihc))
						{
							// A correctly functioning list endpoint would never return nulls - 
							// that indicates something deeper is going on.
							throw new Exception("Bad IHaveCreatorUser object - nulls aren't permitted in these lists.");
						}

						// Add to content lookup so we can map the tags to it shortly:
						var creatorId = ihc.GetCreatorUserId();

						if (creatorId != 0)
						{
							uniqueUsers[creatorId] = null;
						}
					}

					if (uniqueUsers.Count == 0)
					{
						// Nothing to do - just return here:
						return list;
					}

					// Create the filter and run the query now:
					var userIds = new object[uniqueUsers.Count];
					var index = 0;
					foreach (var kvp in uniqueUsers) {
						userIds[index++] = kvp.Key;
					}

					var filter = new Filter<User>();
					filter.EqualsSet("Id", userIds);

					// Use the regular list method here:
					var allUsers = await List(context, filter);

					foreach (var user in allUsers)
					{
						// Get as the public profile and hook up the mapping:
						var profile = await GetProfile(context, user);
						uniqueUsers[user.Id] = profile;
					}

					for (var i = 0; i < list.Count; i++)
					{
						// Get as IHaveCreatorUser objects (must be valid because of the above check):
						var ihc = (IHaveCreatorUser)list[i];

						// Add to content lookup so we can map the tags to it shortly:
						var creatorId = ihc.GetCreatorUserId();

						// Note that this user object might be null.
						var userProfile = creatorId == 0 ? null : uniqueUsers[creatorId];

						// Get it as the public profile object next:
						ihc.CreatorUser = userProfile;
					}
					
					return list;
				});

			}
		}

		/// <summary>
		/// List a filtered set of users, mapped to their public profile.
		/// </summary>
		/// <returns></returns>
		public async Task<List<UserProfile>> ListProfiles(Context context, Filter<User> filter)
		{
			// Get the user list:
			var list = await List(context, filter);

			if (list == null)
			{
				return new List<UserProfile>();
			}

			// Map through:
			var profileList = new List<UserProfile>(list.Count);
			
			for(var i=0;i<list.Count;i++){
				// (this doesn't hit the database):
				profileList.Add(await GetProfile(context, list[i]));
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
			var listAndTotal = await ListWithTotal(context, filter);
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
				profileList.Add(await GetProfile(context, list[i]));
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
			var result = await Get(context, id);
			return await GetProfile(context, result);
		}

		/// <summary>
		/// Gets a public facing user profile.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public async Task<UserProfile> GetProfile(Context context, User result)
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

			// Run the load event:
			profile = await Events.UserProfileAfterLoad.Dispatch(context, profile);

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
			if (NestableAddMask != 0 && (context.NestedTypes & NestableAddMask) == NestableAddMask)
			{
				// This happens when we're nesting Get calls.
				// For example, a User has Tags which in turn have a (creator) User.
				return null;
			}

			var item = await _database.Select(context, selectByEmailOrUsernameQuery, emailOrUsername);

			context.NestedTypes |= NestableAddMask;
			item = await EventGroup.AfterLoad.Dispatch(context, item);
			context.NestedTypes &= NestableRemoveMask;
			return item;
        }

		/// <summary>
		/// Gets a user by the given email.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="email"></param>
		/// <returns></returns>
		public async Task<User> GetByEmail(Context context, string email)
		{
			if (NestableAddMask != 0 && (context.NestedTypes & NestableAddMask) == NestableAddMask)
			{
				// This happens when we're nesting Get calls.
				// For example, a User has Tags which in turn have a (creator) User.
				return null;
			}

			var item = await _database.Select(context, selectByEmailQuery, email);

			context.NestedTypes |= NestableAddMask;
			item = await EventGroup.AfterLoad.Dispatch(context, item);
			context.NestedTypes &= NestableRemoveMask;
			return item;
		}

		/// <summary>
		/// Gets a user by the given username.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="username"></param>
		/// <returns></returns>
		public async Task<User> GetByUsername(Context context, string username)
		{
			if (NestableAddMask != 0 && (context.NestedTypes & NestableAddMask) == NestableAddMask)
			{
				// This happens when we're nesting Get calls.
				// For example, a User has Tags which in turn have a (creator) User.
				return null;
			}

			var item = await _database.Select(context, selectByUsernameQuery, username);

			context.NestedTypes |= NestableAddMask;
			item = await EventGroup.AfterLoad.Dispatch(context, item);
			context.NestedTypes &= NestableRemoveMask;
			return item;
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

			if (string.IsNullOrEmpty(result.MoreDetailRequired))
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

		/// <summary>
		/// Updates a fileref for the given user.
		/// </summary>
		public async Task<bool> UpdateFile(Context context, int id, string uploadType, string uploadRef)
		{
			if (uploadType == "avatar")
			{
				return await _database.Run(context, updateAvatarQuery, id, uploadRef);
			}
			else if (uploadType == "feature")
			{
				return await _database.Run(context, updateFeatureQuery, id, uploadRef);
			}
			else
			{
				return false;
			}
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
