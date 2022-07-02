using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.WebSockets;
using System;
using Api.Pages;

namespace Api.Presence
{
	/// <summary>
	/// Handles presenceRecords.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PresenceRecordService : AutoService<PresenceRecord>
    {
		/// <summary>
		/// True if presence should be stored in the DB.
		/// </summary>
		public bool StorePresence = true;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PresenceRecordService() : base(Events.PresenceRecord)
        {
			var pageContentTypeId = ContentTypes.GetId(typeof(Page));

			Events.Page.BeforeNavigate.AddEventListener((Context context, Page page, string url) => {
				// Hot path - don't block it up for analytics.

				if (page != null)
				{
					var record = new PresenceRecord()
					{
						EventName = "page",
						ContentTypeId = pageContentTypeId,
						ContentId = page.Id,
						UserId = context.UserId,
						CreatedUtc = DateTime.UtcNow,
						MetaJson = "{\"url\":\"" + url + "\"}"
					};

					// Don't wait:
					_ = Create(context, record, DataOptions.IgnorePermissions);
				}

				return new ValueTask<Page>(page);
			});
		}
	}
    
}
