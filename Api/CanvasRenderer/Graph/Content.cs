using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Api.CanvasRenderer;
	
/// <summary>
/// Used for generated content field setters.
/// </summary>
public delegate void SetGeneratedField(GraphContext ctx, object entity);

/// <summary>
/// A content loader node
/// </summary>
public class Content : Executor
{
	/// <summary>
	/// The service being loaded from
	/// </summary>
    public AutoService _svc;
	/// <summary>
	/// True if we're using the primary type of the pg
	/// </summary>
    private bool _isPrimary;
	/// <summary>
	/// The content type being loaded
	/// </summary>
    private Type _contentType;
	/// <summary>
	/// All fields available on the type
	/// </summary>
	protected ContentFields _fields;
	/// <summary>
	/// A writer field into which the JSON with includes is written
	/// </summary>
	public FieldBuilder _outputWriterFld;

	/// <summary>
	/// Used to serialise a complete object. Is a TypeReaderWriter[T].
	/// </summary>
	public object _typeReadWrite;

	/// <summary>
	/// IncludesSet
	/// </summary>
	public object _includes;

	/// <summary>
	/// Creates a new content loader node
	/// </summary>
	/// <param name="d"></param>
	public Content(JToken d) : base(d)
    {
    }

	/// <summary>
	/// Compile this node. It must read inputs from and write outputs to the graph state.
	/// </summary>
	/// <param name="compileEngine"></param>
	public override async ValueTask Compile(NodeLoader compileEngine)
    {
        // Is the content type constant?
        if (!GetConstString("contentType", out string ct))
        {
            // Read the textual content type from the upstream link field.
            throw new NotImplementedException("Runtime content types not supported by content node in server graphexec.");
        }

        if (ct == "primary")
        {
            // This node outputs the primary content for the page.
            // The actual execution of the node does nothing at all unless there are includes.
            _contentType = compileEngine.GetPrimaryType();
            _svc = Services.GetByContentType(_contentType);
            _isPrimary = true;

            await SetupFields(compileEngine);

			if (_includes != null)
			{
				// Must load and wait for the includes.
				var loadIncludesMethod = GetType().GetMethod("LoadPrimaryIncludes").MakeGenericMethod(_contentType, _svc.IdType);

				// "This" for the load content call:
				compileEngine.EmitCurrentNode();

				// Emit state:
				compileEngine.EmitLoadState();

				// Emit the LoadContent call:
				compileEngine.CodeBody.Emit(OpCodes.Call, loadIncludesMethod);
			}

			return;
        }

		// It's a constant. Can establish which service it is once at compile time:
		_svc = Services.Get(ct + "Service");

        await SetupFields(compileEngine);

		// Output type is a..
		var svcType = _svc.ServicedType;
        _contentType = svcType;

		// Get the load content method:
		var loadContentMethod = GetType().GetMethod("LoadContent").MakeGenericMethod(_contentType,_svc.IdType);

		// Create field of that type:
		_outputFld = compileEngine.DefineStateField(svcType);
		_setContent = compileEngine.DefineSetter(_outputFld);

		// Input ID type is a..
		var idType = _svc.IdType;

		// "This" for the load content call:
		compileEngine.EmitCurrentNode();

		// Emit state:
		compileEngine.EmitLoadState();

		// And the ID:
		if (GetConstNumber("contentId", out long lId))
		{
            // Constant ID. Convert it to the correct ID type:
            if (idType == typeof(ulong))
            {
                compileEngine.CodeBody.Emit(OpCodes.Ldc_I8, lId);
            }
            else if (idType == typeof(uint))
            {
                compileEngine.CodeBody.Emit(OpCodes.Ldc_I4, (uint)lId);
            }
            else
            {
                throw new NotSupportedException("ID type must be uint or ulong.");
            }
		}
		else
		{
			// Read ID from input as an ID type (service.IdType)
			var inputType = compileEngine.EmitLoadInput("contentId", this);

			if (inputType == null)
			{
				// Backwards compatibility. No contentId was provided so we'll load a 0.
				if (idType == typeof(ulong))
				{
					compileEngine.CodeBody.Emit(OpCodes.Ldc_I8, (ulong)0);
				}
				else if (idType == typeof(uint))
				{
					compileEngine.CodeBody.Emit(OpCodes.Ldc_I4, (uint)0);
				}
				else
				{
					throw new NotSupportedException("ID type must be uint or ulong.");
				}
			}
			else if (inputType != idType)
			{
				// May be a nullable however.
				if (Nullable.GetUnderlyingType(inputType) == idType)
				{
					// There is a nullable on the stack. Must now proceed to read the value from it.
					// That requires an address so we must store it in a local first.
					var loc = compileEngine.CodeBody.DeclareLocal(inputType);
					var after = compileEngine.CodeBody.DefineLabel();
					var doneAll = compileEngine.CodeBody.DefineLabel();
					compileEngine.CodeBody.Emit(OpCodes.Stloc, loc);
					compileEngine.CodeBody.Emit(OpCodes.Ldloca, loc);
					compileEngine.CodeBody.Emit(OpCodes.Dup);
					compileEngine.CodeBody.Emit(OpCodes.Callvirt, inputType.GetProperty("HasValue").GetGetMethod());
					// The t/f is now on the stack. Check if it's null, and if so, ret.
					compileEngine.CodeBody.Emit(OpCodes.Ldc_I4_0);
					compileEngine.CodeBody.Emit(OpCodes.Ceq);
					compileEngine.CodeBody.Emit(OpCodes.Brfalse, after);
					compileEngine.CodeBody.Emit(OpCodes.Pop); // Remove the val (which we duped above to read HasValue) from the stack.

					if (idType == typeof(uint))
					{
						compileEngine.CodeBody.Emit( OpCodes.Ldc_I4_0); // Push a 0
					}
					else
					{
						compileEngine.CodeBody.Emit(OpCodes.Ldc_I8, (long)0);
					}
					
					compileEngine.CodeBody.Emit(OpCodes.Br, doneAll);
					compileEngine.CodeBody.MarkLabel(after);
					compileEngine.CodeBody.Emit(OpCodes.Callvirt, inputType.GetProperty("Value").GetGetMethod());
					compileEngine.CodeBody.MarkLabel(doneAll);

				}
				else
				{
					throw new Exception("Unsupported use of a different ID type currently. ID type must match or be a nullable version.");
				}
			}
		}

		// Emit the LoadContent call:
		compileEngine.CodeBody.Emit(OpCodes.Call, loadContentMethod);

	}

