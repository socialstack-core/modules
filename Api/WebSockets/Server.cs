namespace Api.SocketServerLibrary
{
	public partial class Server
	{
		/// <summary>
		/// Call this for this server to accept connections and messaging via websockets.
		/// </summary>
		/// <param name="requireApplicationHello"></param>
		public void AcceptWebsockets(bool requireApplicationHello = true)
		{
			var ocm = new WebsocketHandshake(requireApplicationHello);
			ocm.Code = 71;
			ocm.IsHello = true;
			AddToOpcodeMap(ocm.Code, ocm);
		}
	}
}