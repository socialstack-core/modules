using Api.Database;
using Api.SocketServerLibrary;
using Api.Contexts;
using System;
using System.Threading.Tasks;

namespace Api.Startup;


/// <summary>
/// Weakly typed VirtualFieldValueGenerator.
/// </summary>
public partial class VirtualFieldValueGenerator {

	/// <summary>
	/// Sets the AutoService object which will be of type AutoService{T,ID}.
	/// </summary>
	/// <param name="svc"></param>
	public virtual void SetService(AutoService svc)
	{
	}

}

/// <summary>
/// Inherit this to define a virtual field value generator.
/// Value generators let you define custom code for an includable name.
/// primaryUrl is an example of a value generator (it's in the Pages module).
/// The name of the includable field simply originates from the name of the class; 
/// it's your class name, minus ValueGenerator (if it ends with it), then camelCase'd.
/// A usage template is available at the bottom of this file.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="ID"></typeparam>
public partial class VirtualFieldValueGenerator<T, ID>
	: VirtualFieldValueGenerator
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{

	/// <summary>
	/// The service for the content object.
	/// </summary>
	public AutoService<T, ID> Service;

	/// <summary>
	/// Sets the AutoService object which will be of type AutoService{T,ID}.
	/// </summary>
	/// <param name="svc"></param>
	public override void SetService(AutoService svc)
	{
		Service = svc as AutoService<T, ID>;
	}

	/// <summary>
	/// Generate the value.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="forObject"></param>
	/// <param name="writer">Must write the value into the given JSON writer. If you aren't outputting anything, you must use writer.WriteASCII("null");</param>
	/// <param name="flags">Flags provided by the serialiser which can be used for additional permission state checking.</param>
	/// <returns></returns>
	public virtual ValueTask GetValue(Context context, T forObject, Writer writer, ContextFlags flags)
	{
		writer.WriteASCII("null");
		return new ValueTask();
	}

	/// <summary>
	/// The type, if any, associated with the value being outputted.
	/// For example, if GetValue outputs only strings, this is typeof(string).
	/// </summary>
	/// <returns></returns>
	public virtual Type OutputType => typeof(object);

}

/*
Usage Template:

/// <summary>
/// Automatically instanced and the include field name is derived from the class name by the includes system. See VirtualFieldValueGenerator for more info.
/// </summary>
public partial class {IncludeFieldName}ValueGenerator<T, ID> : VirtualFieldValueGenerator<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	/// <summary>
	/// Generate the value.
	/// </summary>
	/// <param name="forObject"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public override ValueTask GetValue(T forObject, Writer writer)
	{
		writer.WriteASCII("null");
		return new ValueTask();
	}

}
 */

