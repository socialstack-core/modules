using Api.Database;
using Api.Startup;
using System.Threading.Tasks;
using System;
using Api.SocketServerLibrary;
using Api.Contexts;
using Api.Startup.Routing;
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
	/// <summary>
	/// Generate the value.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="forObject"></param>
	/// <param name="writer"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public override ValueTask GetValue(Context context, T forObject, Writer writer, ContextFlags flags)
	{
		var url = Service.GetPrimaryUrl(context, forObject);

		if (url == null)
		{
			writer.WriteASCII("null");
			return new ValueTask();
		}

		// Write the URL string:
		writer.WriteEscaped(url);

		return new ValueTask();
	}

	/// <summary>
	/// The type, if any, associated with the value being outputted.
	/// For example, if GetValue outputs only strings, this is typeof(string).
	/// </summary>
	/// <returns></returns>
	public override Type OutputType => typeof(string);
}