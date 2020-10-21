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
    public class ContentSyncConfig
    {
		/// <summary>
		/// Minimum ID. Assigned IDs must never be any less than this.
		/// </summary>
		public int Offset { get; set; }
		
		/// <summary>
		/// Verbose messaging mode
		/// </summary>
		public bool Verbose {get; set;}
		
		/// <summary>
		/// The username to stripe range config.
		/// </summary>
		public Dictionary<string, List<StripeRange>> Users { get; set; }

		/// <summary>
		/// True if the sync file should be explicitly enabled/ disabled.
		/// </summary>
		public bool? SyncFileMode { get; set; }
	}
	
}
