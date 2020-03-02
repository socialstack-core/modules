using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.StaffMembers
{
	/// <summary>
	/// Handles Staff Members.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class StaffMemberService : AutoService<StaffMember>, IStaffMemberService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public StaffMemberService() : base(Events.StaffMember)
        {
			
			// Create admin pages if they don't already exist:
			InstallAdminPages("Staff", "fa:fa-users", new string[]{"id", "firstName", "lastName"});
			
		}
	}
    
}
