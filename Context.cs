using System.Security.Claims;
using System.Threading.Tasks;
using Api.Permissions;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.Contexts
{
	/// <summary>
	/// A context constructed primarily from a cookie value. 
	/// Uses other locale hints such as Accept-Lang when the user doesn't specifically have one set in the cookie.
	/// </summary>
    public partial class Context : ClaimsPrincipal
	{
		/// <summary>
		/// The identity this token represents (a genericu user).
		/// </summary>
		private static System.Security.Principal.GenericIdentity GenericIdentity= new System.Security.Principal.GenericIdentity("User");
		private static IUserService _users;
		private static ILocaleService _locales;

		/// <summary>
		/// The current locale or the site default.
		/// </summary>
		public int LocaleId = 1;

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
				_locales = Services.Get<ILocaleService>();
			}

			// Get the user now:
			_locale = await _locales.GetCached(this, LocaleId <= 0 ? 1 : LocaleId);

			if (_locale == null)
			{
				// Dodgy locale in the cookie. Locale #1 always exists.
				return await _locales.GetCached(this, 1);
			}
			
			return _locale;
		}

		/// <summary>
		///  The logged in users ID.
		/// </summary>
		public int UserId;

		/// <summary>
		/// A number held in the user row which is used to check for signature revocations.
		/// </summary>
		public int UserRef;

		/// <summary>
		/// The role ID from the token.
		/// </summary>
		public int RoleId;

		/// <summary>
		/// The full user object, if it has been requested.
		/// </summary>
		private User _user;
		
		/// <summary>
		/// The nested type mask, used to automatically detect cyclical references.
		/// </summary>
		public ulong NestedTypes;
		
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
				_users = Services.Get<IUserService>();
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
		
	}
}
