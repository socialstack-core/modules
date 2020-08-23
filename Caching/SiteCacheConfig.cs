using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Startup
{
	/// <summary>
	/// The appsettings.json config block for caching on services.
	/// </summary>
    public class SiteCacheConfig
    {
		/// <summary>
		/// Service caching configs. If a service has caching active by default, you can disable it via "ServiceName": {"Active": false}
		/// </summary>
		public Dictionary<string, CacheConfig> Services { get; set; }
	}
	
}
