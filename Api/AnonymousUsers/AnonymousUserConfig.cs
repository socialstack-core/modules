using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.AnonymousUsers
{
	/// <summary>
	/// The appsettings.json config block for anon users.
	/// </summary>
    public class AnonymousUserConfig
    {
		/// <summary>
		/// Set this to true to disable the module without needing to uninstall it.
		/// </summary>
		public bool Disabled { get; set; }
		
		/// <summary>
		/// Account role to use. Default is the guest role (3).
		/// </summary>
		public string Role {get; set;}  = "guest";
		
		/// <summary>
		/// Random name will be selected from this. If not provided, default set is used.
		/// </summary>
		public string[] FirstNames { get; set; }
		
		/// <summary>
		/// Random name will be selected from this. If not provided, default set is used.
		/// </summary>
		public string[] LastNames { get; set; }
	}
	
}