    private async ValueTask SetupFields(NodeLoader compileEngine)
    {
		if (_svc == null)
		{
			throw new Exception("Unknown content type in graphexec");
		}

		// All available fields:
		_fields = _svc.GetContentFields();

		// Invoke SetupWriter to set the _typeReadWrite field.
		var setupWriterMethod = GetType().GetMethod(nameof(SetupWriter));

		var setupWriter = setupWriterMethod.MakeGenericMethod(new Type[] {
				_svc.ServicedType,
				_svc.IdType
			});

		await(ValueTask)(setupWriter.Invoke(this, new object[] {
			compileEngine,
			_svc
		}));
	}

	/// <summary>
	/// Sets up the JSON writer for a specific identified service during the compile pass.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	/// <param name="compileEngine"></param>
	/// <param name="svc"></param>
	/// <returns></returns>
	public async ValueTask SetupWriter<T, ID>(NodeLoader compileEngine, AutoService<T, ID> svc)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		var jsonStructure = await svc.GetTypedJsonStructure(new Context());

		var tio = jsonStructure.TypeIO;

		if (tio == null)
		{
			jsonStructure.TypeIO = tio = TypeIOEngine.Generate(jsonStructure);
		}

		_typeReadWrite = tio;

		string incl = GetUsedIncludes(_fields);

