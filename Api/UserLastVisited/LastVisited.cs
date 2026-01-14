using System;
using System.Collections.Generic;
using Api.Database;

namespace Api.Users
{
    /// <summary>
    /// A particular user account.
    /// </summary>
    public partial class User
    {
        /// <summary>
        /// The UTC date this user last visited.
        /// </summary>
        public DateTime LastVisitedUtc;
    }
}
