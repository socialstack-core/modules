using Api.CanvasRenderer;
using Api.Startup;


namespace Api.Uploader;


public partial class Upload
{
	/// <summary>
	/// Gets the absolute URL of this upload (unsigned).
	/// </summary>
	/// <returns></returns>
	public string GetUrl(string variant = "original", uint localeId = 1)
	{
		return Services.Get<FrontendCodeService>().GetContentUrl(localeId) + (IsPrivate ? "/content-private/" : "/content/") + GetRelativePath(variant);
	}
	
	/// <summary>
	/// Gets a transcode callback URL. This allows trustless file manipulation.
	/// </summary>
	/// <returns></returns>
	public string GetTranscodeCallbackUrl(uint localeId = 1)
	{
		return Services.Get<FrontendCodeService>().GetPublicUrl(localeId) + "/v1/upload/transcoded/" + Id + "?token=" + Services.Get<UploadService>().GetTranscodeToken(Id);
	}

}