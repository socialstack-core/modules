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
	public partial class ActiveLoginHistoryService : AutoService<ActiveLoginHistory>
	{
		private UserService _users = null;
		private WebSocketService _wsService = null;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ActiveLoginHistoryService(WebSocketService wsService, UserService userService, ContentSyncService cSync) : base(Events.ActiveLoginHistory)
        {
			_wsService = wsService;
			_users = userService;

			uint serverId = cSync.ServerId;

			Events.User.BeforeSettable.AddEventListener((Context context, JsonField<User, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<User, uint>>(field);
				}

				if (field.Name == "OnlineState")
				{
					// This field isn't settable
					field = null;
				}

				return new ValueTask<JsonField<User, uint>>(field);
			});

			// Add event listeners for websocket users:
			Events.WebSocket.AfterLogin.AddEventListener(async (Context ctx, WebSocketClient client, NetworkRoom<User, uint, uint> personalNetworkRoom) =>
			{
				var onlineStateField = _users.GetChangeField("OnlineState");

				// Triggers when the user login state changes *locally*.
				if (client == null || ctx.UserId == 0)
				{
					return client;
				}

				// Does the network room have exactly one local client? If yes, this is the first time they joined it.
				if (personalNetworkRoom.First != null && personalNetworkRoom.First.Next == null)
				{
					// Logged in (first time on this server)
					var now = DateTime.UtcNow;

					// Make sure user row is definitely online:
					var user = await _users.Get(ctx, ctx.UserId, DataOptions.IgnorePermissions);

					if (user != null && (!user.OnlineState.HasValue || user.OnlineState != 1))
					{
						await _users.Update(ctx, user, (Context c, User u) => {
							user.OnlineState = 1;
							user.MarkChanged(onlineStateField);
						}, DataOptions.IgnorePermissions);

						// Insert to historical record. This user came online across the cluster.
						await Create(ctx, new ActiveLoginHistory()
						{
							UserId = user.Id,
							IsLogin = true,
							CreatedUtc = now
						}, DataOptions.IgnorePermissions);
					}
				}

				return client;
			});

			// Add event listeners for websocket users:
			Events.WebSocket.AfterLogout.AddEventListener(async (Context ctx, WebSocketClient client, NetworkRoom<User, uint, uint> personalNetworkRoom) =>
			{
				var onlineStateField = _users.GetChangeField("OnlineState");

				// Triggers when the user login state changes *locally*.
				if (client == null || ctx.UserId == 0)
				{
					return client;
				}

				// Is this room empty? If yes, clear the login state record.
				if (personalNetworkRoom == null || personalNetworkRoom.IsEmpty)
				{
					// Logged out (last time anywhere on this cluster)
					// Set OnlineState and create a historical logout record.
					var user = await _users.Get(ctx, ctx.UserId, DataOptions.IgnorePermissions);

					if (user != null && user.OnlineState.HasValue && user.OnlineState != 0)
					{
						await _users.Update(ctx, user, (Context c, User u) => {
							user.OnlineState = 0;
							user.MarkChanged(onlineStateField);
						}, DataOptions.IgnorePermissions);

						// Insert to historical record. This user came online across the cluster.
						await Create(ctx, new ActiveLoginHistory()
						{
							UserId = user.Id,
							IsLogin = false,
							CreatedUtc = DateTime.UtcNow
						}, DataOptions.IgnorePermissions);
					}
				}

				return client;
			});

			if (Services.Started)
			{
				_ = Start();
			}
			else
			{
				// Must happen after services start to ensure all caches are ready to go.
				Events.Service.AfterStart.AddEventListener(async (Context ctx, object src) =>
				{
					await Start();
					return src;

					// Happens after WS starts.
					// This means personal rooms are available.
				}, 20);
			}
		}


		/// <summary>
		/// Starts the ALS
		/// </summary>
		public async Task<bool> Start()
		{
			var onlineStateField = _users.GetChangeField("OnlineState");

			var ctx = new Context();

			// Get the list of global logins to validate:
			var onlineUsers = await _users.Where("OnlineState=?", DataOptions.IgnorePermissions).Bind((int?)1).ListAll(ctx);

			// Update online state for any users who should be marked as offline:
			if (onlineUsers != null)
			{
				// For each unique user, check if they should now be offline.
				// Do this by checking in with their personal network room.
				foreach (var user in onlineUsers)
				{
					if (user != null && user.OnlineState.HasValue && user.OnlineState != 0)
					{
						var personalRoom = _wsService.PersonalRooms.GetOrCreateRoom(user.Id);

						if (personalRoom.IsEmpty)
						{
							// The room is empty - mark as offline.
							await _users.Update(ctx, user, (Context c, User u) => {
								user.OnlineState = 0;
								user.MarkChanged(onlineStateField);
							}, DataOptions.IgnorePermissions);

							// Insert to historical record. This user came online across the cluster.
							await Create(ctx, new ActiveLoginHistory()
							{
								UserId = user.Id,
								IsLogin = false,
								CreatedUtc = DateTime.UtcNow,
							}, DataOptions.IgnorePermissions);
						}
					}
				}
			}

			return true;
		}
	}
}
