using Api.Database;
using Api.Startup;
using System.Threading.Tasks;
using System;
using Api.SocketServerLibrary;
using Api.Contexts;

namespace Api.Uploader;

/// <summary>
/// A virtual field value generator for a field called "signedRef256".
/// You can include this field on an upload and it will generate the signed ref for you - typically a signed URL on a CDN - for the size "256" base file. 
/// Automatically instanced and the include field name is derived from the class name by the includes system. See VirtualFieldValueGenerator for more info.
/// </summary>
public partial class SignedRef256ValueGenerator<T, ID> : VirtualFieldValueGenerator<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{

	private UploadService _uploadService;
	
	/// <summary>
	/// Generate the value.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="forObject"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public override ValueTask GetValue(Context context, T forObject, Writer writer)
	{
		if (_uploadService == null)
		{
			_uploadService = Services.Get<UploadService>();
		}
		
		var upload = forObject as Upload;
		
		if(upload == null)
		{
			writer.WriteASCII("null");
			return new ValueTask();
		}
		
		var signedUrl = _uploadService.GetSignedRef(upload, "256");
		
		if (signedUrl == null)
		{
			writer.WriteASCII("null");
			return new ValueTask();
		}
		
		// Write the URL string:
		writer.WriteEscaped(signedUrl);
		return new ValueTask();
	}
	
	/// <summary>
	/// The type, if any, associated with the value being outputted.
	/// For example, if GetValue outputs only strings, this is typeof(string).
	/// </summary>
	/// <returns></returns>
	public override Type GetOutputType()
	{
		return typeof(string);
	}

}