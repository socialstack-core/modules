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
using System.Text;

namespace Api.ActiveLogins
{
	/// <summary>
	/// Handles activeLogins.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ActiveLoginService : AutoService<ActiveLogin>, IActiveLoginService
	{
		private IUserService _users = null;

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
				
				if(_users == null)
				{
					_users = Services.Get<IUserService>();
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
					
					// Make sure user row is definitely online:
					var user = await _users.Get(ctx, userId);
					
					if(user != null && (!user.OnlineState.HasValue || user.OnlineState != 1)){
						user.OnlineState = 1;
						await _users.Update(ctx, user);
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
						
						// Ask the DB if this user is online anywhere currently:
						var onlineEntries = await List(ctx, new Filter<ActiveLogin>().Equals("UserId", userId));
						
						if(onlineEntries == null || onlineEntries.Count == 0)
						{
							var user = await _users.Get(ctx, userId);
							if(user != null && user.OnlineState.HasValue && user.OnlineState != 0){
								user.OnlineState = 0;
								await _users.Update(ctx, user);
							}
						}
						
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

			if (_users == null)
			{
				_users = Services.Get<IUserService>();
			}

			var ctx = new Context();

			// Get the list of active logins for the server:
			var onlineEntries = await List(ctx, new Filter<ActiveLogin>().Equals("Server", id));
			
			// Delete from DB now:
			var delQuery = Query.Delete<ActiveLogin>();
			delQuery.SetRawQuery("DELETE FROM " + typeof(ActiveLogin).TableName() + " WHERE Server=" + id);
			await _database.Run(null, delQuery);
			
			// Update online state for any users who should now be marked offline:
			if(onlineEntries != null){
				Dictionary<int, bool> uniqueUsers = new Dictionary<int, bool>();
				
				foreach(var entry in onlineEntries){
					if(entry == null || entry.UserId == 0)
					{
						continue;
					}
					uniqueUsers[entry.UserId] = true;
				}
				
				if(uniqueUsers.Count > 0)
				{
					var inQuery = new StringBuilder();
					inQuery.Append("SELECT count(*) as Count, UserId FROM " + typeof(ActiveLogin).TableName() + " where UserId in(");
					var first = true;
					
					foreach(var kvp in uniqueUsers)
					{
						if(first)
						{
							first = false;
						}
						else
						{
							inQuery.Append(',');
						}
						inQuery.Append(kvp.Key.ToString());
					}
					
					inQuery.Append(") group by UserId");
					
					// For each unique user, check if they should now be offline.
					// Do this by counting the # of other online records they have.
					var countQuery = Query.List<ActiveLoginCount>();
					countQuery.SetRawQuery(inQuery.ToString());
					var counts = await _database.List(null, countQuery, null);

					// NB: could be done in a query, but going through the update API
					// means other servers will become aware of the user(s) going offline.
					foreach (var entry in counts)
					{
						if (entry != null && entry.Count != 0)
						{
							uniqueUsers[entry.UserId] = false;
						}
					}

					foreach(var kvp in uniqueUsers)
					{
						if (!kvp.Value)
						{
							// It has other entries elsewhere
							continue;
						}
						// This user was online on this server, but now isn't.
						// Update to mark as offline:
						var user = await _users.Get(ctx, kvp.Key);
						if(user != null && user.OnlineState.HasValue && user.OnlineState != 0){
							user.OnlineState = 0;
							await _users.Update(ctx, user);
						}
					}
				}
			}
			
			return true;
		}
	}
	
	/// <summary>
	/// Active login counts per user.
	/// </summary>
	public class ActiveLoginCount{
		/// <summary>
		/// User Id.
		/// </summary>
		public int UserId;
		/// <summary>
		/// The number of servers this user is active on.
		/// </summary>
		public int Count;
	}
    
}
