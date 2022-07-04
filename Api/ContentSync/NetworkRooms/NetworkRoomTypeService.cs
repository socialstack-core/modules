using Api.Eventing;
using Api.Startup;

namespace Api.ContentSync
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(2)]
	public partial class NetworkRoomTypeService : AutoService<NetworkRoomType>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NetworkRoomTypeService() : base(Events.NetworkRoomType)
		{
			// Don't cache this type.
		}
	}
}
