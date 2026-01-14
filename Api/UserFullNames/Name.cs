using System;
using System.Collections.Generic;
using Api.AutoForms;
using Api.Database;
using Api.Permissions;


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
		[Permissions(Rule = "IsSelf()", Roles = "*,!admins")] /* Admins can always see the field */
		public string FirstName;

        /// <summary>
        /// The last name(s) of the user. 
        /// </summary>
        [DatabaseField(Length = 40)]
		[Permissions(Rule = "IsSelf()", Roles = "*,!admins")] /* Admins can always see the field */
		public string LastName;
		
		/// <summary>
		/// First + last concatted with a space.
        /// </summary>
        [DatabaseField(Length = 90)]
        [Data("hidden", true)] // It's writeable but hidden from the admin UI
		public string FullName;
    }
    
}
