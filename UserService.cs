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

namespace Api.Users
{

	/// <summary>
	/// Manages user accounts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public class UserService : AutoService<User>, IUserService
    {
        private IEmailService _email;
        private IContextService _contexts;
		private readonly Query<User> selectByEmailOrUsernameQuery;
		private readonly Query<User> selectByUsernameQuery;
		private readonly Query<User> selectByEmailQuery;
		private readonly Query<User> updateAvatarQuery;
		private readonly Query<User> updateFeatureQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserService(IEmailService email, IContextService context) : base(Events.User)
        {
            _email = email;
			_contexts = context;
			updateAvatarQuery = Query.Update<User>().RemoveAllBut("Id", "AvatarRef");
			updateFeatureQuery = Query.Update<User>().RemoveAllBut("Id", "FeatureRef");
			selectByEmailOrUsernameQuery = Query.Select<User>();
			selectByEmailOrUsernameQuery.Where().EqualsArg("Email", 0).Or().EqualsArg("Username", 0);

			selectByUsernameQuery = Query.Select<User>();
			selectByUsernameQuery.Where().EqualsArg("Username", 0);

			selectByEmailQuery = Query.Select<User>();
			selectByEmailQuery.Where().EqualsArg("Email", 0);

			SetupAutoUserFieldEvents();

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
			var profile = new UserProfile(result)
			{
				Id = result.Id,
				FeatureRef = result.FeatureRef,
				AvatarRef = result.AvatarRef,
                Username = result.Username
			};

			// Run the load event:
			profile = await Events.UserProfileLoad.Dispatch(context, profile);

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
			return await _database.Select(selectByEmailOrUsernameQuery, emailOrUsername);
        }

		/// <summary>
		/// Gets a user by the given email.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="email"></param>
		/// <returns></returns>
		public async Task<User> GetByEmail(Context context, string email)
		{
			return await _database.Select(selectByEmailQuery, email);
		}

		/// <summary>
		/// Gets a user by the given username.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="username"></param>
		/// <returns></returns>
		public async Task<User> GetByUsername(Context context, string username)
		{
			return await _database.Select(selectByUsernameQuery, username);
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
				result.Token = _contexts.CreateToken(result.User.Id, 0, result.User.Role);

				// The default expiry:
				result.Expiry = DateTime.UtcNow.AddDays(30);
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
				return await _database.Run(updateAvatarQuery, id, uploadRef);
			}
			else if (uploadType == "feature")
			{
				return await _database.Run(updateFeatureQuery, id, uploadRef);
			}
			else
			{
				return false;
			}
		}
	}
}
