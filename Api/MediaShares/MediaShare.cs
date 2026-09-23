using Api.Database;
using Api.Permissions;
using System;
using Api.Translate;
using Api.Users;


namespace Api.MediaShares
{
	
	/// <summary>
	/// A MediaShare
	/// </summary>
	public partial class MediaShare : VersionLinkedContent<uint>
	{
		/// <summary>
		/// The date this share expires. Null means it never expires. Defaults to 7 days after creation.
		/// </summary>
		[Permissions(WriteRule = "false", Roles = "!admins")]
		public DateTime? ExpiryUtc;
		
		/// <summary>
		/// The number of times the files in this share have been downloaded.
		/// </summary>
		[Permissions(WriteRule = "false", Roles = "!admins")]
		public long DownloadCount;
	}
	
}