using System;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.PublishGroups
{

	/// <summary>
	/// Content within a publish group.
	/// </summary>
	// [HasVirtualField("content", "ContentType", "RevisionId")] Can't do this - RevisionId is a revision ID.
	public partial class PublishGroupContent : Content<uint>
	{
		/// <summary>
		/// The type ID of the content. See also: Api.Database.ContentTypes
		/// </summary>
		public string ContentType;
		/// <summary>
		/// The ID of the revision record that would get published.
		/// </summary>
		public ulong RevisionId;
		/// <summary>
		/// The ID of the publish group that this belongs to.
		/// </summary>
		public uint PublishGroupId;
	}

}
