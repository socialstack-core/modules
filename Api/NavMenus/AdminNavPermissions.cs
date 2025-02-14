using Newtonsoft.Json;
using System.Collections.Generic;

namespace Api.NavMenus
{
    /// <summary>
    /// A structure defining the available permission rules on a navigation menu item.
    /// </summary>
    public struct AdminNavPermissions
    {
        /// <summary>
        /// The list of required permissions, the user will need all of these permissions.
        /// </summary>
        public List<string> RequiredPermissions { get; set; }

        
    }
}