using System;
using System.Threading.Tasks;

namespace Api.Uploader;


/// <summary>
/// A stream of information about present uploaded files.
/// </summary>
public partial class FileMetaStream
{
	/// <summary>
	/// Set to true if this stream should be cancelled.
	/// </summary>
	public bool Cancelled;
	
	/// <summary>
	/// True when a filesystem of some kind has handled this stream request.
	/// </summary>
	public bool Handled;

	/// <summary>
	/// The total number of files listed.
	/// </summary>
	public int FilesListed;

	/// <summary>
	/// True if this stream will search the private area or the public one.
	/// </summary>
	public bool SearchPrivate;

	/// <summary>
	/// The directory to start from, if any. Must not contain /content/ or /content-private/ - that is handled by SearchPrivate.
	/// </summary>
	public string SearchDirectory;

	/// <summary>
	/// Last modified date of the current file. Valid during the OnFile callback.
	/// </summary>
	public DateTime LastModifiedUtc;

	/// <summary>
	/// Current file path, including /content/ or /content-private/. Valid during the OnFile callback.
	/// </summary>
	public string Path;
	
	/// <summary>
	/// The file size.
	/// </summary>
	public ulong FileSize;
	
	/// <summary>
	/// True if it's a directory.
	/// </summary>
	public bool IsDirectory;
	
	/// <summary>
	/// Optionally async callback which occurs when a file has been discovered.
	/// Use fields on this FileMetaStream itself for information about the file.
	/// </summary>
	public Func<FileMetaStream, ValueTask> OnFile;
}