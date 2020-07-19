using System;
using System.Collections.Generic;
using Api.Database;


namespace Api.Users
{
    /// <summary>
    /// A particular user account.
    /// </summary>
    public partial class User : DatabaseRow
    {
		/// <summary>
		/// The user's email address.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string Email;
		
		/// <summary>
		/// The user's login revoke count. An incrementing number used to revoke login tokens.
		/// </summary>
		[Newtonsoft.Json.JsonIgnore]
		public int LoginRevokeCount;

		/// <summary>
		/// The user's main role.
		/// </summary>
		public int Role;

		/// <summary>
		/// The UTC date this user was created.
		/// </summary>
		public DateTime JoinedUtc;

		/// <summary>
		/// The feature image ref (optionally used on their profile page). See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;

		/// <summary>
		/// The avatar upload ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string AvatarRef;

        /// <summary>
        /// The username of the user. 
        /// </summary>
        [DatabaseField(Length = 40)]
        public string Username;
		
		/// <summary>
		/// The latest locale this user used. Primarily, this is used for emails being sent to them. If it's null or 0, the site default, 1, is assumed.
		/// </summary>
		public int? LocaleId;
    }
    
}
