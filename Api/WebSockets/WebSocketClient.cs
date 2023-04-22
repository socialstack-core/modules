using Api.Startup;
using Api.SocketServerLibrary;
using Api.Contexts;
using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Users;
using Api.ColourConsole;

namespace Api.WebSockets
{
	/// <summary>
	/// A connected websocket client.
	/// </summary>
	public partial class WebSocketClient : Client
	{
		/// <summary>
		/// Linked list of rooms that this client is currently in.
		/// </summary>
		public UserInRoom FirstRoom;
		/// <summary>
		/// Linked list of rooms that this client is currently in.
		/// </summary>
		public UserInRoom LastRoom;

		/// <summary>
		/// Leaves all rooms that this client is in.
		/// </summary>
		public void LeaveAllRooms()
		{
			var current = FirstRoom;

			// Expects that users will be in very few rooms at once.
			while (current != null)
			{
				var next = current.NextForClient;
				current.Remove();
				current = next;
			}
		}

		/// <summary>
		/// Gets the userInRoom for this client in the given room.
		/// Null if this user is not in the given room.
		/// </summary>
		/// <param name="room"></param>
		/// <returns></returns>
		public UserInRoom GetInNetworkRoom(NetworkRoom room)
		{
			if (room == null || room.IsEmptyLocally)
			{
				// The room is known to be empty. The user can't be in it.
				return null;
			}

			var current = FirstRoom;

			// Expects that users will be in very few rooms at once.
			while (current != null)
			{
				if (current.RoomBase == room)
				{
					return current;
				}

				current = current.NextForClient;
			}

			return null;
		}

		/// <summary>
		/// Gets the UserInRoom for the given custom ID. 
		/// Can only be obtained if such an ID was actually set in the first place.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public UserInRoom GetRoomById(uint id)
		{
			var current = FirstRoom;

			// Expects that users will be in very few rooms at once.
			while (current != null)
			{
				if (current.CustomId == id)
				{
					return current;
				}

				current = current.NextForClient;
			}

			return null;
		}

		/// <summary>
		/// Called when the client dc's (either intentionally or otherwise).
		/// </summary>
		public override void Close()
		{
			if (Socket != null)
			{	
				LeaveAllRooms();

				_ = ClientDisconnectedEvent();
			}

			base.Close();
		}

		private async ValueTask ClientDisconnectedEvent()
		{
			if (Context != null)
			{
				if (Context.UserId != 0)
				{
					// Try getting personal network room:
					NetworkRoom<User, uint, uint> personalRoomForCurrentId = WebSocketService.PersonalRooms == null ? null : WebSocketService.PersonalRooms.GetRoom(Context.UserId);

					// Trigger WS logout:
					await Events.WebSocket.AfterLogout.Dispatch(Context, this, personalRoomForCurrentId);
				}

				try
				{
					// Trigger disconnected event:
					await Events.WebSocket.Disconnected.Dispatch(Context, this);
				}
				catch (Exception e)
				{
                    WriteColourLine.Error(e.ToString());
				}
			}
		}

		/// <summary>
		/// Sets the context on this client.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public override async ValueTask SetContext(Context context)
		{
			var prevContext = Context;
			var prevUserId = Context != null ? Context.UserId : 0;
			Context = context;
			var newId = context != null ? context.UserId : 0;

			if (newId != prevUserId && WebSocketService.PersonalRooms != null)
			{
				if (prevUserId != 0)
				{
					// Remove from user personal room.
					var personalRoom = WebSocketService.PersonalRooms.GetRoom(prevUserId);

					var roomRef = GetInNetworkRoom(personalRoom);

					if (roomRef != null)
					{
						roomRef.Remove();
					}

					// Trigger WS logout:
					await Events.WebSocket.AfterLogout.Dispatch(prevContext, this, personalRoom);
				}

				if(newId != 0)
				{
					// Add to user personal room.
					var personalRoom = WebSocketService.PersonalRooms.GetOrCreateRoom(newId);

					await personalRoom.Add(this, 0);

					await Events.WebSocket.AfterLogin.Dispatch(context, this, personalRoom);
				}

			}
		}

		private static WebSocketService _wsService;

		/// <summary>
		/// The websocket service.
		/// </summary>
		private static WebSocketService WebSocketService
		{
			get {
				if (_wsService == null)
				{
					_wsService = Services.Get<WebSocketService>();
				}

				return _wsService;
			}
		}
		
	}
}