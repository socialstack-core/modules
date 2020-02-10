using Api.Uploader;
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
		/// Just before a new upload is created. The given upload won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Upload> UploadBeforeCreate;

		/// <summary>
		/// Just after a upload has been created. The given upload object will now have an ID.
		/// </summary>
		public static EventHandler<Upload> UploadAfterCreate;

		/// <summary>
		/// Just before a upload is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Upload> UploadBeforeDelete;

		/// <summary>
		/// Just after a upload has been deleted.
		/// </summary>
		public static EventHandler<Upload> UploadAfterDelete;

		/// <summary>
		/// Just before updating a upload. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Upload> UploadBeforeUpdate;

		/// <summary>
		/// Just after updating a upload.
		/// </summary>
		public static EventHandler<Upload> UploadAfterUpdate;

		/// <summary>
		/// Just after a upload was loaded.
		/// </summary>
		public static EventHandler<Upload> UploadAfterLoad;

		/// <summary>
		/// Just before a service loads a upload list.
		/// </summary>
		public static EventHandler<Filter<Upload>> UploadBeforeList;

		/// <summary>
		/// Just after a upload list was loaded.
		/// </summary>
		public static EventHandler<List<Upload>> UploadAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new upload.
		/// </summary>
		public static EndpointEventHandler<FileUploadBody> UploadCreate;
		/// <summary>
		/// Delete a upload.
		/// </summary>
		public static EndpointEventHandler<Upload> UploadDelete;
		/// <summary>
		/// Update upload metadata.
		/// </summary>
		public static EndpointEventHandler<UploadAutoForm> UploadUpdate;
		/// <summary>
		/// Load upload metadata.
		/// </summary>
		public static EndpointEventHandler<Upload> UploadLoad;
		/// <summary>
		/// List uploads.
		/// </summary>
		public static EndpointEventHandler<Filter<Upload>> UploadList;

		#endregion

	}

}
