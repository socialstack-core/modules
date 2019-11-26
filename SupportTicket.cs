using System;
using Api.Database;


namespace Api.SupportTickets
{
	
	/// <summary>
	/// A support ticket. These just store additional info about a 
	/// forum thread without bloating the thread table.
	/// </summary>
	[DatabaseField(AutoIncrement=false)]
	public partial class SupportTicket : DatabaseRow
	{
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