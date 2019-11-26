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
        /// The first name of the user. 
        /// </summary>
        [DatabaseField(Length = 40)]
        public string FirstName;

        /// <summary>
        /// The last name of the user. 
        /// </summary>
        [DatabaseField(Length = 40)]
        public string LastName;
    }
    
}
