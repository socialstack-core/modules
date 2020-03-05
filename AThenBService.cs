using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;


namespace Api.IfAThenB
{
	/// <summary>
	/// Handles a then b rules.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class AThenBService : AutoService<AThenB>, IAThenBService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AThenBService() : base(Events.AThenB)
        {
			
			// Create admin pages if they don't already exist:
			InstallAdminPages("If A Then B", "fa:fa-long-arrow-alt-right", new string[]{"id", "eventName", "actionName"});
			
		}

	}
    
}
