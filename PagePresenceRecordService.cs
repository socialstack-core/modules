using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Contexts;
using Api.Eventing;
using Microsoft.Extensions.Configuration;
using Api.Configuration;
using Api.Startup;
using Api.WebSockets;
using Api.ContentSync;
using System;
using Api.Users;
using Api.Presence;
using Microsoft.Extensions.DependencyInjection;


namespace Api.Presence
{

    /// <summary>
    /// Handles pagePresenceRecords.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class PagePresenceRecordService : AutoService<PagePresenceRecord, ulong>
	{
		/// <summary>
		/// User service
		/// </summary>
		private UserService _users;

		private PagePresenceRecordConfig _configuration;
		/// <summary>
		/// True if this service is active.
		/// </summary>
		public bool Active;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PagePresenceRecordService() : base(Events.PagePresenceRecord)
		{
			_configuration = AppSettings.GetSection("PagePresenceRecord").Get<PagePresenceRecordConfig>();

			Active = _configuration != null && _configuration.Active;

			if (!Active)
			{
				return;
			}

			if (Services.Started)
			{
				Start();
			}
			else
			{
				// Must happen after services start otherwise the page service isn't necessarily available yet.
				// Notably this happens immediately after services start in the first group
				// (that's before any e.g. system pages are created).
				Events.Service.AfterStart.AddEventListener((Context ctx, object src) =>
				{
					Start();
					return new ValueTask<object>(src);

					// Happens on tick 2 which is immediately after contentSync starts.
					// This means the ContentSync service has the server config/ ID available.
				}, 2);
			}

		}

		/// <summary>
		/// Sets the presence of the given websocket client to the given page record.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="record"></param>
		/// <returns></returns>
		public async Task SetPresence(WebSocketClient client, PresenceRecord record)
		{
			// Url is from the meta which is always of the form:
			// {"url":"x"} so a substring can capture x for us:
			var url = record.MetaJson == null || record.MetaJson.Length < 10 ? "" : record.MetaJson.Substring(8, record.MetaJson.Length - 10);

			if (!IdSetup)
			{
				SetupServerId();
			}

			if (client.Record != null)
			{
				// Must delete it. This ensures removals occur on the client end.
				await Delete(client.Context, client.Record);
			}
			else
			{
				// First time for this client. Must ensure this record doesn't exist at the server end.
				// This happens when a server is abruptly shutdown - it leaves records in the DB.
				var rec = await Get(client.Context, client.Id + ServerIdMask, DataOptions.IgnorePermissions);
			
				if(rec != null){
					// Delete it:
					await Delete(client.Context, rec, DataOptions.IgnorePermissions);
				}
			}
			
			// Create a presence record by client+server ID. 
			var now = DateTime.UtcNow;

			client.Record = new PagePresenceRecord()
			{
				PageId = record.ContentId,
				ServerId = ServerId,
				Url = url,
				WebSocketId = client.Id,
				Id = client.Id + ServerIdMask,
				UserId = client.Context.UserId,
				CreatedUtc = now,
				EditedUtc = now
			};
			
			await Create(client.Context, client.Record);
			
			/*
				// MUST
				client.Record.Url = url;
				client.Record.PageId = record.ContentId;
				await Update(client.Context, client.Record);
			*/
		}

		private bool IdSetup;
		private ulong ServerIdMask;
		/// <summary>
		/// The server ID.
		/// </summary>
		public uint ServerId;

		private void SetupServerId()
		{
			if (IdSetup)
			{
				return;
			}

			var contentSyncService = Services.Get<ContentSyncService>();

			if (contentSyncService == null)
			{
				return;
			}

			IdSetup = true;
		    var id = contentSyncService.ServerId;
			ServerId = (uint)id;
			ServerIdMask = ((ulong)id) << 32;
		}

		/// <summary>
		/// Starts the page presence service
		/// </summary>
		public bool Start()
			{
			// Get server ID:
			SetupServerId();

			if (ServerId == 0)
			{
				// ALS requires a server ID.
				Console.WriteLine("[WARN] Page Record Service using ServerId 0 because ContentSync is not configured for this machine. This is fine locally.");
			}

            if (_users == null)
			{
				_users = Services.Get<UserService>();
			}

			// This service is always cached - cache it now:
			Cache(new CacheConfig<PagePresenceRecord>() {
				Preload = true,

				#warning todo - need to add a verification which runs after the cache has loaded and contentsync has started
				// It should wait for a moment, then check for any entries in the cache that do not represent actually connected users.
				// These "dangling" entries are caused by a prior shutdown of the server.
				
				/*
				OnCacheLoaded = async () => {

					
					// - because it needs to clear remote server caches too.
					// Cache has loaded - delete any entries which were from this server.
					var context = new Context();
					var priorServerEntries = await List(context, new Filter<PagePresenceRecord>().Equals("ServerId", ServerId));

					foreach (var entry in priorServerEntries)
					{
						await Delete(context, entry);
					}

				}
				*/
			});

			// Add event listeners for websocket users:
			Events.WebSocketClientDisconnected.AddEventListener(async (Context ctx, WebSocketClient client) =>
			{
				// Triggers when the user login state changes *locally*.
				if (client == null)
				{
					return client;
				}
				
				if (client != null && client.Record != null)
				{
					await Delete(ctx, client.Record, DataOptions.IgnorePermissions);
				}

				return client;
			});
			return true;
		}
	}

}

namespace Api.WebSockets
{
	/// <summary>
	/// A connected websocket client.
	/// </summary>
	public partial class WebSocketClient
	{
		/// <summary>
		/// This current WS clients page presence record.
		/// </summary>
		public PagePresenceRecord Record;
	}
}
