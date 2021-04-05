using System;
using Microsoft.Extensions.Configuration;


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
		public static readonly IConfigurationRoot Configuration;
		
		/// <summary>
		/// A heavily used database table prefix.
		/// </summary>
		public static readonly string DatabaseTablePrefix;
		
		
		static AppSettings()
		{
			var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings." + Environment.Name + ".json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
			
			// Database table prefix:
			DatabaseTablePrefix = Configuration["DatabaseTablePrefix"];
			
			if(string.IsNullOrEmpty(DatabaseTablePrefix)){
				DatabaseTablePrefix = "site_";
			}
		}
		
		/// <summary>
		/// Convenience shortcut for Configuration.GetSection.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static IConfigurationSection GetSection(string key)
		{
			return Configuration.GetSection(key);
		}

		/// <summary>
		/// Gets a string from the settings.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static string GetString(string name, string defaultValue)
		{
			var textValue = Configuration[name];

			return string.IsNullOrEmpty(textValue) ? defaultValue : textValue;
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
			Console.WriteLine("Warning: Field '" + name + "' in appsettings.json isn't an int32 number. Using the default value " + defaultValue + " instead.");
			
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
			Console.WriteLine("Warning: Field '" + name + "' in appsettings.json isn't an int64 number. Using the default value " + defaultValue + " instead.");
			
			return defaultValue;
		}
		
	}
}