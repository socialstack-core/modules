using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PublishGroups
{
	
	/// <summary>
	/// Content within a publish group.
	/// </summary>
	public partial class PublishGroupContent : Content<uint>
	{
		/// <summary>
		/// The type ID of the content. See also: Api.Database.ContentTypes
		/// </summary>
		public string ContentType;
		/// <summary>
		/// The ID of the included content. Uses the latest draft of this content and publishes it (if there is one).
		/// </summary>
		public ulong ContentId;
		/// <summary>
		/// The ID of the publish group that this belongs to.
		/// </summary>
		public uint PublishGroupId;
	}

}
