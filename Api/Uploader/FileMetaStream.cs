using System;
using System.Collections.Generic;
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
	/// True if AllFiles should be populated.
	/// </summary>
	public bool RetainAll;

	/// <summary>
	/// Populated only if RetainAll is true.
	/// </summary>
	public List<FileMeta> AllFiles;

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
	/// Reused meta object for the current file, unless RetainAll is true.
	/// </summary>
	public FileMeta Current;

	/// <summary>
	/// Optionally async callback which occurs when a file has been discovered.
	/// Use fields on this FileMetaStream itself for information about the file.
	/// </summary>
	public Func<FileMetaStream, ValueTask> OnFile;

	/// <summary>
	/// Sorts the files alphabetically. RetainAll must be true for this to work.
	/// </summary>
	/// <param name="asc">A->Z if true (default)</param>
	public void SortAlphabetically(bool asc = true)
	{
		if (!RetainAll)
		{
			throw new InvalidOperationException("Incorrect usage of SortAlphabetically: you need to ListFiles with the retainAll mode set to true in order to sort them.");
		}

		if (AllFiles == null)
		{
			return;
		}

		if (asc)
		{
			AllFiles.Sort((FileMeta a, FileMeta b) =>
			{
				return a.Path.CompareTo(b.Path);
			});
		}
		else
		{
			AllFiles.Sort((FileMeta a, FileMeta b) =>
			{
				return -a.Path.CompareTo(b.Path);
			});
		}
	}

	/// <summary>
	/// Sorts the files by modified date. RetainAll must be true for this to work.
	/// </summary>
	/// <param name="asc">Oldest -> Newest (default)</param>
	public void SortByModified(bool asc = true)
	{
		if (!RetainAll)
		{
			throw new InvalidOperationException("Incorrect usage of SortByModified: you need to ListFiles with the retainAll mode set to true in order to sort them.");
		}

		if (AllFiles == null)
		{
			return;
		}

		if (asc)
		{
			AllFiles.Sort((FileMeta a, FileMeta b) =>
			{
				return a.LastModifiedUtc.CompareTo(b.LastModifiedUtc);
			});
		}
		else
		{
			AllFiles.Sort((FileMeta a, FileMeta b) =>
			{
				return -a.LastModifiedUtc.CompareTo(b.LastModifiedUtc);
			});
		}
	}

	/// <summary>
	/// Start indicating a file is in progress.
	/// </summary>
	/// <returns></returns>
	public FileMeta StartFile()
	{
		if (Current == null)
		{
			// Current is reusable if RetainAll is false (the default).
			Current = new FileMeta();
		}

		return Current;
	}

	/// <summary>
	/// Indicates the given meta has been listed.
	/// </summary>
	/// <param name="fm"></param>
	/// <returns></returns>
	public async ValueTask FileListed(FileMeta fm)
	{
		Current = fm; // Should always no-op but just in case.
		FilesListed++;

		if (OnFile != null)
		{
			await OnFile(this);
		}

		if (RetainAll)
		{
			if (AllFiles == null)
			{
				AllFiles = new List<FileMeta>();
			}

			AllFiles.Add(fm);
			Current = null; // Such that the next call to StartFile instances a new object.
		}
	}
}

/// <summary>
/// Meta for a single file.
/// </summary>
public partial class FileMeta
{

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

}