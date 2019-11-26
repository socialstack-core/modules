using Api.StoryAttachments;
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
		/// Just before a new story attachment is created. The given story attachment won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<StoryAttachment> StoryAttachmentBeforeCreate;

		/// <summary>
		/// Just after an story attachment has been created. The given story attachment object will now have an ID.
		/// </summary>
		public static EventHandler<StoryAttachment> StoryAttachmentAfterCreate;

		/// <summary>
		/// Just before an story attachment is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<StoryAttachment> StoryAttachmentBeforeDelete;

		/// <summary>
		/// Just after an story attachment has been deleted.
		/// </summary>
		public static EventHandler<StoryAttachment> StoryAttachmentAfterDelete;

		/// <summary>
		/// Just before updating an story attachment. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<StoryAttachment> StoryAttachmentBeforeUpdate;

		/// <summary>
		/// Just after updating an story attachment.
		/// </summary>
		public static EventHandler<StoryAttachment> StoryAttachmentAfterUpdate;

		/// <summary>
		/// Just after an story attachment was loaded.
		/// </summary>
		public static EventHandler<StoryAttachment> StoryAttachmentAfterLoad;

		/// <summary>
		/// Just before a service loads an story attachment list.
		/// </summary>
		public static EventHandler<Filter<StoryAttachment>> StoryAttachmentBeforeList;

		/// <summary>
		/// Just after an story attachment list was loaded.
		/// </summary>
		public static EventHandler<List<StoryAttachment>> StoryAttachmentAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new story attachment.
		/// </summary>
		public static EndpointEventHandler<StoryAttachmentAutoForm> StoryAttachmentCreate;
		/// <summary>
		/// Delete an story attachment.
		/// </summary>
		public static EndpointEventHandler<StoryAttachment> StoryAttachmentDelete;
		/// <summary>
		/// Update story attachment metadata.
		/// </summary>
		public static EndpointEventHandler<StoryAttachmentAutoForm> StoryAttachmentUpdate;
		/// <summary>
		/// Load story attachment metadata.
		/// </summary>
		public static EndpointEventHandler<StoryAttachment> StoryAttachmentLoad;
		/// <summary>
		/// List story attachments.
		/// </summary>
		public static EndpointEventHandler<Filter<StoryAttachment>> StoryAttachmentList;

		#endregion

	}

}
