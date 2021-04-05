using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.ContentSync
{
	/// <summary>
	/// The appsettings.json config block for push notification config.
	/// </summary>
    public class ContentSyncServiceConfig
    {
		/// <summary>
		/// The port number for csync service to use.
		/// </summary>
		public int Port { get; set; } = 12020;

		/// <summary>
		/// Verbose messaging mode
		/// </summary>
		public bool Verbose {get; set;}
		
		/// <summary>
		/// True if the sync file should be explicitly enabled/ disabled.
		/// </summary>
		public bool? SyncFileMode { get; set; }
	}
	
}
