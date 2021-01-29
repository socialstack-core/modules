using System.Collections;
using System.Collections.Generic;


namespace Api.Configuration
{
	
	/// <summary>
	/// Base class of every configuration block.
	/// To create config for your service, create a class that inherits this called {ServiceName}Config.
	/// For example, if your config is for ThingService, then you would 
    /// * Make	public partial class ThingServiceConfig : Config
	/// * GetConfig<ThingServiceConfig>() in e.g. the constructor of ThingService.
	/// Use the BeforeConfigure event to track when it's about to change.
	/// Config is JSON serialised so it can contain lists and sub classes as well.
	/// </summary>
	public partial class Config
	{
		
	}
	
}