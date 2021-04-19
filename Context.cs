using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Configuration;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Signatures;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Microsoft.AspNetCore.Http;

namespace Api.Contexts
{
	/// <summary>
	/// A context constructed primarily from a cookie value. 
	/// Uses other locale hints such as Accept-Lang when the user doesn't specifically have one set in the cookie.
	/// </summary>
	public partial class Context
	{
		/// <summary>
		/// A date in the past used to set expiry on cookies.
		/// </summary>
		private readonly static DateTimeOffset ThePast = new DateTimeOffset(1993, 1, 1, 0, 0, 0, TimeSpan.Zero);
		
		private static UserService _users;
		private static LocaleService _locales;
		private static RoleService _roles;
		private static ContextService _contextService;

		/// <summary>
		/// Don't set this! Use the Options argument on e.g. aService.List calls - it will manage this field for you. True if this context will skip permissions checking.
		/// </summary>
		public bool IgnorePermissions;

		private uint _localeId = 1;
		
		/// <summary>
		/// Set to false if editedUtc values shouldn't be updated.
		/// </summary>
		public bool PermitEditedUtcChange = true;
		
		/// <summary>
		/// True if this context has the given content in it. For example, checks if this content has User #12, or Company #120 etc.
		/// </summary>
		public bool HasContent(int contentTypeId, uint contentId)
		{
			if (_contextService == null)
			{
				_contextService = Services.Get<ContextService>();
			}
			
			// See if a field exists for the given contentTypeId on the Context object:
			var fieldInfo = _contextService.FieldByContentType(contentTypeId);
			
			if(fieldInfo == null){
				return false;
			}
			
			// Read the ID to match with:
			var contextsContentId = (uint)fieldInfo.Get.Invoke(this, null);
			
			// This context has the content if the ID (in the context) matches the content we were asked about:
			return contextsContentId == contentId;
		}
		
		/// <summary>
		/// The current locale or the site default.
		/// </summary>
		public uint LocaleId
		{
			get
			{
				return _localeId;
			}
			set
			{
				if (value == 0)
				{
					value = 1;
				}

				_locale = null;
				_localeId = value;
			}
		}

		/// <summary>
		/// The full locale object, if it has been requested.
		/// </summary>
		private Locale _locale;

		/// <summary>
		/// Gets the locale for this context.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<Locale> GetLocale()
		{
			if (_locale != null)
			{
				return _locale;
			}

			if (_locales == null)
			{
				_locales = Services.Get<LocaleService>();
			}

			// Get the user now:
			_locale = await _locales.Get(this, LocaleId, DataOptions.IgnorePermissions);

			if (_locale == null)
			{
				// Dodgy locale in the cookie. Locale #1 always exists.
				return await _locales.Get(this, 1, DataOptions.IgnorePermissions);
			}

			return _locale;
		}

		private uint _userId;

		/// <summary>
		///  The logged in users ID.
		/// </summary>
		public uint UserId {
			get {
				return _userId;
			}
			set {
				_userId = value;
				_user = null;
			}
		}

		/// <summary>
		/// A number held in the user row which is used to check for signature revocations.
		/// </summary>
		public uint UserRef { get; set; }

		/// <summary>
		/// Underlying role ID.
		/// </summary>
		private uint _roleId = 6;  // Public is the default

		/// <summary>
		/// The role ID from the token.
		/// </summary>
		public uint RoleId {
			get
			{
				return _roleId;
			}
			set
			{
				if (value == 0)
				{
					// Public (role #6):
					value = 6;
				}

				_role = null;
				_roleId = value;
			}
		}

