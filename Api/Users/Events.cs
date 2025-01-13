using Api.Users;
using Api.Permissions;
using System.Collections.Generic;
using Api.Contexts;

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
		/// Just after an anon user has been identified.
		/// </summary>
		public static EventHandler<Context, Microsoft.AspNetCore.Http.HttpRequest> ContextAfterAnonymous;
		
		/// <summary>
		/// Set of events for a User.
		/// </summary>
		public static UserEventGroup User;
		
		#endregion
	}

	/// <summary>
	/// Custom user specific events.
	/// </summary>
	public class UserEventGroup : EventGroup<User>
	{

		/// <summary>
		/// During a login. This is where you can make context changes due to a user logging in.
		/// </summary>
		public EventHandler<LogoutResult> Logout;

		/// <summary>
		/// An event which runs when the verify email is sent. Make it return null to block the verify email.
		/// </summary>
		public EventHandler<User> OnSendVerificationEmail;
	}

}
