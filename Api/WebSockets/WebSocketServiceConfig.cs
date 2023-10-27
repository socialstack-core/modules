using Api.Configuration;


namespace Api.WebSockets
{
	/// <summary>
	/// Config for WS service.
	/// </summary>
	public class WebSocketServiceConfig : Config
	{
		/// <summary>
		/// True if all clients should be available via websocketservice.AllClients.
		/// </summary>
		public bool? TrackAllClients {get; set;}
	}
}