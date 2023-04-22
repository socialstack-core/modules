using Api.Configuration;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Api.Startup;
using System;
using System.Collections;
using System.Collections.Generic;
using Api.ColourConsole;

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
	/// This service's config (as a set).
	/// </summary>
	private object _loadedSetConfiguration;

	/// <summary>
	/// Get all config of the given type.
	/// This set is live - it will update whenever the underlying config does.
	/// </summary>
	public ConfigSet<T> GetAllConfig<T>() where T : Config, new()
	{
		if (_loadedSetConfiguration != null)
		{
			return (ConfigSet<T>)_loadedSetConfiguration;
		}

		// Ask the cache of the config service. The key is based on the name of the type.
		var configService = Services.Get<ConfigurationService>();

		// Live TBD!

		var set = new ConfigSet<T>();
		set.Configurations = new List<T>();
		_loadedSetConfiguration = set;

		if (configService != null)
		{
			var name = typeof(T).Name;

			if (name.EndsWith("Config"))
			{
				// Trim it:
				name = name.Substring(0, name.Length - 6);
			}
			else if (name.EndsWith("Configuration"))
			{
				// Trim it:
				name = name.Substring(0, name.Length - 13);
			}
			
			var allConfig = configService.AllFromCache(name);

			foreach(var entry in allConfig)
			{
				try
				{
					var res = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(entry.ConfigJson);
					res.Id = entry.Id;
					entry.SetObject = set;
					entry.ConfigObject = res;
					set.Configurations.Add(res);
					configService.Map[entry.Id] = entry;
					configService.UpdateFrontendConfig(res, set);
				}
				catch (Exception e)
				{
                    WriteColourLine.Warning("[WARN] Invalid JSON detected in config for '" + name +
						"'. It's in the site_configuration database table as row #" + entry.Id + ". The full error is below.");
					Console.WriteLine(e.ToString());
				}
			}
		}

		return set;
	}

	/// <summary>
	/// Get config. Always returns the latest configuration.
	/// You can safely reuse references to the object returned - if the config changes, it'll still be the same object.
	/// You can find when it changes (or loads the first time) via the Config.OnChange event.
	/// Also note that the config section key is the name of the given type, minus "Config" or "Configuration" from the end.
	/// Optionally stores a default config entry in the database if none were found.
	/// </summary>
	public T GetConfig<T>(Action<T> onNewConfig = null, bool storeDefault = true) where T:Config, new()
	{
		if(_loadedConfiguration == null)
		{
			// Ask the cache of the config service. The key is based on the name of the type.
			var configService = Services.Get<ConfigurationService>();

			var name = typeof(T).Name;

			if (name.EndsWith("Config"))
			{
				// Trim it:
				name = name.Substring(0, name.Length - 6);
			}
			else if (name.EndsWith("Configuration"))
			{
				// Trim it:
				name = name.Substring(0, name.Length - 13);
			}

			if (configService != null)
			{
				var result = configService.FromCache(name);

				if (result != null)
				{
					// Parse as obj:
					try
					{
						var res = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(result.ConfigJson);
						_loadedConfiguration = res;
						result.ConfigObject = res;
						res.Id = result.Id;
						configService.Map[result.Id] = result;
						configService.UpdateFrontendConfig(res, null);
						return res;
					}
					catch (Exception e)
					{
                        WriteColourLine.Warning("[WARN] Invalid JSON detected in config for '" + name + 
							"'. It's in the site_configuration database table as row #" + result.Id + ". The full error is below.");
						Console.WriteLine(e.ToString());
					}
				}
			}

			//Try from appsettings:
			_loadedConfiguration = AppSettings.GetSection(name).Get<T>();

			if (_loadedConfiguration == null)
			{
				// default cfg:
				var dflt = new T();
				_loadedConfiguration = dflt;

				if (onNewConfig != null)
				{
					onNewConfig(dflt);
				}

				// Store in the DB:
				if (configService != null && storeDefault)
				{
					_ = configService.InstallConfig(dflt, name, name);
				}
			}

		}
		
		return (T)_loadedConfiguration;
	}
	
}