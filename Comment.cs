using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;

namespace Api.Comments
{

	/// <summary>
	/// A comment on some particular content.
	/// </summary>
	public partial class Comment : RevisionRow, IHaveReactions
	{
		/// <summary>
		/// The content this comment is on.
		/// </summary>
		public int ContentId;
		/// <summary>
		/// The content type.
		/// </summary>
		public int ContentTypeId;
		/// <summary>
		/// If the comment is a reply to some other comment, then this is the parent comment Id.
		/// The Order value is also set based on this being present.
		/// </summary>
		public int? ParentCommentId;
		/// <summary>
		/// If comments can nest, this is 
		/// used to indicate the sort order of the thread as a whole.
		/// </summary>
		[DatabaseField(Length = 20)]
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
	}

}