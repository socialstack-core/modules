
using Api.Configuration;

namespace Api.CanvasRenderer
{
	
	/// <summary>
	/// Config for the canvas renderer service.
	/// </summary>
	public partial class CanvasRendererServiceConfig : Config
	{
		
		/// <summary>
		/// The module set to use when rendering canvases. Either "Admin", "Email" or "UI". 
		/// The default is "Admin" as it always includes the modules from the other 2 sets.
		/// </summary>
		public string Modules { get; set; } = "Admin";

		/// <summary>
		/// Debug render info to console?
		/// </summary>
		public bool DebugToConsole { get; set; } = true;

		/// <summary>
		/// Turns on the JS debugger. Connect to it with Chrome devtools on port 8118. Enabled remotely too, i.e. you can debug a remote server as well.
		/// </summary>
		public bool EnableJsDebugger { get; set; } = false;
		
	}
	
}