namespace Api.Uploader;


/// <summary>
/// Used during file deletion.
/// </summary>
public struct FileDelete{
	
	/// <summary>
	/// The full path the file is at.
	/// </summary>
	public string Path;
	/// <summary>
	/// True if it's a private file.
	/// </summary>
	public bool IsPrivate;
	/// <summary>
	/// Set to true when any subsystem found and deleted the file.
	/// </summary>
	public bool Succeeded;
	/// <summary>
	/// Set to true when any subsystem attempted to delete the file.
	/// </summary>
	public bool Handled;
	
}