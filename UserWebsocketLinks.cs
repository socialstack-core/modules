
namespace Api.WebSockets
{

	/// <summary>
	/// All a particular user's websocket links.
	/// </summary>
	public partial class UserWebsocketLinks
	{
		/// <summary>
		/// Database row ID of the current active login entry for this user on this server.
		/// </summary>
		public int ActiveLoginId;
	}
	
}