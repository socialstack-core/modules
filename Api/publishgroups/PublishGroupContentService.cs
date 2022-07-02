using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.PublishGroups
{
	/// <summary>
	/// Handles publishGroupContents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PublishGroupContentService : AutoService<PublishGroupContent>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PublishGroupContentService() : base(Events.PublishGroupContent)
        {
		}
	}
    
}
