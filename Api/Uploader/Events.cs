using Api.Uploader;
using Api.Permissions;
using System.Collections.Generic;
using System.IO;

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
		/// After a chunk group has been uploaded by a transcoder.
		/// </summary>
		public EventHandler<Upload> AfterChunksUploaded;

		/// <summary>
		/// Called when the upload system must store a file.
		/// Given the upload, the temp file path and the variant name.
		/// </summary>
		public EventHandler<Upload, string, string> StoreFile;

		/// <summary>
		/// Reads the file at the given storage relative path, and returns its byte[]. You can block future event handlers by returning an empty array of bytes.
		/// </summary>
		public EventHandler<byte[], string, bool> ReadFile;
		
		/// <summary>
		/// Reads the file at the given storage relative path, and returns it as a stream.
		/// </summary>
		public EventHandler<Stream, string, bool> OpenFile;

	}

}
