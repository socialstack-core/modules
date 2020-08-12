using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Api.ContentSync;
using System;
using Api.Users;
using Api.WebSockets;

namespace Api.ActiveLogins
{
	/// <summary>
	/// Handles activeLogins.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ActiveLoginService : AutoService<ActiveLogin>, IActiveLoginService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ActiveLoginService() : base(Events.ActiveLogin)
        {
			var serverId = 0;
			
			// Add event listeners for websocket users:
			Events.WebSocketUserState.AddEventListener(async (Context ctx, int userId, UserWebsocketLinks userSockets) =>
			{
				// Triggers when the user login state changes *locally*.
				if(userId == 0)
				{
					return userId;
				}
				
				if(serverId == 0){
					serverId = Services.Get<IContentSyncService>().ServerId;
				}
				
				if (userSockets.ContainsOne)
				{
					// Logged in (first time on this server)
					var now = DateTime.UtcNow;
					
					var activeLogin = await Create(ctx, new ActiveLogin(){
						UserId = userId,
						CreatedUtc = now,
						EditedUtc = now,
						Server = serverId
					});
					
					if(activeLogin != null){
						userSockets.ActiveLoginId = activeLogin.Id;
					}
				}
				else if (userSockets.First == null)
				{
					// Logged out (last time on this server)
					// Remove the entry from the DB for this server/ userId combo.
					if(userSockets.ActiveLoginId != 0)
					{
						var id = userSockets.ActiveLoginId;
						userSockets.ActiveLoginId = 0;
						await Delete(ctx, id);
					}
				}

				return userId;
			});

			if (Services.Started)
			{
				_ = Start();
			}
			else
			{
				// Must happen after services start otherwise the page service isn't necessarily available yet.
				// Notably this happens immediately after services start in the first group
				// (that's before any e.g. system pages are created).
				Events.ServicesAfterStart.AddEventListener(async (Context ctx, object src) =>
				{
					await Start();
					return src;
					
				// Happens on tick 2 which is immediately after contentSync starts.
				// This means the ContentSync service has the server config/ ID available.
				}, 2);
			}
		}
		
		/// <summary>
		/// Starts the ALS
		/// </summary>
		public async Task<bool> Start()
		{
			// Get server ID:
			var contentSyncService = Services.Get<IContentSyncService>();
			
			if(contentSyncService == null){
				return false;
			}
			
			var id = contentSyncService.ServerId;
			
			if(id == 0){
				// ALS requires a server ID.
				Console.WriteLine("[WARN] Active Login Service using ServerId 0 because ContentSync is not configured for this machine. This is fine locally.");
			}
			
			// Delete from DB now:
			var delQuery = Query.Delete<ActiveLogin>();
			delQuery.SetRawQuery("DELETE FROM " + typeof(ActiveLogin).TableName() + " WHERE Server=" + id);
			await _database.Run(null, delQuery);

			return true;
		}
	}
    
}
