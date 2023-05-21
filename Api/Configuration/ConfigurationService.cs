using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Contexts;
using Api.Eventing;
using Api.AutoForms;
using Api.Startup;
using System.Reflection;
using System;
using System.Text;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;

namespace Api.Configuration
{
	/// <summary>
	/// Handles configurations.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(2)]
	public partial class ConfigurationService : AutoService<Api.Configuration.Configuration>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ConfigurationService(AutoFormService autoForms) : base(Events.Configuration)
        {
			// Example admin page install:
			InstallAdminPages("Configuration", "fa:fa-cogs", new string[] { "id", "name" });

			Cache();

			Events.Configuration.Received.AddEventListener(async (Context context, Configuration config, int type) => {

				// Some other server in the cluster updated config.
				// 1 = Create, 2 = Update, 3 = Delete.

				switch(type)
				{
					case 1:

						// Created
						if (PermittedOnThisEnvironment(config.Environments))
						{
							// Ok - any matching sets?
							if (config.Key != null && Config.SetMap.TryGetValue(config.Key, out ConfigSet cSet))
							{
								// Yep! Load the config object now and add it to the set.
								Config pConfig = null;

								try
								{
									pConfig = Newtonsoft.Json.JsonConvert.DeserializeObject(config.ConfigJson, cSet.EntryType) as Config;
								}
								catch (Exception e)
								{
									Console.WriteLine(e);
									return config;
								}

								if (pConfig != null)
								{
									pConfig.Id = config.Id;
									await cSet.UpdateInSet(pConfig);
								}
							}
						}

						break;
					case 2:

						// Updated
						// We don't know if the key just changed. It very likely didn't but it might have done so.
						// Because it probably didn't, we'll check its current key first. It's only in 1 set.

						Config parsedConfig = null;

						var permitted = PermittedOnThisEnvironment(config.Environments);

						if (Config.SetMap.TryGetValue(config.Key, out ConfigSet currentSet))
						{
							// It's probably currently in a set or needs to be added to one.

							// Try to parse the JSON. If it fails, reject the update.
							try
							{
								parsedConfig = Newtonsoft.Json.JsonConvert.DeserializeObject(config.ConfigJson, currentSet.EntryType) as Config;
							}
							catch (Exception e)
							{
								Console.WriteLine(e);
								return config;
							}

							if (parsedConfig == null)
							{
								return config;
							}

							// We can't identify if the key changed via comparison, so must search for it in all existing sets.
							// We'll take a guess that it's in the current one first though.

							var existingIndex = currentSet.GetIndex(config.Id);

							if (existingIndex == -1)
							{
								// Check every other set for a config object with this ID.
								foreach (var kvp in Config.SetMap)
								{
									if (kvp.Key == config.Key)
									{
										continue;
									}

									existingIndex = kvp.Value.GetIndex(config.Id);

									if (existingIndex != -1)
									{
										// Set changed.
										await kvp.Value.RemoveById(config.Id);
										break;
									}
								}
							}
							else if (!permitted)
							{
								// No longer permitted. Remove it.
								await currentSet.RemoveById(config.Id);
							}
						}
						else
						{
							// Target key set didn't exist. Check every other set for a config object with this ID.
							foreach (var kvp in Config.SetMap)
							{
								if (kvp.Key == config.Key)
								{
									continue;
								}

								var existingIndex = kvp.Value.GetIndex(config.Id);

								if (existingIndex != -1)
								{
									// Set changed.
									await kvp.Value.RemoveById(config.Id);
									break;
								}
							}
						}

						if (currentSet != null && permitted)
						{
							// Ensure ID is set:
							parsedConfig.Id = config.Id;

							// Add it or nudges the set if it is already in there:
							await currentSet.UpdateInSet(parsedConfig);
						}

						break;
					case 3:

						// Deleted
						if (config.Key != null && Config.SetMap.TryGetValue(config.Key, out ConfigSet delSet))
						{
							await delSet.RemoveById(config.Id);
						}

						break;
				}

				return config;
			});

