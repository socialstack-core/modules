using System;
using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;

namespace Api.Configuration
{
	
	/// <summary>
	/// A Configuration entry in the database. Do not derive from this! 
	/// Use :Config instead when declaring the config options for your thing.
	/// </summary>
	[DatabaseIndex(false, "Key")]
	public sealed partial class Configuration : VersionedContent<uint>
	{
        /// <summary>
        /// The name of the configuration. This is often the name of the service.
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Name;

		/// <summary>
		/// If this is set, this configuration applies to the specified comma separated environments. It is completely ignored otherwise.
		/// Valid environments are typically "dev", "stage" and "prod" (but it ultimately derives from whatever is in your appsettings.json "Environment" field).
		/// </summary>
		[DatabaseField(Length = 100)]
		public string Environments;

		/// <summary>
        /// The key that things use to identify this config. Usually the same as the English name but without spaces.
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Key;

		/// <summary>
		/// The raw content of this configuration.
		/// </summary>
		[Module("UI/Input")]
		[Data("contentType", "application/json")]
		public string ConfigJson;
	}

}