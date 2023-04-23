using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Api.ColourConsole;

namespace Api.Configuration
{
	
	/// <summary>
	/// This is the global config. It originates from the appsettings.json file.
	/// </summary>
	public static partial class AppSettings
	{
		/// <summary>
		/// The config from the appsettings.json file.
		/// </summary>
		public static readonly AppSettingsFile Configuration;
		
		/// <summary>
		/// A heavily used database table prefix.
		/// </summary>
		public static string DatabaseTablePrefix;

		/// <summary>
		/// A handler called whenever the appsettings changes.
		/// </summary>
		public static event Action OnChange;


		static AppSettings()
		{
			// ConfigurationBuilder has a reloadOnChange option which doesn't work as expected
			// So we'll instead use a simpler custom implementation such that we can get realtime reloads

			Configuration = LoadFromJsonFile("appsettings.extension.json");
			Configuration.Parent = LoadFromJsonFile("appsettings.json");

			// Database table prefix:
			DatabaseTablePrefix = GetString("DatabaseTablePrefix", "site_");

			// Db prefix updater:
			OnChange += () => {

				// Database table prefix:
				DatabaseTablePrefix = GetString("DatabaseTablePrefix", "site_");

			};

		}

		private static AppSettingsFile LoadFromJsonFile(string path)
		{
			var fullPath = Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), path);

