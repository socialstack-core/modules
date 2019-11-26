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

		#region Service events

		/// <summary>
		/// Just before a new comment is created. The given comment won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Comment> CommentBeforeCreate;

		/// <summary>
		/// Just after an comment has been created. The given comment object will now have an ID.
		/// </summary>
		public static EventHandler<Comment> CommentAfterCreate;

		/// <summary>
		/// Just before an comment is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Comment> CommentBeforeDelete;

		/// <summary>
		/// Just after an comment has been deleted.
		/// </summary>
		public static EventHandler<Comment> CommentAfterDelete;

		/// <summary>
		/// Just before updating an comment. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Comment> CommentBeforeUpdate;

		/// <summary>
		/// Just after updating an comment.
		/// </summary>
		public static EventHandler<Comment> CommentAfterUpdate;

		/// <summary>
		/// Just after an comment was loaded.
		/// </summary>
		public static EventHandler<Comment> CommentAfterLoad;

		/// <summary>
		/// Just before a service loads an comment list.
		/// </summary>
		public static EventHandler<Filter<Comment>> CommentBeforeList;

		/// <summary>
		/// Just after an comment list was loaded.
		/// </summary>
		public static EventHandler<List<Comment>> CommentAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new comment.
		/// </summary>
		public static EndpointEventHandler<CommentAutoForm> CommentCreate;
		/// <summary>
		/// Delete an comment.
		/// </summary>
		public static EndpointEventHandler<Comment> CommentDelete;
		/// <summary>
		/// Update comment metadata.
		/// </summary>
		public static EndpointEventHandler<CommentAutoForm> CommentUpdate;
		/// <summary>
		/// Load comment metadata.
		/// </summary>
		public static EndpointEventHandler<Comment> CommentLoad;
		/// <summary>
		/// List comments.
		/// </summary>
		public static EndpointEventHandler<Filter<Comment>> CommentList;

		#endregion

	}

}