		if (!string.IsNullOrEmpty(incl))
		{
			var includesEngine = new IncludesExecutor<T, ID>(svc, _fields, tio);
			_includes = includesEngine;
			_outputWriterFld = await includesEngine.Setup(incl, compileEngine);
			_setWriter = compileEngine.DefineSetter(_outputWriterFld);
		}

	}

	/// <summary>
	/// For nodes that want the includes system. Includes must be constant. This returns the string of includes to use.
	/// </summary>
	/// <returns></returns>
	public string GetUsedIncludes(ContentFields fields)
	{
		if (DataMapOutputs != null)
		{
			// If any of these are using an included field or output then we must handle includes too.
			var includesUsed = false;

			foreach (var dmo in DataMapOutputs)
			{
				if (dmo.Field == "output")
				{
					includesUsed = true;
					break;
				}
				else if (fields.TryGetOrGlobal(dmo.Field.ToLower(), out ContentField cf))
				{
					if (cf.IsVirtual)
					{
						includesUsed = true;
						break;
					}
				}
			}

			// Do we even have includes? they must be constant.
			if (includesUsed && GetConstString("includes", out string incl))
			{
				if (!string.IsNullOrEmpty(incl))
				{
					return incl;
				}
			}
		}

		return null;
	}
	
	/// <summary>
	/// Emits JSON in to the datamap for an outputted field.
	/// </summary>
	/// <param name="compileEngine"></param>
	/// <param name="field"></param>
	public override void EmitOutputJson(NodeLoader compileEngine, string field)
	{
		if (field == "output")
		{
			// Using a full JSON write here.
			if (_includes != null)
			{
				// Copy from the includes writer to the output writer.
				// Note that this writer is released at the end.
				compileEngine.EmitLoadState();
				compileEngine.CodeBody.Emit(OpCodes.Ldfld, _outputWriterFld);

				// The writer to copy from is now on the top of the stack.

				// Writer to copy to:
				compileEngine.EmitWriter();

				// Call the CopyTo method:
				var copyTo = typeof(Writer).GetMethod("CopyTo", BindingFlags.Public | BindingFlags.Instance, new Type[] { typeof(Writer) });
				compileEngine.CodeBody.Emit(OpCodes.Call, copyTo);
			}
			else
			{
				// _typeReadWriter
				compileEngine.EmitCurrentNode();

				var typeReadWriteField = GetType().GetField("_typeReadWrite", BindingFlags.Public | BindingFlags.Instance);

				compileEngine.CodeBody.Emit(OpCodes.Ldfld, typeReadWriteField);

				// The object to serialise:
				if (_isPrimary)
				{
					compileEngine.EmitLoadPrimary();
				}
				else
				{
					// Load state:
					compileEngine.EmitLoadState();

					// Load output field:
					compileEngine.CodeBody.Emit(OpCodes.Ldfld, _outputFld);
				}

				var notNullO = compileEngine.CodeBody.DefineLabel();
				var afterO = compileEngine.CodeBody.DefineLabel();
				compileEngine.CodeBody.Emit(OpCodes.Dup);
				compileEngine.CodeBody.Emit(OpCodes.Ldnull);
				compileEngine.CodeBody.Emit(OpCodes.Ceq);
				compileEngine.CodeBody.Emit(OpCodes.Brfalse, notNullO);

				compileEngine.CodeBody.Emit(OpCodes.Pop);
				compileEngine.CodeBody.Emit(OpCodes.Pop);

				// Parent object was null. Emit a null here too.
				compileEngine.EmitWriteASCII("null");
				compileEngine.CodeBody.Emit(OpCodes.Br, afterO);
				compileEngine.CodeBody.MarkLabel(notNullO);

				// Writer to put it in:
				compileEngine.EmitWriter();

				// Invoke the write method:
				var writeJsonMethod = _typeReadWrite.GetType().GetMethod("WriteJsonUnclosed");
				compileEngine.CodeBody.Emit(OpCodes.Callvirt, writeJsonMethod);

				// It's unclosed so next emit a write of the closing bracket.
				compileEngine.EmitWriteByte((byte)'}');

				compileEngine.CodeBody.MarkLabel(afterO);
			}

			return;
		}

		if (_fields == null || !_fields.TryGetOrGlobal(field.ToLower(), out ContentField fld))
		{
			// Field not found, but we will output a null for backwards compatibility.
			compileEngine.EmitWriteASCII("null");
			return;
		}

		if (fld.IsVirtual)
		{
			// Hack-lite to greatly simplify virtual field usage here.
			// Emit the whole content object and its include fields,
			// but then tell the datamap (in the js) to select the field the user asked for.
			// Wastes some bytes on the wire, but massively simplifies include system usage (in part because of nested includes).	

			compileEngine.EmitLoadState();
			compileEngine.CodeBody.Emit(OpCodes.Ldfld, _outputWriterFld);

			// The writer to copy from is now on the top of the stack.

			// Writer to copy to:
			compileEngine.EmitWriter();

			// Call the CopyTo method:
			var copyTo = typeof(Writer).GetMethod("CopyTo", BindingFlags.Public | BindingFlags.Instance, new Type[] { typeof(Writer) });
			compileEngine.CodeBody.Emit(OpCodes.Call, copyTo);

			// And the field name in the datamap entry. Must match the name used by the includes system, which is the field name with a lowercase first letter.

			// lc first:
			var fieldName = char.ToLower(fld.Name[0]) + fld.Name.Substring(1);

			compileEngine.EmitWriteASCII(",\"f\":\"" + fieldName + "\"");

			return;
		}

		// If thing is null, effectively put a null on the stack.
		if (_isPrimary)
		{
			compileEngine.EmitLoadPrimary();
		}
		else
		{
			// Load state:
			compileEngine.EmitLoadState();

			// Load output field:
			compileEngine.CodeBody.Emit(OpCodes.Ldfld, _outputFld);
		}

		var notNull = compileEngine.CodeBody.DefineLabel();
		var after = compileEngine.CodeBody.DefineLabel();
		compileEngine.CodeBody.Emit(OpCodes.Ldnull);
		compileEngine.CodeBody.Emit(OpCodes.Ceq);
		compileEngine.CodeBody.Emit(OpCodes.Brfalse, notNull);

		// Parent object was null. Emit a null here too.
		compileEngine.EmitWriteASCII("null");

		compileEngine.CodeBody.Emit(OpCodes.Br, after);

		compileEngine.CodeBody.MarkLabel(notNull);

		// Write a singular field:
		TypeIOEngine.EmitWriteField(compileEngine.CodeBody, fld, (ILGenerator body) => {

            // Load the input:
            if (_isPrimary)
            {
				compileEngine.EmitLoadPrimary();
			}
            else
            {
				// Load state:
				compileEngine.EmitLoadState();

				// Load output field:
				compileEngine.CodeBody.Emit(OpCodes.Ldfld, _outputFld);
			}

		});

		compileEngine.CodeBody.MarkLabel(after);

	}
        
	private FieldBuilder _outputFld;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="compileEngine"></param>
	/// <param name="field"></param>
	/// <exception cref="NotImplementedException"></exception>
	public override Type EmitOutputRead(NodeLoader compileEngine, string field)
	{
		if (field == "output")
		{
			// Whole object. Do nothing else here.
			return _contentType;
		}

		if (_fields == null || !_fields.TryGetOrGlobal(field.ToLower(), out ContentField fld))
		{
			// Field not found. We don't know what type the field used to be, so we have to emit nothing and return null.
			return null;
		}

		if (fld.FieldInfo != null || fld.PropertyInfo != null)
		{
			if (_isPrimary)
			{
				compileEngine.EmitLoadPrimary();
			}
			else
			{
				// Load state:
				compileEngine.EmitLoadState();

				// Load output field:
				compileEngine.CodeBody.Emit(OpCodes.Ldfld, _outputFld);
			}

			// Check if it is null. If so, do not read the field.
			var wasNull = compileEngine.CodeBody.DefineLabel();
			var after = compileEngine.CodeBody.DefineLabel();
			compileEngine.CodeBody.Emit(OpCodes.Dup);
			compileEngine.CodeBody.Emit(OpCodes.Ldnull);
			compileEngine.CodeBody.Emit(OpCodes.Ceq);
			compileEngine.CodeBody.Emit(OpCodes.Brtrue, wasNull);

			if (fld.PropertyInfo != null)
			{
				// Load from the field using the src object on the stack currently:
				compileEngine.CodeBody.Emit(OpCodes.Callvirt, fld.PropertyInfo.GetGetMethod());
			}
			else if (fld.FieldInfo != null)
			{
				// Load from the field, using the src object on the stack currently:
				compileEngine.CodeBody.Emit(OpCodes.Ldfld, fld.FieldInfo);
			}

			compileEngine.CodeBody.Emit(OpCodes.Br, after);
			compileEngine.CodeBody.MarkLabel(wasNull);

			// There is a null on the stack currently which we will reuse as the actual output value.
			// Note that would be inappropriate if the src value is a valuetype.
			/*
			 Handle this in the future:

			if(fld.FieldType.IsValueType){
				compileEngine.CodeBody.Emit(OpCodes.Pop); // pop the null
				// do default(fld.FieldType)
			}

			 */

			compileEngine.CodeBody.MarkLabel(after);
		}
		else
		{
			Log.Warn(
				"graphs",
				"Virtual field is being used by another node - this is not supported serverside and indicates a likely incorrect graph. The field in use was " + fld.Name + "."
			);

			compileEngine.CodeBody.Emit(OpCodes.Ldnull);
			return typeof(object);
		}

		return fld.FieldType;
	}

	/// <summary>
	/// Sets an output writer field for includes.
	/// </summary>
	public SetGeneratedField _setWriter;
	/// <summary>
	/// Sets the output content field.
	/// </summary>
	public SetGeneratedField _setContent;

	/// <summary>
	/// Loads the content in to the given graph context now. Called via generated code.
	/// </summary>
	/// <param name="state"></param>
	/// <param name="id"></param>
	public void LoadContent<T, ID>(GraphContext state, ID id)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
        // Load as a ValueTask:
        var contentVT = ((AutoService<T, ID>)_svc).Get(state.Context, id);

		if (contentVT.IsCompleted)
		{
			_setContent(state, contentVT.Result);

			// Are includes needed?
			if (_includes != null)
			{
				// If yes, we'll start constructing them in to the writer and begin a waiter.
				var incEx = (IncludesExecutor<T, ID>)_includes;

				var writer = Writer.GetPooled();
				writer.Start(null);

				_setWriter(state, writer);

				var vt = incEx.WriteJson(state.Context, contentVT.Result, writer);

				if (!vt.IsCompleted)
				{
					// Wait for it too.
					state.AddWaiter();
					var t = vt.AsTask();

					t.ContinueWith((Task res, object st) =>
					{
						// Definitely done now:
						var gc = (GraphContext)st;
						gc.RemoveWaiter();
					}, state);
				}
			}
		}
		else
		{
			var t = contentVT.AsTask();
			state.AddWaiter();
			t.ContinueWith(ContentAfterLoad<T,ID>, state);
		}
	}

	/// <summary>
	/// Loads the includes for a primary object in to the given graph context now. Called via generated code.
	/// </summary>
	/// <param name="state"></param>
	public void LoadPrimaryIncludes<T, ID>(GraphContext state)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		// Load as a ValueTask:
		var content = (T)state.PrimaryObject;
			
		// start constructing them in to the writer and begin a waiter.
		var incEx = (IncludesExecutor<T, ID>)_includes;

		var writer = Writer.GetPooled();
		writer.Start(null);
		_setWriter(state, writer);

		var vt = incEx.WriteJson(state.Context, content, writer);

		if (!vt.IsCompleted)
		{
			// Wait for it too.
			state.AddWaiter();
			var t = vt.AsTask();

			t.ContinueWith((Task res, object st) =>
			{
				// Definitely done now:
				var gc = (GraphContext)st;
				gc.RemoveWaiter();
			}, state);
		}
	}

	private void ContentAfterLoad<T, ID>(Task<T> res, object st)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		var gc = (GraphContext)st;
		_setContent(gc, res.Result);

		// Are includes needed?
		// If yes, don't stop the waiter and start constructing the json into the inclded writer.
		if (_includes == null)
		{
			gc.RemoveWaiter();
			return;
		}

		var incEx = (IncludesExecutor<T, ID>)_includes;

		var writer = Writer.GetPooled();
		writer.Start(null);
		_setWriter(gc, writer);

		var vt = incEx.WriteJson(gc.Context, res.Result, writer);

		if (vt.IsCompleted)
		{
			gc.RemoveWaiter();
		}
		else
		{
			// Wait for it too.
			var t = vt.AsTask();

			t.ContinueWith((Task res, object st) =>
			{
				// Definitely done now!
				var gc = (GraphContext)st;
				gc.RemoveWaiter();
			}, gc);
		}
	}
}
