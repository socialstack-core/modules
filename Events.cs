using Api.PasswordReset;
using Api.Permissions;
using Api.Users;

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
		/// Just before a new password reset is created. The given password reset won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<PasswordResetRequest> PasswordResetRequestBeforeCreate;

		/// <summary>
		/// Just after an password reset has been created. The given password reset object will now have an ID.
		/// </summary>
		public static EventHandler<PasswordResetRequest> PasswordResetRequestAfterCreate;

		/// <summary>
		/// Just before an password reset is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<PasswordResetRequest> PasswordResetRequestBeforeDelete;

		/// <summary>
		/// Just after an password reset has been deleted.
		/// </summary>
		public static EventHandler<PasswordResetRequest> PasswordResetRequestAfterDelete;

		/// <summary>
		/// Just before updating an password reset. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<PasswordResetRequest> PasswordResetRequestBeforeUpdate;

		/// <summary>
		/// Just after updating an password reset.
		/// </summary>
		public static EventHandler<PasswordResetRequest> PasswordResetRequestAfterUpdate;

		/// <summary>
		/// Just after an password reset was loaded.
		/// </summary>
		public static EventHandler<PasswordResetRequest> PasswordResetRequestAfterLoad;

		#endregion

		#region Controller events
		
		/// <summary>
		/// During a reset request.
		/// </summary>
		public static EndpointEventHandler<UserPasswordForgot, User> PasswordResetRequestReset;

		#endregion

	}

}
