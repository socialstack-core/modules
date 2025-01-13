using Api.Configuration;
using System.Collections.Generic;


namespace Api.CanvasRenderer
{
	
	/// <summary>
	/// Config for HtmlService
	/// </summary>
	public class FrontendCodeServiceConfig : Config
	{
		/// <summary>
		/// True if it should load the prebuilt UI. This is implied as true if no Source directory is found (i.e. you can force a true by just not deploying your UI/Source directory).
		/// </summary>
		public bool Prebuilt {get; set; } = false;

		/// <summary>
		/// Developer instances will automatically reload the UI whenever it updates (UI files are saved) if this is set true.
		/// </summary>
		public bool AutoReload { get; set; } = true;

		/// <summary>
		/// True if the watcher mode should run minified JS.
		/// </summary>
		public bool Minified { get; set; } = false;

		/// <summary>
		/// True to use React instead of Preact (Preact is the default).
		/// </summary>
		public bool React { get; set; } = false;

		/// <summary>
		/// Custom websocket URL. Use this to customise where the websocket is. It can contain a token - ${server.id} - to allow a cluster to have direct WS connectivity.
		/// e.g. wss://node${server.id}.mysite.com/live-websocket/
		/// </summary>
		public string WebSocketUrl { get; set; } = null;
		
		/// <summary>
		/// Set this to true to entirely disable the websocket.
		/// </summary>
		public bool DisableWebSocket {get; set;}

	}
}