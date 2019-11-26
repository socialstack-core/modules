using System;
using Api.Database;


namespace Api.ForumThreads
{
	
	/// <summary>
	/// Extension of forum thread so it also knows it's a support ticket.
	/// </summary>
	public partial class ForumThread
	{
		/// <summary>
		/// True if this thread is a support ticket.
		/// </summary>
		public bool IsSupportTicket;
	}
	
}