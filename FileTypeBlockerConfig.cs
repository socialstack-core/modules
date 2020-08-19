using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.FileTypeBlocker
{
	/// <summary>
	/// The appsettings.json config block for the file type blocker.
	/// </summary>
    public class FileTypeBlockerConfig
    {
		/// <summary>
		/// Filetypes to reject uploading. If neither this or whitelist is specified then a default set is used instead.
		/// </summary>
		public string[] Blacklist {get; set;}
		
		/// <summary>
		/// Filetypes to only permit. If neither this or whitelist is specified then a default set is used instead.
		/// </summary>
		public string[] Whitelist {get; set;}
		
		/// <summary>
		/// True if it should use the whitelist technique, but with the default set.
		/// </summary>
		public bool UseWhitelist {get; set;}
	}
	
}
