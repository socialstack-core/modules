
using Api.Configuration;

namespace Api.AnonymousUsers
{
	/// <summary>
	/// The database config block for anonymous user config.
	/// </summary>
	public class AnonymousUserConfig : Config
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

		/// <summary>
		/// List of user agents to ignore. So for example from monitoring services so that they dont create users.
		/// </summary>
		public string[] IgnoreUserAgents { get; set; }
	}
	
}