			Events.Configuration.BeforeCreate.AddEventListener(async (Context context, Configuration config) =>
			{
				if (config == null)
				{
					return config;
				}

				if (string.IsNullOrEmpty(config.Key))
				{
					throw new PublicException("At least a key is required", "config_key_required");
				}

				// New config created. Does it belong in any global sets and is it permitted on this server?

				if (PermittedOnThisEnvironment(config.Environments))
				{
					// Ok - any matching sets?
					if (config.Key != null && Config.SetMap.TryGetValue(config.Key, out ConfigSet set))
					{
						// Yep! Load the config object now and add it to the set.
						Config parsedConfig = null;

						try
						{
							parsedConfig = Newtonsoft.Json.JsonConvert.DeserializeObject(config.ConfigJson, set.EntryType) as Config;
						}
						catch (Exception e)
						{
							Console.WriteLine(e);
							throw new PublicException("Unable to save configuration because the JSON is invalid.", "invalid_json");
						}

						if (parsedConfig == null)
						{
							throw new PublicException("Unable to save configuration because the JSON was null or did not match the target type at all.", "invalid_json");
						}

						parsedConfig.Id = config.Id;
						await set.UpdateInSet(parsedConfig);
					}
				}

				return config;
			}, 200);

			Events.Configuration.BeforeUpdate.AddEventListener(async (Context context, Configuration config, Configuration originalConfig) => {

				if (config == null)
				{
					return config;
				}

				if (string.IsNullOrEmpty(config.Key))
				{
					throw new PublicException("At least a key is required", "config_key_required");
				}

				// Get the set for the config type:
				Config parsedConfig = null;

				if (Config.SetMap.TryGetValue(config.Key, out ConfigSet currentSet))
				{
					// It's probably currently in a set or needs to be added to one.

					// Try to parse the JSON. If it fails, reject the update.
					try
					{
						parsedConfig = Newtonsoft.Json.JsonConvert.DeserializeObject(config.ConfigJson, currentSet.EntryType) as Config;
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
						throw new PublicException("Unable to save configuration because the JSON is invalid.", "invalid_json");
					}

					if (parsedConfig == null)
					{
						throw new PublicException("Unable to save configuration because the JSON was null or did not match the target type at all.", "invalid_json");
					}
				}

				var permitted = PermittedOnThisEnvironment(config.Environments);

				if ((!permitted || (originalConfig.Key != null && originalConfig.Key != config.Key)) && 
					Config.SetMap.TryGetValue(originalConfig.Key, out ConfigSet originalSet))
				{
					// The key changed or it is not permitted. Remove from original set:
					await originalSet.RemoveById(config.Id);
				}

				if (currentSet != null && permitted)
				{
					// Ensure ID is set:
					parsedConfig.Id = config.Id;

					// Add it or nudges the set if it is already in there:
					await currentSet.UpdateInSet(parsedConfig);
				}

				return config;
			}, 200);

			Events.Configuration.AfterDelete.AddEventListener(async (Context context, Configuration config) =>
			{
				// Config deleted. Any matching sets?
				if (config.Key != null && Config.SetMap.TryGetValue(config.Key, out ConfigSet set))
				{
					await set.RemoveById(config.Id);
				}

				return config;
			});

