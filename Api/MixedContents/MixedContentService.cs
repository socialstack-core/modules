using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.MixedContents
{
	/// <summary>
	/// Handles mixedContents. Does not store in the database at all, 
	/// but instead uses the content system to allow for mixed content types to be collected and returned via includes.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class MixedContentService : AutoService<MixedContent, ulong>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MixedContentService() : base(Events.MixedContent)
        {
		}
	}
    
}
