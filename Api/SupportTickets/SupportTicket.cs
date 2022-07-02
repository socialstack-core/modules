using System;
using Api.Database;
using Api.Users;


namespace Api.SupportTickets
{
	
	/// <summary>
	/// A support ticket.
	/// </summary>
	public partial class SupportTicket : RevisionRow
	{
		/// <summary>
		/// The title of this ticket.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;
		
		/// <summary>
		/// The current ticket status.
		/// </summary>
		public int Status;

		/// <summary>
		/// The user ID of the current support team member working this ticket.
		/// </summary>
		public int? TeamMemberId;
	}
	
}