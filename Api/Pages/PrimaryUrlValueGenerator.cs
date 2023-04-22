using Api.Database;
using Api.Startup;
using System.Threading.Tasks;
using System;
using Api.SocketServerLibrary;
using Api.Contexts;

namespace Api.Pages;


/// <summary>
/// A virtual field value generator for a field called "primaryUrl".
/// You can include this field on any type and it will provide the URL of the 
/// page where the object is the primary content. See also: Primary Content on the wiki.
/// 
/// Automatically instanced and the include field name is derived from the class name by the includes system. See VirtualFieldValueGenerator for more info.
/// </summary>
public partial class PrimaryUrlValueGenerator<T, ID> : VirtualFieldValueGenerator<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{

	private UrlGenerationMeta _genMeta;
	private bool _hadNoPrimaryPage;

	/// <summary>
	/// Generate the value.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="forObject"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public override async ValueTask GetValue(Context context, T forObject, Writer writer)
	{
		if (_genMeta == null)
		{
			if (_hadNoPrimaryPage)
			{
				writer.WriteASCII("null");
				return;
			}

			_genMeta = await Services.Get<PageService>()
				.GetUrlGenerationMeta(typeof(T), UrlGenerationScope.UI);

			if (_genMeta == null)
			{
				_hadNoPrimaryPage = true;
				writer.WriteASCII("null");
				return;
			}
		}

		// Generate the URL:
		var pageUrl = _genMeta.Generate(forObject);

		// Write the URL string:
		writer.WriteEscaped(pageUrl);
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