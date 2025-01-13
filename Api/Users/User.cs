using System;
using System.Collections.Generic;
using Api.AutoForms;
using Api.Database;
using Api.Permissions;
using Api.Startup;

namespace Api.Users
{
	/// <summary>
	/// A particular user account.
	/// </summary>
	[Permissions(HideFieldByDefault = true)]
	[HasVirtualField("userRole", typeof(Role), "role")]
	public partial class User : VersionedContent<uint>
	{
		/// <summary>
		/// The user's login revoke count. An incrementing number used to revoke login tokens.
		/// </summary>
		[Newtonsoft.Json.JsonIgnore]
		public uint LoginRevokeCount;

		/// <summary>
		/// The user's main role.
		/// </summary>
		[Module("Admin/ContentSelect")]
		[Data("contentType", "role")]
		[Permissions(HideFieldByDefault = false)]
		public uint Role;
		
		/// <summary>
		/// Private server only data used to verify this user, e.g. during registration.
		/// </summary>
		[Newtonsoft.Json.JsonIgnore]
		public long PrivateVerify;
		
		/// <summary>
		/// The feature image ref (optionally used on their profile page). See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 300)]
		[Permissions(HideFieldByDefault = false)]
		public string FeatureRef;

		/// <summary>
		/// The avatar upload ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 300)]
		[Permissions(HideFieldByDefault = false)]
		public string AvatarRef;

		/// <summary>
		/// The username of the user. 
		/// </summary>
		[DatabaseField(Length = 40)]
		[Permissions(HideFieldByDefault = false)]
		public string Username;

		/// <summary>
		/// The latest locale this user used. Primarily, this is used for emails being sent to them. If it's null or 0, the site default, 1, is assumed.
		/// </summary>
		public uint? LocaleId;

		/// <summary>
		/// The UTC date this user was created.
		/// </summary>
		[Obsolete("Use CreatedUtc instead")]
		public DateTime JoinedUtc
		{
			get {
				return CreatedUtc;
			}
		}

	}

}