			// Register the autoforms for config entries:
			autoForms.RegisterCustomFormType("config", (Context context, Dictionary<string, AutoFormInfo> cache) => {
				
				// Populate the given cache now.
				// Do this by discovering all :Config classes in the code, then constructing the form for them.
				
				var allTypes = typeof(ConfigurationService).Assembly.DefinedTypes;

				foreach (var typeInfo in allTypes)
				{
					// If it:
					// - Is a class
					// - Inherits :Config
					// Then add to map

					if (!typeInfo.IsClass || typeInfo.IsAbstract)
					{
						continue;
					}

					if (!typeof(Config).IsAssignableFrom(typeInfo))
					{
						continue;
					}

					var name = typeInfo.Name;

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

					var fields = new List<AutoFormField>();

					cache[name.ToLower()] = new AutoFormInfo(){
						Fields = fields
					};

					var props = typeInfo.GetProperties();

					foreach (var property in props)
					{
						var field = new JsonField()
						{
							Name = property.Name,
							OriginalName = property.Name,
							PropertyInfo = property,
							Attributes = property.GetCustomAttributes(),
							TargetType = property.PropertyType,
							PropertyGet = property.GetGetMethod(),
							PropertySet = property.GetSetMethod()
						};

						field.SetDefaultDisplayModule();

						fields.Add(autoForms.BuildFieldInfo(field));

						fields = fields.OrderBy(f => f.Order).ToList();
					}

				}

				return new ValueTask();
			});
			
		}

		/// <summary>
		/// Json serialization settings for canvases
		/// </summary>
		private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

		/// <summary>
		/// Lowercases first letter of a given string
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private static string FirstCharToLowerCase(string str)
		{
			if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
				return str;

			return char.ToLower(str[0]) + str.Substring(1);
		}

		/// <summary>
		/// Creates a config row. The given object is jsonified and put into the DB.
		/// </summary>
		/// <returns></returns>
		public async ValueTask InstallConfig(Config cfg, string name, string key, ConfigSet set)
		{
			var cfgRow = new Configuration()
			{
				Name = name,
				Key = key,
				ConfigJson = JsonConvert.SerializeObject(cfg, Formatting.Indented)
			};
			await Create(new Context(), cfgRow, DataOptions.IgnorePermissions);
			cfg.Id = cfgRow.Id;
			await set.UpdateInSet(cfg);
		}

		/// <summary>
		///  Potentially adds/ updates the given config object to the frontend config, 
		///  depending on if it is marked with [Frontend] attributes or not.
		/// </summary>
		/// <param name="configObject"></param>
		/// <param name="configSet"></param>
		public void UpdateFrontendConfig(Config configObject, ConfigSet configSet)
		{
			var type = configObject.GetType();
			var properties = type.GetProperties();

			var feAttribute = type.GetCustomAttribute<FrontendAttribute>();

			StringBuilder sb = null;

			if (feAttribute != null)
			{
				// Everything is frontend on this config
				sb = new StringBuilder();
				sb.Append('{');
				var first = true;

				foreach (var property in properties)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						sb.Append(',');
					}
					sb.Append('"');
					sb.Append(FirstCharToLowerCase(property.Name));
					sb.Append("\":");
					sb.Append(Newtonsoft.Json.JsonConvert.SerializeObject(property.GetValue(configObject), jsonSettings));
				}

