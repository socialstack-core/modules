using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Api.Startup.Routing;


/// <summary>
/// File contents with a specified mime type.
/// </summary>
public struct FileContent {
	
	/// <summary>
	/// The file mime type.
	/// </summary>
	public string MimeType;
	
	/// <summary>
	/// The raw bytes.
	/// </summary>
	public byte[] RawBytes;
	
	/// <summary>
	/// Optional last modified date.
	/// </summary>
	public string LastModifiedUtc;
	
	/// <summary>
	/// Optional e-tag.
	/// </summary>
	public string Etag;
	
	/// <summary>
	/// True if it is pre-compressed.
	/// </summary>
	public bool IsCompressed;
	
	/// <summary>
	/// Set the content-disposition attachment filename.
	/// </summary>
	public string FileName;
	
	/// <summary>
	/// Create a new fileContent struct.
	/// </summary>
	/// <param name="rawBytes"></param>
	/// <param name="mimeType"></param>
	/// <param name="fileName"></param>
	/// <param name="isCompressed"></param>
	/// <param name="lastModded">
	/// Generally use LastModifiedUtcString if you're dealing with a cached file. 
	/// Otherwise, use a UTC DateTime .ToString("R") for the correct format.
	/// As always, if it doesn't change frequently, you should cache that string in memory.
	/// </param>
	/// <param name="etag"></param>
	public FileContent(byte[] rawBytes, string mimeType, string fileName = null, bool isCompressed = false, string lastModded = null, string etag = null)
	{
		RawBytes = rawBytes;
		MimeType = mimeType;
		IsCompressed = isCompressed;
		LastModifiedUtc = lastModded;
		Etag = etag;
		FileName = fileName;
	}

	/// <summary>
	/// Sends this file content as a HttpResonse.
	/// </summary>
	/// <param name="response"></param>
	/// <returns></returns>
	public async ValueTask SendAsResponse(HttpResponse response)
	{
		response.ContentType = MimeType;

		if (IsCompressed)
		{
			response.Headers["Content-Encoding"] = "gzip";
		}

		if (!string.IsNullOrEmpty(FileName))
		{
			response.Headers.ContentDisposition = "attachment; filename=" + FileName;
		}

		if (Etag != null)
		{
			response.Headers.ETag = Etag;
		}

		if (LastModifiedUtc != null)
		{
			response.Headers.LastModified = LastModifiedUtc;
		}

		if (RawBytes != null)
		{
			// Write the bytes:
			await response.Body.WriteAsync(RawBytes);
		}
	}


}