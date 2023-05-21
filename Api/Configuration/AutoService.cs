using Api.Configuration;
using Api.Startup;
using System;
using System.Collections.Generic;

/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
public partial class AutoService
{
	/// <summary>
	/// Get all config of the given type.
	/// This set is live - it will update whenever the underlying config does. Do not call this more than once per config object.
	/// </summary>
	public ConfigSet<T> GetAllConfig<T>(Action<T> onNewConfig = null, bool storeDefault = true) where T : Config, new()
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

		// Already loaded?
		if (Config.SetMap.TryGetValue(name, out ConfigSet existingSet))
		{
			return (ConfigSet<T>)existingSet;
		}

		var set = new ConfigSet<T>();
		set.Name = name;
		set.Configurations = new List<T>();

		Config.SetMap[name] = set;

		// Ask the cache of the config service. The key is based on the name of the type.
		var configService = Services.Get<ConfigurationService>();

		if (configService != null)
		{
			var allConfig = configService.AllFromCache(name);

			foreach(var entry in allConfig)
			{
				try
				{
					var res = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(entry.ConfigJson);
					res.Id = entry.Id;
					set.Configurations.Add(res);
					configService.UpdateFrontendConfig(res, set);
				}
				catch (Exception e)
				{
					Log.Warn(LogTag, e, "Invalid JSON detected in config for '" + name +
						"'. It's in the site_configuration database table as row #" + entry.Id + ".");
				}
			}
		}

		// Try get from appsettings:
		var appsettingsConfig = AppSettings.GetSection(name).Get<T>();

		if (appsettingsConfig != null)
		{
			set.Configurations.Add(appsettingsConfig);
		}

		if (set.Configurations.Count > 0)
		{
			set.Primary = set.Configurations[0];
		}

		if (set.Primary == null)
		{
			// Set default cfg now:
			set.Primary = new T();

			if (onNewConfig != null)
			{
				onNewConfig(set.Primary);
			}

			// Store in the DB:
			if (configService != null && storeDefault)
			{
				_ = configService.InstallConfig(set.Primary, name, name, set);
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
		return GetAllConfig<T>(onNewConfig, storeDefault).Primary;
	}
	
}