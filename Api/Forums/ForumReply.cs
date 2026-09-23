using System;
using Api.Database;
using Api.Users;

namespace Api.Forums
{
	
	/// <summary>
	/// A forum reply.
	/// </summary>
	public partial class ForumReply : VersionedContent<uint>
	{
		/// <summary>
		/// The thread this reply is part of.
		/// </summary>
		public uint ThreadId;

		/// <summary>
		/// The forum this reply is in.
		/// </summary>
		public uint ForumId;

		/// <summary>
		/// If forum replies can nest, this is 
		/// used to indicate the sort order of the thread as a whole.
		/// </summary>
		[DatabaseField(Length = 20)]
		public byte[] Order;

		/// <summary>
		/// The text body of this reply, optionally in markdown.
		/// </summary>
		// [DatabaseField(Length = 8000)]
		public string BodyText;
	}
	
}