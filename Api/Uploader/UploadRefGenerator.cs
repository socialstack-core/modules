namespace Api.Uploader;


/// <summary>
/// A generator used to produce a ref for an upload.
/// </summary>
public class UploadRefGenerator
{
	/// <summary>
	/// Gets a signed ref (typically a URL) for the given upload.
	/// </summary>
	public virtual string GetSignedRef(Upload upload, string sizeName = "original")
	{
		return null;
	}
}