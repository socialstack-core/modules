namespace Api.Uploader;

/// <summary>
/// Info for a particular file.
/// </summary>
public struct FileConsistencyInfo
{
	/// <summary>
	/// File upload number.
	/// </summary>
	public ulong Number;
	
	/// <summary>
	/// Complete original path. Can contain \ or /
	/// </summary>
	public string Path;

	/// <summary>
	/// Offset to the dash in the filename, just before the variant.
	/// </summary>
	public int DashOffset;

	/// <summary>
	/// Offset to the first / in the filename, just before the name.
	/// </summary>
	public int DirectoryOffset;

	/// <summary>
	/// The type of the file.
	/// </summary>
	public string FileType;

	/// <summary>
	/// The name of the file.
	/// </summary>
	public string FileName;
	
	/// <summary>
	/// The variant of the file.
	/// </summary>
	public string Variant;

	/// <summary>
	/// The subdirectory.
	/// </summary>
	public string Subdirectory => DirectoryOffset == -1 ? null : Path.Substring(0, DirectoryOffset);
}