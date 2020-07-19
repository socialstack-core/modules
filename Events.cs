using Api.Users;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		#region Service events
		
		/// <summary>
		/// During authentication. Add a handler to this to define custom auth schemes, or secondary auth schemes like 2FA.
		/// </summary>
		public static EventHandler<LoginResult, UserLogin> UserOnAuthenticate;

		/// <summary>
		/// Just before updating an user. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<UserLogin> UserBeforeAuthenticate;

		/// <summary>
		/// During a login. This is where you can make context changes due to a user logging in.
		/// </summary>
		public static EventHandler<LoginResult> UserOnLogin;

		/// <summary>
		/// Just after a user has authenticated.
		/// </summary>
		public static EventHandler<User> UserAfterAuthenticate;
		
		/// <summary>
		/// Just after a profile has loaded.
		/// </summary>
		public static EventHandler<UserProfile> UserProfileAfterLoad;
		
		/// <summary>
		/// Set of events for a User.
		/// </summary>
		public static EventGroup<User> User;
		
		#endregion

		#region Controller events
		
		/// <summary>
		/// A user profile was loaded.
		/// </summary>
		public static EndpointEventHandler<UserProfile> UserProfileLoad;
		
		/// <summary>
		/// User profiles are being listed.
		/// </summary>
		public static EndpointEventHandler<Filter<User>> UserProfileList;
		
		/// <summary>
		/// User profiles are being listed.
		/// </summary>
		public static EndpointEventHandler<List<UserProfile>> UserProfileListed;
		
		/// <summary>
		/// Create a new user.
		/// </summary>
		public static EndpointEventHandler<User> UserCreate;
		/// <summary>
		/// Delete an user.
		/// </summary>
		public static EndpointEventHandler<User> UserDelete;
		/// <summary>
		/// Update user metadata.
		/// </summary>
		public static EndpointEventHandler<User> UserUpdate;
		/// <summary>
		/// Load user metadata.
		/// </summary>
		public static EndpointEventHandler<User> UserLoad;
		/// <summary>
		/// Uploading files.
		/// </summary>
		public static EndpointEventHandler<UserImageUpload> UserOnUpload;
		/// <summary>
		/// List users.
		/// </summary>
		public static EndpointEventHandler<Filter<User>> UserList;

		#endregion

	}

}
