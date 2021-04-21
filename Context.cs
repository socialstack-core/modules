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
		/// Main cookie name
		/// </summary>
		public static readonly string CookieName = "user";

		/// <summary>
		/// Create a default anonymous context.
		/// </summary>
		public Context() { }

		/// <summary>
		/// Create a context for no particular user but with the given role.
		/// </summary>
		/// <param name="role"></param>
		public Context(Role role) {
			_roleId = role.Id;
		}

		/// <summary>
		/// Create a context for no particular user but with the given role.
		/// </summary>
		/// <param name="roleId"></param>
		/// <param name="userId"></param>
		/// <param name="localeId"></param>
		public Context(uint localeId, uint userId, uint roleId)
		{
			_roleId = roleId;
			_userId = userId;
			_localeId = localeId;
		}

		/// <summary>
		/// Create a context for no particular user but with the given role.
		/// </summary>
		/// <param name="roleId"></param>
		/// <param name="user"></param>
		/// <param name="localeId"></param>
		public Context(uint localeId, User user, uint roleId)
		{
			_roleId = roleId;
			_user = user;
			_userId = user == null ? 0 : user.Id;
			_localeId = localeId;
		}

		/// <summary>
		/// A date in the past used to set expiry on cookies.
		/// </summary>
		// private readonly static DateTimeOffset ThePast = new DateTimeOffset(1993, 1, 1, 0, 0, 0, TimeSpan.Zero);

		private static LocaleService _locales;
		private static RoleService _roles;
		private static ContextService _contextService;

		/// <summary>
		/// Don't set this! Use the Options argument on e.g. aService.List calls - it will manage this field for you. True if this context will skip permissions checking.
		/// </summary>
		public bool IgnorePermissions;

		/// <summary>
		/// Set to false if editedUtc values shouldn't be updated.
		/// </summary>
		public bool PermitEditedUtcChange = true;
		
		/// <summary>
		/// Underlying locale ID.
		/// </summary>
		private uint _localeId = 1;

		/// <summary>
		/// Underlying role ID.
		/// </summary>
		private uint _roleId = 6;  // Public is the default

		/// <summary>
		/// Underlying user ID.
		/// </summary>
		private uint _userId;

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

		/// <summary>
		///  The logged in users ID.
		/// </summary>
		public uint UserId {
			get {
				return _userId;
			}
		}

		/// <summary>
		/// The role ID from the token.
		/// </summary>
		public uint RoleId {
			get
			{
				return _roleId;
			}
		}

		/// <summary>
		/// Triggers an anon user event. Potentially may instance a new user if they're anonymous but need to be tracked.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public async ValueTask RoleCheck(HttpRequest request, HttpResponse response)
		{
			if (UserId == 0)
			{
				// Anonymous - fire off the anon user event:
				await Events.ContextAfterAnonymous.Dispatch(this, this, request);
			}
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
		/// Get the user associated to this login token.
		/// </summary>
		/// <returns></returns>
		public User User
		{
			get {
				return _user;
			}
			set
			{
				if (value == null)
				{
					_user = null;
					_userId = 0;
					_roleId = 6;
					return;
				}

				_user = value;
				_userId = value.Id;
				_roleId = value.Role;
			}
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
