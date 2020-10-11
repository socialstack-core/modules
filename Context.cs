using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Configuration;
using Api.Eventing;
using Api.Permissions;
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
	public partial class Context : ClaimsPrincipal
	{
		/// <summary>
		/// The identity this token represents (a generic user).
		/// </summary>
		private static System.Security.Principal.GenericIdentity GenericIdentity = new System.Security.Principal.GenericIdentity("User");
		private static UserService _users;
		private static LocaleService _locales;
		private static ContextService _contextService;


		private int _localeId = 1;
		
		/// <summary>
		/// Set to false if editedUtc values shouldn't be updated.
		/// </summary>
		public bool PermitEditedUtcChange = true;
		
		/// <summary>
		/// True if this context has the given content in it. For example, checks if this content has User #12, or Company #120 etc.
		/// </summary>
		public bool HasContent(int contentTypeId, int contentId)
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
			var contextsContentId = (int)fieldInfo.Get.Invoke(this, null);
			
			// This context has the content if the ID (in the context) matches the content we were asked about:
			return contextsContentId == contentId;
		}
		
		/// <summary>
		/// The current locale or the site default.
		/// </summary>
		public int LocaleId
		{
			get
			{
				return _localeId;
			}
			set
			{
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
		public async Task<Locale> GetLocale()
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
			_locale = await _locales.Get(this, LocaleId <= 0 ? 1 : LocaleId);

			if (_locale == null)
			{
				// Dodgy locale in the cookie. Locale #1 always exists.
				return await _locales.Get(this, 1);
			}

			return _locale;
		}

		private int _userId;

		/// <summary>
		///  The logged in users ID.
		/// </summary>
		public int UserId {
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
		public int UserRef { get; set; }

		/// <summary>
		/// The role ID from the token.
		/// </summary>
		public int RoleId {get; set;}

		/// <summary>
		/// The full user object, if it has been requested.
		/// </summary>
		private User _user;
		
		/// <summary>
		/// The nested type mask, used to automatically detect cyclical references.
		/// </summary>
		public ulong NestedTypes;

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
				return RoleId >= Roles.All.Length ? null : Roles.All[RoleId];
			}
		}

		/// <summary>
		/// Creates a new login token.
		/// </summary>
		public Context() : base(GenericIdentity) { }

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
		public async Task<User> GetUser()
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
			_user = await _users.Get(this, UserId);
			
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
		/// Builds a public context. Used by e.g. self or login/ register EP's.
		/// </summary>
		public async Task<PublicContext> GetPublicContext()
		{
			var ctx = new PublicContext();
			ctx.User = await GetUser();
			ctx.Locale = await GetLocale();
			ctx.Role = Role;

			// Get any custom extensions:
			ctx = await Events.PubliccontextOnSetup.Dispatch(this, ctx);

			return ctx;
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
		/// Sends a token into the 
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

	/// <summary>
	/// The publicly (to the user themselves) exposed context.
	/// </summary>
	public partial class PublicContext
	{

		/// <summary>
		/// Authed user.
		/// </summary>
		public User User;

		/// <summary>
		/// Authed user locale.
		/// </summary>
		public Locale Locale;

		/// <summary>
		/// Authed user role.
		/// </summary>
		public Role Role;

	}
}
