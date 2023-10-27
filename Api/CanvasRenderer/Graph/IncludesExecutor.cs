using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Api.CanvasRenderer;

/// <summary>
/// An executor which also has some code for handling includes - both functional includes and regular listed includes.
/// </summary>
public class IncludesExecutor<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	/// <summary>
	/// The service this include is related to
	/// </summary>
	public AutoService<T, ID> _svc;
	/// <summary>
	/// The full fields
	/// </summary>
	protected ContentFields _fields;
	/// <summary>
	/// The include set this is part of
	/// </summary>
	public IncludeSet includeSet;
	/// <summary>
	/// JSON writer for this type.
	/// </summary>
	private TypeReaderWriter<T> jsonWriter;


	/// <summary>
	/// Any functional includes.
	/// </summary>
	public FunctionalInclusionNode[] functionalIncludes;


	/// <summary>
	/// An executor which runs includes.
	/// </summary>
	/// <param name="svc"></param>
	/// <param name="fields"></param>
	/// <param name="jsonWriter"></param>
	public IncludesExecutor(AutoService<T, ID> svc, ContentFields fields, TypeReaderWriter<T> jsonWriter)
	{
		_svc = svc;
		_fields = fields;
		this.jsonWriter = jsonWriter;
	}

	/// <summary>
	/// ,"includes":[ 
	/// </summary>
	private static readonly byte[] IncludesHeader = new byte[] {
		(byte)',', (byte)'"', (byte)'i', (byte)'n', (byte)'c', (byte)'l', (byte)'u', (byte)'d', (byte)'e', (byte)'s', (byte)'"', (byte)':', (byte)'['
	};

	private static readonly byte[] IncludesFooter = new byte[] { (byte)']', (byte)'}' };

	/// <summary>
	/// {"result": 
	/// </summary>
	private static readonly byte[] ResultHeader = new byte[] { (byte)'{', (byte)'"', (byte)'r', (byte)'e', (byte)'s', (byte)'u', (byte)'l', (byte)'t', (byte)'"', (byte)':' };

	/// <summary>
	/// "null"
	/// </summary>
	private static readonly byte[] NullText = new byte[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };

	/// <summary>
	/// Invoked by generated code. Emits in to a temporary writer which then MUST be copied over to the main writer synchronously.
	/// </summary>
	public async ValueTask WriteJson(Context context, T entity, Writer writer)
	{
		writer.Write(ResultHeader, 0, 10);

		if (entity == null)
		{
			writer.Write(NullText, 0, 4);
		}
		else
		{
			jsonWriter.WriteJsonUnclosed(entity, writer);

			if (functionalIncludes != null)
			{
				for (var i = 0; i < functionalIncludes.Length; i++)
				{
					var fi = functionalIncludes[i];
					var valueGen = fi.ValueGenerator as VirtualFieldValueGenerator<T, ID>;

					if (valueGen != null)
					{
						// ,"propertyName":
						writer.Write(fi._jsonPropertyHeader, 0, fi._jsonPropertyHeader.Length);

						// value:
						await valueGen.GetValue(context, entity, writer);
					}
				}
			}

			writer.Write((byte)'}');
		}

		if (includeSet == null)
		{
			writer.Write((byte)'}');
		}
		else
		{
			// We've got some includes to add.
			// Write the includes header, then write out the data so far.
			writer.Write(IncludesHeader, 0, 13);

			// First we need to obtain ID collectors, and then collect the IDs.
			var firstCollector = includeSet.RootInclude.GetCollectors();

			// Collect all IDs:
			if (entity != null)
			{
				var current = firstCollector;

				while (current != null)
				{
					current.Collect(entity);
					current = current.NextCollector;
				}

				await _svc.ExecuteIncludes(context, null, writer, firstCollector, includeSet.RootInclude);
			}

			writer.Write(IncludesFooter, 0, 2);
		}
	}

	/// <summary>
	/// Sets up the JSON writer for a specific identified service during the compile pass.
	/// </summary>
	/// <param name="incl"></param>
	/// <param name="compileEngine"></param>
	/// <returns></returns>
	public async ValueTask<FieldBuilder> Setup(string incl, NodeLoader compileEngine)
	{
		// Yes - processing includes is required. Create a writer field. This will store the includes output:
		var outputWriterFld = compileEngine.DefineStateField(typeof(Writer));

		// Mark it as a writer field. This ensures that it will be released after a graph runs.
		compileEngine.AddWriterField(outputWriterFld);

		// Obtain the includes set now.
		includeSet = await _fields.GetIncludeSet(incl);
		functionalIncludes = (includeSet == null) ? null : includeSet.RootInclude.FunctionalIncludes;

		return outputWriterFld;
	}
}
