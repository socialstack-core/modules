using Api.Startup;
using Api.Users;
using System.Threading.Tasks;


namespace Api.Contexts
{
	/// <summary>
	/// A context constructed primarily from a cookie value. 
	/// Uses other locale hints such as Accept-Lang when the user doesn't specifically have one set in the cookie.
	/// </summary>
	partial class Context
	{
		
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
		/// Underlying user ID.
		/// </summary>
		private uint _userId;

		/// <summary>
		///  The logged in users ID.
		/// </summary>
		public uint UserId {
			get {
				return _userId;
			}
		}

		/// <summary>
		/// The full user object, if it has been requested.
		/// </summary>
		private User _user;
		
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
					_role = null;
					return;
				}

				_user = value;
				_userId = value.Id;
				_roleId = value.Role;
				_role = null;
			}
		}

		private static UserService _users;

		/// <summary>
		/// Underlying real user ID.
		/// </summary>
		private uint _realUserId = 0;

		/// <summary>
		/// The current real user ID or zero.
		/// </summary>
		[ContextField(OmitFromToken = true)]
		public uint RealUserId
		{
			get
			{
				return _realUserId;
			}
			set
			{
				_realUser = null;
				_realUserId = value;
			}
		}

		/// <summary>
		/// The full real user object, if it has been requested.
		/// </summary>
		private User _realUser;

		/// <summary>
		/// Gets the real user for this context.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<User> GetRealUser()
		{
			if (_realUser != null)
			{
				return _realUser;
			}

			if (_users == null)
			{
				_users = Services.Get<UserService>();
			}

			// Get the user now:
			_realUser = await _users.Get(this, RealUserId, DataOptions.IgnorePermissions);

			return _realUser;
		}
	}
	
}