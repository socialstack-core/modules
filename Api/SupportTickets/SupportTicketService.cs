using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;


namespace Api.SupportTickets
{
	/// <summary>
	/// Handles projects.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class SupportTicketService : AutoService<SupportTicket>, ISupportTicketService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public SupportTicketService() : base(Events.SupportTicket)
        {
			InstallAdminPages("Support Tickets", "fa:fa-rocket", new string[] { "id", "title" });
		}
	}
    
}