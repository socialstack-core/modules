using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.ContentSync
{
	/// <summary>
	/// Handles contentSync.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IContentSyncService
    {

		/// <summary>
		/// This server's ID from the ContentSync config.
		/// </summary>
		int ServerId {get; set;}
		
	}
}
