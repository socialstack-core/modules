using System;
using System.Collections.Generic;
using Api.Database;


namespace Api.Users
{
    /// <summary>
    /// A typically public facing segment of a user account.
    /// </summary>
    public partial class UserProfile: IHaveId<uint>
    {
		/// <summary>
		/// Starts creating a public facing variant of the given user object.
		/// This constructor doesn't populate the fields though - use the UserService for that.
		/// </summary>
		/// <param name="user"></param>
		public UserProfile(User user) {
			User = user;
		}

		/// <summary>
		/// The full source user object.
		/// </summary>
		[Newtonsoft.Json.JsonIgnore]
		public User User;

		/// <summary>
		/// The user ID.
		/// </summary>
		public uint Id;

		/// <summary>
		/// The feature image ref (optionally used on their profile page). See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string FeatureRef;

		/// <summary>
		/// The avatar upload ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string AvatarRef;

        /// <summary>
        /// The username of the user. 
        /// </summary>
        public string Username;

		/// <summary>
		/// Gets the ID of this row.
		/// </summary>
		/// <returns></returns>
		public uint GetId()
		{
			return Id;
		}

		/// <summary>
		/// Sets the ID of this row.
		/// </summary>
		/// <returns></returns>
		public void SetId(uint id)
		{
			Id = id;
		}

		/// <summary>
		/// Type of this obj.
		/// </summary>
		public string Type{
			get{
				return "User";
			}
		}
	}

}
