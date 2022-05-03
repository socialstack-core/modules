using Api.Users;


namespace Api.WebSockets
{
	public partial class WebSocketService : AutoService
    {
		/// <summary>
		/// The set of personal rooms.
		/// </summary>
		public NetworkRoomSet<User, uint, uint> PersonalRooms;
	}
}