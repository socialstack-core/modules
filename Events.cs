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
		/// Just before a new user is created. The given user won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<User> UserBeforeCreate;

		/// <summary>
		/// Just after an user has been created. The given user object will now have an ID.
		/// </summary>
		public static EventHandler<User> UserAfterCreate;

		/// <summary>
		/// Just before an user is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<User> UserBeforeDelete;
		
		/// <summary>
		/// During authentication. Add a handler to this to define custom auth schemes, or secondary auth schemes like 2FA.
		/// </summary>
		public static EventHandler<LoginResult, UserLogin> UserOnAuthenticate;

		/// <summary>
		/// Just after an user has been deleted.
		/// </summary>
		public static EventHandler<User> UserAfterDelete;

		/// <summary>
		/// Just before updating an user. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<UserLogin> UserBeforeAuthenticate;

		/// <summary>
		/// Just after updating an user.
		/// </summary>
		public static EventHandler<User> UserAfterAuthenticate;

		/// <summary>
		/// Just before updating an user. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<User> UserBeforeUpdate;

		/// <summary>
		/// Just after updating an user.
		/// </summary>
		public static EventHandler<User> UserAfterUpdate;

		/// <summary>
		/// Just after an user was loaded.
		/// </summary>
		public static EventHandler<User> UserAfterLoad;

		/// <summary>
		/// Just before a service loads a user list.
		/// </summary>
		public static EventHandler<Filter<User>> UserBeforeList;

		/// <summary>
		/// Just after a user list was loaded.
		/// </summary>
		public static EventHandler<List<User>> UserAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new user.
		/// </summary>
		public static EndpointEventHandler<UserAutoForm> UserCreate;
		/// <summary>
		/// Delete an user.
		/// </summary>
		public static EndpointEventHandler<User> UserDelete;
		/// <summary>
		/// Update user metadata.
		/// </summary>
		public static EndpointEventHandler<UserAutoForm> UserUpdate;
		/// <summary>
		/// Load user metadata.
		/// </summary>
		public static EndpointEventHandler<User> UserLoad;
		/// <summary>
		/// Uploading files.
		/// </summary>
		public static EndpointEventHandler<UserImageUpload> UserOnUpload;
		/// <summary>
		/// Load user profile (generally public) metadata.
		/// </summary>
		public static EventHandler<UserProfile> UserProfileLoad;
		/// <summary>
		/// List users.
		/// </summary>
		public static EndpointEventHandler<Filter<User>> UserList;

		#endregion

	}

}
