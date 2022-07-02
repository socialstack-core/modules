using System;
using Api.Database;
using Api.Users;

namespace Api.ForumReplies
{
	
	/// <summary>
	/// A forum reply.
	/// </summary>
	public partial class ForumReply : RevisionRow
	{
		/// <summary>
		/// The thread this reply is part of.
		/// </summary>
		public int ThreadId;
		/// <summary>
		/// The forum this reply is in.
		/// </summary>
		public int ForumId;
		/// <summary>
		/// If forum replies can nest, this is 
		/// used to indicate the sort order of the thread as a whole.
		/// </summary>
		[DatabaseField(Length = 20)]
		public byte[] Order;
		/// <summary>
		/// The JSON body of this reply. It's JSON because it is a *canvas*. 
		/// This means the reply can easily include other components such as polls etc 
		/// and be formatted in complex ways.
		/// </summary>
		// [DatabaseField(Length = 8000)]
		public string BodyJson;
		/// <summary>
		/// The creator user of the thread that this reply is in. 
		/// Stored in the replies as it doesn't change and improves the efficiency of the permission system.
		/// </summary>
		public int ThreadCreatorId;
	}
	
}