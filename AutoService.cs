using Api.Configuration;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
public partial class AutoService
{
	/// <summary>
	/// This service's config.
	/// </summary>
	private object _loadedConfiguration;
	
	/// <summary>
	/// Service config. Always returns the latest configuration.
	/// You can safely reuse references to the object returned - if the config changes, it'll still be the same object.
	/// You can find when it changes (or loads the first time) via the Configure event.
	/// </summary>
	public T GetConfig<T>()
	{
		if(_loadedConfiguration == null)
		{
			// For now this is only from appsettings:
			_loadedConfiguration = AppSettings.GetSection(GetType().Name).Get<T>();
			
			/*
			 * When config is CMS backed, it'll fire this Configure event.
			if(EventGroup != null){
				_ = EventGroup.Configure.Dispatch(null, _loadedConfiguration);
			}
			*/
		}
		
		return (T)_loadedConfiguration;
	}
	
}