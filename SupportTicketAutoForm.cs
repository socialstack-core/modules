using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.AutoForms;


namespace Api.SupportTickets
{
    /// <summary>
    /// Used when creating a support ticket
    /// </summary>
    public partial class SupportTicketAutoForm : AutoForm<SupportTicket>
	{
		/// <summary>
		/// The forum that the ticket will be in.
		/// </summary>
		public int ForumId;

		/// <summary>
		/// The title of the ticket.
		/// </summary>
		public string Title;
		
		/// <summary>
		/// The full canvas JSON of the ticket. If you just want raw text/ html, use {"content": "text or html here"}.
		/// It's a canvas so you can embed media or do powerful formatting if you wish.
		/// </summary>
		public string BodyJson;
    }
}
