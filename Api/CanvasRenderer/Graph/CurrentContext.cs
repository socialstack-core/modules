using Api.Contexts;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Api.CanvasRenderer;

/// <summary>
/// A content loader node
/// </summary>
public class CurrentContext : Executor
{
	/// <summary>
	/// All fields available on the type
	/// </summary>
	protected ContentFields _fields;
	/// <summary>
	/// A writer field into which the JSON with includes is written
	/// </summary>
	public FieldBuilder _outputWriterFld;

	/// <summary>
	/// Creates a new content loader node
	/// </summary>
	/// <param name="d"></param>
	public CurrentContext(JToken d) : base(d)
    {
    }

	/// <summary>
	/// Compile this node. It must read inputs from and write outputs to the graph state.
	/// </summary>
	/// <param name="compileEngine"></param>
	public override ValueTask Compile(NodeLoader compileEngine)
    {
		// Nothing to do here! The node has no inputs and all
		// values that the output side need are already present.
		return new ValueTask();
	}

	/// <summary>
	/// Emits JSON in to the datamap for an outputted field.
	/// </summary>
	/// <param name="compileEngine"></param>
	/// <param name="field"></param>
	public override void EmitOutputJson(NodeLoader compileEngine, string field)
	{
		// Output a number. Is simply WriteS-ed to the writer. (assumes all IDs are uints).
		compileEngine.EmitWriter();
		EmitOutputRead(compileEngine, field);
		compileEngine.EmitWriteSCall();
	}
    
	/// <summary>
	/// 
	/// </summary>
	/// <param name="compileEngine"></param>
	/// <param name="field"></param>
	/// <exception cref="NotImplementedException"></exception>
	public override Type EmitOutputRead(NodeLoader compileEngine, string field)
	{
		ContextFieldInfo fieldToRead = null;

		foreach (var ctxField in ContextFields.FieldList)
		{
			if (ctxField.Name.ToLower() == field.ToLower())
			{
				fieldToRead = ctxField;
				break;
			}
		}

		if (fieldToRead == null)
		{
			// Context field called 'field' does not exist.
			Log.Warn("graph", "Context field used by a CurrentContext node in a graph does not exist. It should be an ID field e.g. 'UserId'. It was: " + field);
			return null;
		}

		// Read the ID property
		compileEngine.EmitLoadUserContext();
		compileEngine.CodeBody.Emit(OpCodes.Callvirt, fieldToRead.Property.GetGetMethod());

		return fieldToRead.PrivateFieldInfo.FieldType;
	}
}
