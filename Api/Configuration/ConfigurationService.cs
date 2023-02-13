using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
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
		/// Loaded config map
		/// </summary>
		public Dictionary<uint, Configuration> Map = new Dictionary<uint, Configuration>();

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

				Map.TryGetValue(config.Id, out Configuration existingLoadedConfig);

				if (existingLoadedConfig != null)
				{
					config.ConfigObject = existingLoadedConfig.ConfigObject;
					config.SetObject = existingLoadedConfig.SetObject;
				}

				Map[config.Id] = config;

				try
				{
					await LoadConfig(config);
				}
				catch (Exception e)
				{
					Console.WriteLine("[WARN] Remote config update failed to load: " + e.ToString());
				}

				return config;
			});

			Events.Configuration.BeforeCreate.AddEventListener((Context context, Configuration config) =>
			{
				if (string.IsNullOrEmpty(config.Key))
				{
					throw new PublicException("At least a key is required", "config_key_required");
				}

				return new ValueTask<Configuration>(config);
			});

			Events.Configuration.BeforeUpdate.AddEventListener(async (Context context, Configuration config, Configuration originalConfig) => {

				if (string.IsNullOrEmpty(config.Key))
				{
					throw new PublicException("At least a key is required", "config_key_required");
				}

				// Attempt to parse the JSON:
				try
				{
					await LoadConfig(config);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					throw new PublicException("Unable to save configuration - the JSON is invalid.", "invalid_json");
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
					}

				}

				return new ValueTask();
			});
			
		}

		/// <summary>
		/// Updates the loaded config object, if there is one. May throw a JSON related error.
		/// </summary>
		/// <param name="config"></param>
		private async ValueTask LoadConfig(Configuration config)
		{
			if (config == null)
			{
				return;
			}
				
			if (config.ConfigObject != null)
			{
				// Deserialise it:
				var type = config.ConfigObject.GetType();
				var res = Newtonsoft.Json.JsonConvert.DeserializeObject(config.ConfigJson, type);

				// Copy values to existing object.
				// By doing this, any refs to the existing object are still valid.
				foreach (var property in type.GetProperties())
				{
					property.SetValue(config.ConfigObject, property.GetValue(res));
				}

				foreach (var field in type.GetFields())
				{
					if (field.Name == "OnChange")
					{
						// Retain original event set for this field.
						continue;
					}

					field.SetValue(config.ConfigObject, field.GetValue(res));
				}

				if (config.ConfigObject != null)
				{
					await config.ConfigObject.Changed();
				}

				UpdateFrontendConfig(config.ConfigObject, config.SetObject);
			}

			if (config.SetObject != null)
			{
				await config.SetObject.Changed();
			}
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
		public async ValueTask InstallConfig(Config cfg, string name, string key, Config set = null)
		{
			var cfgRow = new Configuration()
			{
				Name = name,
				Key = key,
				ConfigJson = JsonConvert.SerializeObject(cfg, Formatting.Indented)
			};
			cfgRow.ConfigObject = cfg;
			cfgRow.SetObject = set;
			await Create(new Context(), cfgRow, DataOptions.IgnorePermissions);
			cfg.Id = cfgRow.Id;
			UpdateFrontendConfig(cfg, null);

			if (set != null)
			{
				set.AddToSet(cfg);
			}
		}

		/// <summary>
		///  Potentially adds/ updates the given config object to the frontend config, 
		///  depending on if it is marked with [Frontend] attributes or not.
		/// </summary>
		/// <param name="configObject"></param>
		/// <param name="configSet"></param>
		public void UpdateFrontendConfig(Config configObject, Config configSet)
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
			var thisEnvironment = Services.Environment;

			while (loop.HasMore())
			{
				var current = loop.Current();

				if (!string.IsNullOrEmpty(current.Environments))
				{
					// Must contain the current environment.
					var environmentSet = current.Environments.ToLower().Split(',');
					var matched = false;

					for (var i = 0; i < environmentSet.Length; i++)
					{
						var checkWith = environmentSet[i].Trim();

						if (thisEnvironment == checkWith)
						{
							matched = true;
							break;
						}
					}

					if (!matched)
					{
						// Skip this config.
						continue;
					}
				}

				set.Add(current);
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
