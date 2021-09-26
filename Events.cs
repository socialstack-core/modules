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
		/// <summary>
		/// Set of events for an Upload.
		/// </summary>
		public static UploadEventGroup Upload;
	}

	/// <summary>
	/// Upload specific events.
	/// </summary>
	public class UploadEventGroup : EventGroup<Upload>
	{
		/// <summary>
		/// Before the upload is done processing.
		/// </summary>
		public EventHandler<Upload> Process;

		/// <summary>
		/// Called when the upload system must store a file.
		/// Given the upload, the temp file path and the variant name.
		/// </summary>
		public EventHandler<Upload, string, string> StoreFile;

	}

}
