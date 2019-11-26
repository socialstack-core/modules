using Api.ForumReplies;
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
		/// Just before a new forum reply is created. The given forum reply won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<ForumReply> ForumReplyBeforeCreate;

		/// <summary>
		/// Just after an forum reply has been created. The given forum reply object will now have an ID.
		/// </summary>
		public static EventHandler<ForumReply> ForumReplyAfterCreate;

		/// <summary>
		/// Just before an forum reply is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<ForumReply> ForumReplyBeforeDelete;

		/// <summary>
		/// Just after an forum reply has been deleted.
		/// </summary>
		public static EventHandler<ForumReply> ForumReplyAfterDelete;

		/// <summary>
		/// Just before updating an forum reply. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<ForumReply> ForumReplyBeforeUpdate;

		/// <summary>
		/// Just after updating an forum reply.
		/// </summary>
		public static EventHandler<ForumReply> ForumReplyAfterUpdate;

		/// <summary>
		/// Just after an forum reply was loaded.
		/// </summary>
		public static EventHandler<ForumReply> ForumReplyAfterLoad;

		/// <summary>
		/// Just before a service loads an forumReply list.
		/// </summary>
		public static EventHandler<Filter<ForumReply>> ForumReplyBeforeList;

		/// <summary>
		/// Just after an forumReply list was loaded.
		/// </summary>
		public static EventHandler<List<ForumReply>> ForumReplyAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new forum reply.
		/// </summary>
		public static EndpointEventHandler<ForumReplyAutoForm> ForumReplyCreate;
		/// <summary>
		/// Delete an forum reply.
		/// </summary>
		public static EndpointEventHandler<ForumReply> ForumReplyDelete;
		/// <summary>
		/// Update forum reply metadata.
		/// </summary>
		public static EndpointEventHandler<ForumReplyAutoForm> ForumReplyUpdate;
		/// <summary>
		/// Load forum reply metadata.
		/// </summary>
		public static EndpointEventHandler<ForumReply> ForumReplyLoad;
		/// <summary>
		/// List forum replys.
		/// </summary>
		public static EndpointEventHandler<Filter<ForumReply>> ForumReplyList;

		#endregion

	}

}