				sb.Append('}');
			}
			else
			{
				var first = true;

				foreach (var property in properties)
				{
					// Any might be marked as frontend:
					var propAttribute = property.GetCustomAttribute<FrontendAttribute>();

					if (propAttribute != null)
					{
						// Got some frontend config
						if (sb == null)
						{
							sb = new StringBuilder();
							sb.Append('{');
						}

						if (first)
						{
							first = false;
						}
						else
						{
							sb.Append(',');
						}
						sb.Append('"');
						sb.Append(FirstCharToLowerCase(property.Name));
						sb.Append("\":");
						sb.Append(Newtonsoft.Json.JsonConvert.SerializeObject(property.GetValue(configObject), jsonSettings));
					}
				}

				if (sb != null)
				{
					sb.Append('}');
				}
			}

			var newJson = (sb == null) ? null : sb.ToString();

			if (configObject.FrontendJson == newJson)
			{
				return;
			}
			
			configObject.FrontendJson = newJson;

			if (_allFrontendConfigs == null)
			{
				_allFrontendConfigs = new ConcurrentDictionary<Config, bool>();
			}

			if (configSet != null)
			{
				if (!_allFrontendConfigs.ContainsKey(configSet))
				{
					_allFrontendConfigs[configSet] = true;
				}
			}
			else
			{
				if (!_allFrontendConfigs.ContainsKey(configObject))
				{
					_allFrontendConfigs[configObject] = true;
				}
			}

			// Clear cached config bytes:
			_frontendConfigBytesJs = null;
			_frontendConfigBytesJson = null;
		}

		/// <summary>
		/// Configs for the frontend. Some may be a ConfigSet.
		/// </summary>
		private ConcurrentDictionary<Config, bool> _allFrontendConfigs;

		private byte[] _frontendConfigBytesJson;

		private byte[] _frontendConfigBytesJs;
		
		private string _frontendConfigJs;

		/// <summary>
		/// Gets the frontend config as a JS string.
		/// </summary>
		/// <returns></returns>
		public string GetLatestFrontendConfigJs()
		{
			GetLatestFrontendConfigBytes();
			return _frontendConfigJs;
		}

		/// <summary>
		/// Gets the frontend config as a UTF8 encoded block of bytes. Can be null if there isn't any.
		/// </summary>
		/// <returns></returns>
		public byte[] GetLatestFrontendConfigBytesJson()
		{
			GetLatestFrontendConfigBytes();
			return _frontendConfigBytesJson;
		}

		/// <summary>
		/// Gets the frontend config as a UTF8 encoded block of bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] GetLatestFrontendConfigBytes()
		{
			if (_frontendConfigBytesJs != null)
			{
				return _frontendConfigBytesJs;
			}

			if (_allFrontendConfigs == null)
			{
				_frontendConfigBytesJs = Array.Empty<byte>();
				return _frontendConfigBytesJs;
			}

			// For each FE config object..
			StringBuilder sb = new StringBuilder();

			sb.Append("{");

			var first = true;

			foreach (var kvp in _allFrontendConfigs)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					sb.Append(',');
				}

				var config = kvp.Key;

				var name = config.GetType().Name;

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

				sb.Append('"');
				sb.Append(name.ToLower());
				sb.Append("\":");

				// Add the configs frontend JSON:
				config.AddFrontendJson(sb);
			}

			sb.Append("}");

			var configStrJson = sb.ToString();
			_frontendConfigJs = "window.__cfg=" + configStrJson + ";";
			_frontendConfigBytesJs = Encoding.UTF8.GetBytes(_frontendConfigJs);
			_frontendConfigBytesJson = Encoding.UTF8.GetBytes(configStrJson);
			return _frontendConfigBytesJs;
		}

		/// <summary>
		/// True if the given environment string matches this environment.
		/// </summary>
		/// <param name="envString"></param>
		/// <returns></returns>
		public bool PermittedOnThisEnvironment(string envString)
		{
			if (string.IsNullOrWhiteSpace(envString))
			{
				return true;
			}

			// MUST match one. This is a fuzzy match so "dev" matches "Development".

			// Split:
			var envs = envString.Split(',');

			for (var i = 0; i < envs.Length; i++)
			{
				var sanitised = Services.SanitiseEnvironment(envs[i]);

				if (sanitised == Services.Environment)
				{
					return true;
				}
			}

			// Matched none.
			return false;
		}

		/// <summary>
		/// Gets config from the cache via the given key. Note that this actively omits entries from other environments.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public List<Configuration> AllFromCache(string key)
		{
			var set = new List<Configuration>();

			var cache = GetCacheForLocale(1);
			if (cache == null)
			{
				return set;
			}

			var keyIndex = cache.GetIndex<string>("Key") as NonUniqueIndex<Configuration, string>;
			var loop = keyIndex.GetEnumeratorFor(key);

			while (loop.HasMore())
			{
				var current = loop.Current();

				if (PermittedOnThisEnvironment(current.Environments))
				{
					set.Add(current);
				}
			}

			return set;
		}
		
		/// <summary>
		/// Gets config from the cache via the given key. Note that this actively omits entries from other environments.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public Configuration FromCache(string key)
		{
			var set = AllFromCache(key);

			if (set.Count > 0)
			{
				// Last one:
				return set[set.Count - 1];
			}

			return null;
		}
	}
    
}
