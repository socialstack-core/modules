using Api.PasswordResetRequests;
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
		/// <summary>
		/// Set of events for a passwordResetRequest.
		/// </summary>
		public static EventGroup<PasswordResetRequest> PasswordResetRequest;
		
		/// <summary>
		/// After successful reset.
		/// </summary>
		public static EventHandler<PasswordResetRequest> PasswordResetRequestAfterSuccess;
	}
}