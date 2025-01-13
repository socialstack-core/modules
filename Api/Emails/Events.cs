using Api.Emails;
using Api.Permissions;
using Api.Users;
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
		/// Set of events for an emailTemplate.
		/// </summary>
		public static EmailEventGroup EmailTemplate;
	}

	/// <summary>
	/// Custom user specific events.
	/// </summary>
	public class EmailEventGroup : EventGroup<EmailTemplate>
	{

		/// <summary>
		/// During email send. Handle this event to override the SMTP send behaviour.
		/// </summary>
		public EventHandler<EmailToSend> Send;

	}

}