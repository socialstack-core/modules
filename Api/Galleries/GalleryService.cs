using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Galleries
{
	/// <summary>
	/// Handles creations of galleries - containers for image uploads.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class GalleryService : AutoService<Gallery>, IGalleryService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public GalleryService() : base(Events.Gallery)
        {
		}
	}
}