using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Comments
{
	
	/// <summary>
	/// A CommentSet
	/// </summary>
	public partial class CommentSet : Content<int>
	{
		/// <summary>
		/// The sum of direct comments on this content.
		/// </summary>
		public int CommentCount;
		
		/// <summary>
		/// The sum of every comment incl. nested ones.
		/// </summary>
		public int NestedCommentCount;
		
		/// <summary>
		/// The content this comment is on.
		/// </summary>
		public int ContentId;
		
		/// <summary>
		/// The content type.
		/// </summary>
		public int ContentTypeId;
	}

}