			return new AppSettingsFile(fullPath);
		}

		private static string[] _publicUrlByLocaleId;

		/// <summary>
		/// The site public URL for a particular locale ID. If you're not sure what the localeId should be, use locale 1.
		/// Note that if an origin does not exist for a localeId, the URL for locale 1 is used.
		/// If the appsettings does not specify a locale, 1 is assumed.
		/// The resulting URL never ends with a path - always just the origin and scheme, e.g. https://www.example.com
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		public static string GetPublicUrl(uint localeId)
		{
			if (_publicUrlByLocaleId == null)
			{
				// Load the public URLs now:
				var urlToken = Configuration.GetToken("PublicUrl");

				if (urlToken == null)
				{
					// No public URL is actually specified. Halt here.
					return null;
				}

				if (urlToken.Type == JTokenType.String)
				{
					// If it's just a string then it is read as locale 1.
					_publicUrlByLocaleId = new string[] {
						urlToken.ToString()
					};
				}
				else if (urlToken.Type == JTokenType.Array)
				{
					// An array of URLs each with a locale.
					var urlArray = urlToken as JArray;

					uint maxLocaleId = 0;
					var hasSpecifiedLocale1 = false;

					foreach (var entry in urlArray)
					{
						var jObj = entry as JObject;

						var entryLocaleId = jObj.Value<uint>("LocaleId");

						if (entryLocaleId > maxLocaleId)
						{
							maxLocaleId = entryLocaleId;
						}

						if (entryLocaleId == 1)
						{
							hasSpecifiedLocale1 = true;
						}
					}

					if (!hasSpecifiedLocale1)
					{
						throw new Exception("Invalid appsettings PublicUrl. It contains an array but does not specify which URL is for locale #1. A url for locale #1 is required.");
					}

					var set = new string[maxLocaleId];

					foreach (var entry in urlArray)
					{
						var jObj = entry as JObject;

						var entryLocaleId = jObj.Value<uint>("LocaleId");
						var url = jObj.Value<string>("Url");

						set[entryLocaleId - 1] = url;
					}

					// Fill any gaps in the set with the default one.
					var urlLocale1 = set[0];

					for (var i = 1; i < set.Length; i++)
					{
						if (set[i] == null)
						{
							set[i] = urlLocale1;
						}
					}

					_publicUrlByLocaleId = set;
				}
				else
				{
					// Not supported
					return null;
				}

			}

			if (localeId == 0 || localeId > _publicUrlByLocaleId.Length)
			{
				// Default:
				return _publicUrlByLocaleId[0];
			}

			// Due to the fill above and a requirement for a default, the set does not contain any gaps.
			return _publicUrlByLocaleId[localeId - 1];
		}
		
		/// <summary>
		/// Wrapper for old API format
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static AppSettingsFileSectionShim GetSection(string key)
		{
			return new AppSettingsFileSectionShim(Configuration, key);
		}

		/// <summary>
		/// Gets a string from the settings.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static string GetString(string name, string defaultValue = null)
		{
			var textValue = Configuration[name];

			return string.IsNullOrEmpty(textValue) ? defaultValue : textValue;
		}

		/// <summary>
		/// Triggers the change event
		/// </summary>
		public static void TriggerChange()
		{
			OnChange();
		}

		/// <summary>
		/// Reads a field from the appsettings.json with the given name as an int32.
		/// Returns the default value if not set or invalid.
		/// </summary>
		public static int GetInt32(string name, int defaultValue)
		{
			// Get the textual value:
			var textValue = Configuration[name];
			
			// Does it exist? If no, use the default.
			if(string.IsNullOrEmpty(textValue))
			{
				return defaultValue;
			}
			
			// Ok - try getting it as a number next.
			int parsedNumber;
			if (int.TryParse(textValue, out parsedNumber))
			{
				return parsedNumber;
			}
			
			// Failed to parse the value. Let them know but use the default in the meantime.
			WriteColourLine.Warning("[Warning]: Field '" + name + "' in appsettings.json isn't an int32 number. Using the default value " + defaultValue + " instead.");
			
			return defaultValue;
		}
		
		/// <summary>
		/// Reads a field from the appsettings.json with the given name as an int64.
		/// Returns the default value if not set or invalid.
		/// </summary>
		public static long GetInt64(string name, long defaultValue)
		{
			// Get the textual value:
			var textValue = Configuration[name];
			
			// Does it exist? If no, use the default.
			if(string.IsNullOrEmpty(textValue))
			{
				return defaultValue;
			}
			
			// Ok - try getting it as a number next.
			long parsedNumber;
			if (long.TryParse(textValue, out parsedNumber))
			{
				return parsedNumber;
			}
			
			// Failed to parse the value. Let them know but use the default in the meantime.
			WriteColourLine.Warning("[Warning]: Field '" + name + "' in appsettings.json isn't an int64 number. Using the default value " + defaultValue + " instead.");
			
			return defaultValue;
		}
		
	}

	/// <summary>
	/// Info about a singular appsettings file.
	/// </summary>
	public class AppSettingsFile
	{
		/// <summary>
		/// Path to the appsettings file.
		/// </summary>
		public string Path;

		/// <summary>
		/// Parent appsettings if there is one.
		/// </summary>
		public AppSettingsFile Parent;

		/// <summary>
		/// The raw config object.
		/// </summary>
		private JObject _rawConfig;

		/// <summary>
		/// Creates and initially loads the file. Adds a file change handler too.
		/// </summary>
		/// <param name="path"></param>
		public AppSettingsFile(string path)
		{
			Path = path;

			LoadFile();

			CreateFileWatcher();
		}

		private void LoadFile()
		{
			string json;
			try
			{
				// Load the json:
				json = File.ReadAllText(Path);
			}
			catch
			{
                WriteColourLine.Warning("[Notice] Config from '" + Path + "' wasn't loaded (usually because file not found)");

				// File not found.
				_rawConfig = null;
				return;
			}

			// Try parsing.
			try
			{
				_rawConfig = Newtonsoft.Json.JsonConvert.DeserializeObject(json) as JObject;
			}
			catch(Exception e)
			{
                WriteColourLine.Warning("[Warn] Failed to load configuration file '" + Path + "' due to JSON parse error: " + e.ToString());
			}

		}

		private FileSystemWatcher _watcher;

 		/// <summary>
		/// Create a file watcher
		/// </summary>
		private void CreateFileWatcher()
		{
			var absPath = System.IO.Path.GetFullPath(Path);

			// Watch the single file
			var watcher = new FileSystemWatcher();
			watcher.Path = System.IO.Path.GetDirectoryName(absPath);
			watcher.NotifyFilter = NotifyFilters.LastWrite;
			watcher.Filter = System.IO.Path.GetFileName(absPath);

			// Prevent gc
			_watcher = watcher;

			// Add event handlers
			watcher.Changed += new FileSystemEventHandler(OnChanged);
			watcher.Created += new FileSystemEventHandler(OnChanged);
			watcher.Deleted += new FileSystemEventHandler(OnChanged);

			// Begin watching
			watcher.EnableRaisingEvents = true;
		}

		private void OnChanged(object source, FileSystemEventArgs e)
		{
			// Wait a moment because the change often triggers before editors are actually done manipulating the file.
			Task.Run(async () => {

				await Task.Delay(200);

				Console.WriteLine("Reloading appsettings file at '" + Path + "' due to external change");

				LoadFile();

				// Invoke onchange:
				AppSettings.TriggerChange();

			});
		}
		
		/// <summary>
		/// Gets the JSON token for the given field.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public JToken GetToken(string field)
		{
			if (_rawConfig != null)
			{
				var result = _rawConfig[field];

				if (result != null)
				{
					return result;
				}
			}

			if (Parent != null)
			{
				return Parent.GetToken(field);
			}

			return null;
		}

		/// <summary>
		/// Gets the given config field as a string.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public string GetString(string field)
		{
			var token = GetToken(field);

			if (token == null)
			{
				return null;
			}

			return token.ToString();
		}

		/// <summary>
		/// Gets a section of the config by field name, constructing an object of the given type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="field"></param>
		/// <returns></returns>
		public T GetSection<T>(string field)
			where T : class
		{
			var token = GetToken(field) as JObject;

			if (token == null)
			{
				return null;
			}

			return token.ToObject<T>();
		}
		
		/// <summary>
		/// Shortcut for GetString
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public string this[string field]
		{
			get {
				return GetString(field);
			}
		}

	}

	/// <summary>
	/// Shims the IConfigurationSection interface
	/// </summary>
	public class AppSettingsFileSectionShim
	{
		private AppSettingsFile _file;
		private string _sectionName;


		/// <summary>
		/// Creates a file section
		/// </summary>
		/// <param name="file"></param>
		/// <param name="section"></param>
		public AppSettingsFileSectionShim(AppSettingsFile file, string section)
		{
			_file = file;
			_sectionName = section;
		}

		/// <summary>
		/// Gets the section cast as the given type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Get<T>()
			where T:class
		{
			return _file.GetSection<T>(_sectionName);
		}

		/// <summary>
		/// Shims GetSection("name")["fieldName"].
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public string this[string field]
		{
			get
			{
				var section = _file.GetToken(_sectionName) as JObject;

				if (section == null)
				{
					return null;
				}

				var token = section[field];

				if (token == null)
				{
					return null;
				}

				return token.ToString();
			}
		}

	}
}