using Api.Uploader;
namespace Api.CloudHosts;


/// <summary>
/// Generates signed URLs for private files when cloudHosts is present.
/// </summary>
public class SignedUrlGenerator : UploadRefGenerator
{
	/// <summary>
	/// The current cloud host.
	/// </summary>
	public CloudHostPlatform Host;
	
	/// <summary>
	/// Gets a signed ref (typically a URL) for the given upload.
	/// </summary>
	public override string GetSignedRef(Upload upload, string sizeName = "original")
	{
		return Host.GetSignedRef(upload, sizeName);
	} 
}