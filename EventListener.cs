
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System;
using System.Threading.Tasks;

namespace Api.ContentSync
{
	
	/// <summary>
	/// Listens for the remote sync type added event which indicates syncable types.
	/// </summary>
	[EventListener]
	public class EventListener
	{
		/// <summary>
		/// Instanced automatically
		/// </summary>
		public EventListener(){

			ContentSyncService cSyncService = null;

			Events.RemoteSyncTypeAdded.AddEventListener((Context ctx, Type type, int index) => {

				if (type == null)
				{
					return new ValueTask<Type>(type);
				}

				// Tell other servers that we're now listening for changes on this type.
				// The opcode will be directly based on index:
				var opcode = index + 10;

				if (cSyncService == null)
				{
					cSyncService = Services.Get<ContentSyncService>();
				}

				cSyncService.SyncRemoteType(type, opcode);
				
				return new ValueTask<Type>(type);
			});
			
		}
	}
	
}