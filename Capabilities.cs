using System.Collections.Generic;


namespace Api.Permissions
{
	/// <summary>
	/// This permissions system is roles/ capabilities based:
	/// * Users have one role.
	/// * A role is defined by a set of capabilities granted to it.
	/// * Functionality checks to see if a user has a particular capability
	/// 
	/// Capabilities are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public static partial class Capabilities
    {
        /// <summary>
        /// A lookup of lowercase capability token -> capability. 
        /// Typically used during init of roles only. Populated by capability constructor.
        /// </summary>
        public static Dictionary<string, Capability> All;
	}
}
