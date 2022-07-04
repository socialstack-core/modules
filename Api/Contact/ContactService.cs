using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;

namespace Api.Contacts
{
	/// <summary>
	/// Handles articles - containers for individual article posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ContactService : AutoService<Contact>, IContactService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ContactService() : base(Events.Contact) {
			
			InstallAdminPages("Contact", "fa:fa-envelope", new string[] { "id", "name" });
			
		}
	
	}
    
}
