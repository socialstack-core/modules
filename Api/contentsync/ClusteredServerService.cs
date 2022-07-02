using Api.Eventing;
using Api.Startup;


namespace Api.ContentSync
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(2)]
	public partial class ClusteredServerService : AutoService<ClusteredServer>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ClusteredServerService() : base(Events.ClusteredServer)
		{
			
		}
	}
}
