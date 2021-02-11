using Api.Comments;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
    {
		/// <summary>
		/// Set of events for a Comment.
		/// </summary>
		public static EventGroup<Comment> Comment;
		
		
		/// <summary>
		/// Set of events for a commentSet.
		/// </summary>
		public static EventGroup<CommentSet> CommentSet;
	}
}