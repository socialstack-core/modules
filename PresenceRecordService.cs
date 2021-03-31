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
		public PresenceRecordService(PagePresenceRecordService pagePresence) : base(Events.PresenceRecord)
        {
			var pageContentTypeId = ContentTypes.GetId(typeof(Page));
			
			Events.WebSocketMessage.AddEventListener((Context context,JObject message, WebSocketClient client, string type) => {
				
				if(type != "Pres"){
					return new ValueTask<JObject>(message);
				}
				
				var typeObj = message["c"];
				
				if(typeObj == null){
					return new ValueTask<JObject>(message);
					
				}
				
				string presType = typeObj.Value<string>();
				
				var metaObj = message["m"];
				var meta = metaObj == null ? null : metaObj.Value<string>();
				
				var record = new PresenceRecord(){
					EventName = presType,
					UserId = context.UserId,
					CreatedUtc = DateTime.UtcNow,
					MetaJson = meta
				};

				var idObj = message["id"];
				record.ContentId = idObj == null ? 0 : idObj.Value<int>();

				// Special handle for presType "page":
				if (presType == "page"){
					record.ContentTypeId = pageContentTypeId;
					
					if(pagePresence.Active){
						// (don't await this - we won't block the websocket for metrics): 
						_ = pagePresence.SetPresence(client, record);
					}
				}

				// Presence record. If it's for something we recognise, store it in the DB
				// (don't await this - we won't block the websocket for metrics):
				_ = Create(context, record, DataOptions.IgnorePermissions);
				
				return new ValueTask<JObject>(message);
			});
			
		}
	}
    
}
