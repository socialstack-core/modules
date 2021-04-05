using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;
using Api.WebSockets;

namespace Api.Comments
{

	/// <summary>
	/// A comment on some particular content.
	/// </summary>
	public partial class Comment : VersionedContent<uint>, IHaveReactions, IAmLive
	{
		/// <summary>
		/// The content this comment is on.
		/// </summary>
		public uint ContentId;
		/// <summary>
		/// The content type.
		/// </summary>
		public int ContentTypeId;
		/// <summary>
		/// The comment set (server assigned).
		/// </summary>
		public uint CommentSetId;
		/// <summary>
		/// Number of child comments.
		/// </summary>
		public int ChildCommentCount;
		/// <summary>
		/// Number of soft deleted child comments.
		/// </summary>
		public int ChildCommentDeleteCount;
		/// <summary>
		/// If comments are nested, this is the depth the comment is at. Root comments are depth 0 with RootComment = true.
		/// </summary>
		public int Depth;
		/// <summary>
		/// This indicates true root comments. Every 10 depth ticks, depth is reset and this goes up 1. It exists to prevent extreme nesting without outright failure.
		/// </summary>
		public int DepthPage;
		/// <summary>
		/// This indicates the current root parent comment for this block of comments. Starts when comments are extreme nested.
		/// </summary>
		public uint RootParentCommentId;
		/// <summary>
		/// This is the xth child comment. Always set.
		/// </summary>
		public int ChildCommentNumber;
		/// <summary>
		/// If the comment is a reply to some other comment, then this is the parent comment Id.
		/// The Order value is also set based on this being present.
		/// </summary>
		public uint? ParentCommentId;
		/// <summary>
		/// If comments can nest, this is 
		/// used to indicate the sort order of the thread as a whole.
		/// </summary>
		[DatabaseField(Length = 30)]
		public byte[] Order;
		/// <summary>
		/// The JSON body of this answer. It's JSON because it is a *canvas*. 
		/// This means the answer can easily include other components such as polls etc 
		/// and be formatted in complex ways.
		/// </summary>
		// [DatabaseField(Length = 2000)]
		public string BodyJson;
		/// <summary>
		/// Reactions to this comment (typically upvote/ downvote).
		/// </summary>
		public List<ReactionCount> Reactions { get; set; }
		/// <summary>
		/// Since hierarchy matter, deletes need to be soft so keep the tree together.
		/// </summary>
		public bool Deleted;
	}

}