		/// <summary>
		/// Triggers a role check which will force a logout if the role changed, 
		/// or potentially instance a new user if they're anonymous but need to be tracked.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public async ValueTask RoleCheck(HttpRequest request, HttpResponse response)
		{
			var uId = _userId;
			var rId = _roleId;
			var user = await GetUser();

			var userRole = user == null ? 6 : user.Role;

			if (userRole == 0)
			{
				userRole = 6;
			}

			if (userRole != rId)
			{
				if (_contextService == null)
				{
					_contextService = Services.Get<ContextService>();
				}

				// Set as anon:
				UserId = 0;
				RoleId = 6;
			}

			if (UserId == 0 && CookieState != 0)
			{
				// Anonymous - fire off the anon user event:
				await Events.ContextAfterAnonymous.Dispatch(this, this, request);
			}

			/*
			 if(UserId == 0){
			 // Force reset if role changed. Getting the public context will verify that the roles match.
				response.Cookies.Append(
					_contextService.CookieName,
					"",
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Domain = _contextService.GetDomain(),
						IsEssential = true,
						Expires = ThePast
					}
				);

				response.Cookies.Append(
					_contextService.CookieName,
					"",
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Expires = ThePast
					}
				);
			}
			 */

		}

		/// <summary>
		/// Full role object.
		/// </summary>
		private Role _role;

		/// <summary>
		/// The full user object, if it has been requested.
		/// </summary>
		private User _user;
		
		/// <summary>
		/// Used to inform about the validity of the context cookie.
		/// </summary>
		public int CookieState;

		/// <summary>
		/// Role. Null indicates a broken AuthUser instance or user of a Role ID which probably hasn't been setup.
		/// </summary>
		public Role Role
		{
			get
			{
				if (_roles == null)
				{
					_roles = Services.Get<RoleService>();
				}

				if (_role != null)
				{
					return _role;
				}

				// RoleService is always cached:
				var cache = _roles.GetCacheForLocale(_localeId);
				_role = cache.Get(RoleId);
				return _role;
			}
		}

		/// <summary>
		/// Sets the given user as the logged in contextual user.
		/// </summary>
		/// <param name="user"></param>
		public void SetUser(User user)
		{
			if (user == null)
			{
				_user = null;
				UserId = 0;
				return;
			}

			_user = user;
			UserId = user.Id;
		}

		/// <summary>
		/// Get the user associated to this login token.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<User> GetUser()
		{
			if (_user != null)
			{
				return _user;
			}

			if (UserId == 0)
			{
				return null;
			}

			if (_users == null)
			{
				_users = Services.Get<UserService>();
			}

			// Get the user now:
			_user = await _users.Get(this, UserId, DataOptions.IgnorePermissions);
			
			if(_user ==  null)
			{
				// Got a cookie for an account that was deleted or otherwise doesn't exist
				return null;
			}
			
			// Overwrite role just in case (the revoke system must catch this though):
			RoleId = _user.Role;

			return _user;
		}

		/// <summary>
		/// Generates a new token for this ctx. Typically put into a cookie.
		/// </summary>
		/// <returns></returns>
		public string CreateToken()
		{
			if (_contextService == null)
			{
				_contextService = Services.Get<ContextService>();
			}

			return _contextService.CreateToken(this);
		}

		/// <summary>
		/// Creates a remote token for this context. 
		/// Essentially, this allows this context to be used on a remote thirdparty system, provided 
		/// that the remote system has permitted the public key of the given keypair by adding it to its SignatureService Hosts config.
		/// </summary>
		/// <param name="hostName"></param>
		/// <param name="keyPair"></param>
		/// <returns></returns>
		public string CreateRemoteToken(string hostName, KeyPair keyPair)
		{
			if (_contextService == null)
			{
				_contextService = Services.Get<ContextService>();
			}

			return _contextService.CreateRemoteToken(this, hostName, keyPair);
		}

		/// <summary>
		/// Sends a token as both a header and a cookie for the given response.
		/// </summary>
		/// <param name="response"></param>
		public void SendToken(HttpResponse response)
		{
			var token = CreateToken();
			response.Headers.Append("Token", token);

			var expiry = DateTime.UtcNow.AddDays(120);

			if (_contextService == null)
			{
				_contextService = Services.Get<ContextService>();
			}

			response.Cookies.Append(
					_contextService.CookieName,
					token,
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Expires = expiry,
						Domain = _contextService.GetDomain(),
						IsEssential = true
					}
				);
		}

	}

}
