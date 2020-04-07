using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Users;

namespace Api.Permissions
{
    /// <summary>
    /// The requested resource is not accessible
    /// </summary>
    public class PermissionException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="capability"></param>
        /// <param name="user"></param>
        public PermissionException(string capability, User user):base($"The user {user?.Username} has no access to {capability}")
        {

        }

    }
}
