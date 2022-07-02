using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Presence
{
	/// <summary>
	/// The appsettings.json config block for page presence records.
	/// </summary>
    public class PagePresenceRecordConfig
    {
		/// <summary>
		/// True if per page presence records are active.
		/// </summary>
		public bool Active {get;set;}
	}
	
}
