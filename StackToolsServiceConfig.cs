
using Api.Configuration;

namespace Api.StackTools
{
	
	/// <summary>
	/// Config for the stack tools service.
	/// </summary>
	public class StackToolsServiceConfig : Config
	{
		/// <summary>
		/// True (default) if the watcher is active on debug builds.
		/// </summary>
		public bool WatcherActiveOnDebugBuilds {get; set;} = true;
		
		/// <summary>
		/// True (default) if the watcher should make production minified builds each time.
		/// </summary>
		public bool ProductionBuilds {get; set;}
	}
	
}