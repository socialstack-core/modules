
using Api.Configuration;

namespace Api.UserLastVisited
{
	/// <summary>
	/// The database config block for user last visited config.
	/// </summary>
	public class UserLastVisitedConfig : Config
	{
		/// <summary>
		/// Set this to true to disable the module without needing to uninstall it.
		/// </summary>
		public bool Disabled { get; set; }
	
	}
	
}
