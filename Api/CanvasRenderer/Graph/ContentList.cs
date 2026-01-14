using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Api.CanvasRenderer;

/// <summary>
/// A content loader node
/// </summary>
public class ContentList : Executor
{
	/// <summary>
	/// The service being loaded from
	/// </summary>
	public AutoService _svc;
	/// <summary>
	/// A writer field into which the JSON with includes is written
	/// </summary>
	public FieldBuilder _outputWriterFld;

	/// <summary>
	/// IncludesSet
	/// </summary>
	public string _includesStr;
	
	/// <summary>
	/// Filter string
	/// </summary>
	public string _filterStr;

	/// <summary>
	/// Creates a new content loader node
	/// </summary>
	/// <param name="d"></param>
	public ContentList(JToken d) : base(d)
	{
	}

	/// <summary>
	/// Compile this node. It must read inputs from and write outputs to the graph state.
	/// </summary>
	/// <param name="compileEngine"></param>
	public override ValueTask Compile(NodeLoader compileEngine)
	{
		// Is the content type constant?
		if (!GetConstString("contentType", out string ct))
		{
			// Read the textual content type from the upstream link field.
			throw new NotImplementedException("Runtime content types not supported by content node in server graphexec.");
		}

		GetConstString("filter", out _filterStr);

		// It's a constant. Can establish which service it is once at compile time:
		_svc = Services.Get(ct + "Service");

		if (_svc == null)
		{
			throw new Exception("Unknown content type in graphexec");
		}

		_includesStr = GetUsedIncludes();
		
		// Output type is a..
		var svcType = _svc.ServicedType;

		// Create output field:
		_outputWriterFld = compileEngine.DefineStateField(typeof(Writer));

		_setWriter = compileEngine.DefineSetter(_outputWriterFld);

		// Mark it as a writer field. This ensures that it will be released after a graph runs.
		compileEngine.AddWriterField(_outputWriterFld);

		// Get the load content method:
		var loadContentMethod = GetType().GetMethod("LoadContent").MakeGenericMethod(svcType, _svc.IdType);

		// "This" for the load content call:
		compileEngine.EmitCurrentNode();

		// Emit state:
		compileEngine.EmitLoadState();

		// Start constructing the filter here, binding each input arg according to what the filter needs.
		// Rather confusingly though we need to construct the filter twice - once here inside this node
		// such that we know what the bind types are and then a 2nd time, the actual instanced used in the emitted code.

		// 1st one: Call .Where on the _svc for the filter itself.
		// A filter is required regardless of if a filter string is present or not.
		// We have the service on "this" node, but we can get it to do the where call for us.
		compileEngine.EmitCurrentNode();
		var createFilterMethod = GetType().GetMethod(nameof(ContentList.CreateFilter));
		compileEngine.CodeBody.Emit(OpCodes.Callvirt, createFilterMethod);

		if (!string.IsNullOrWhiteSpace(_filterStr))
		{
			// This is the 2nd one!
			var filterForDeterminingBindTypes = _svc.GetGeneralFilterFor(_filterStr);
			var bindTypes = filterForDeterminingBindTypes.GetArgTypes();

			var filterType = typeof(Filter<,>).MakeGenericType(new Type[] { svcType, _svc.IdType });

			for (var i = 0; i < bindTypes.Count; i++)
			{
				var bindTypeRequired = bindTypes[i];

				// Each time bind is called, the filter is returned and ends up back on the stack.
				// Thus, we need to emit the value, convert it if necessary, then call the relevant Bind method.
				// We don't need to keep loading refs to the filter object itself or keep it in some sort of local.

				var inputType = compileEngine.EmitLoadInput("arg" + i, this);

				if (inputType != bindTypeRequired.ArgType)
				{
					// A todo here would be to convert the type as needed. E.g. long -> uint etc probably.
					// As of right now, we only need strings going to string binds.
					throw new PublicException("Graph limitation: content list inputs must currently match the exact bind type.", "");
				}

				var specificBind = filterType.GetMethod("Bind", BindingFlags.Instance | BindingFlags.Public, new Type[] {
					inputType
				});

				// Call the bind method
				if (specificBind != null)
				{
					compileEngine.CodeBody.Emit(OpCodes.Callvirt, specificBind);
				}
				else
				{
					var objBind = filterType.GetMethod("Bind", BindingFlags.Instance | BindingFlags.Public, new Type[] {
						typeof(object)
					});

					if (inputType.IsValueType)
					{
						compileEngine.CodeBody.Emit(OpCodes.Box);
					}

					compileEngine.CodeBody.Emit(OpCodes.Callvirt, objBind);
				}
			}

		}

		// Emit the LoadContent call:
		compileEngine.CodeBody.Emit(OpCodes.Call, loadContentMethod);

		return new ValueTask();
	}

	/// <summary>
	/// For nodes that want the includes system. Includes must be constant. This returns the string of includes to use.
	/// </summary>
	/// <returns></returns>
	public string GetUsedIncludes()
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

	/// <summary>
	/// 
	/// </summary>
	/// <param name="compileEngine"></param>
	/// <param name="field"></param>
	/// <exception cref="NotImplementedException"></exception>
	public override Type EmitOutputRead(NodeLoader compileEngine, string field)
	{
		throw new NotImplementedException("Cannot use list output as an intermediate node.");
	}

	/// <summary>
	/// Sets an output writer field which holds the JSON list and any includes.
	/// </summary>
	public SetGeneratedField _setWriter;
	
	/// <summary>
	/// Creates a filter instance.
	/// </summary>
	/// <returns></returns>
	public FilterBase CreateFilter()
	{
		return _svc.GetGeneralFilterFor(_filterStr);
	}

	/// <summary>
	/// Loads the content in to the given graph context now. Called via generated code.
	/// </summary>
	/// <param name="state"></param>
	/// <param name="filter"></param>
	public void LoadContent<T, ID>(GraphContext state, Filter<T, ID> filter)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		var writer = Writer.GetPooled();
		writer.Start(null);

		_setWriter(state, writer);

		// Load as a ValueTask:
		var contentVT = ((AutoService<T, ID>)_svc).ToJson(state.Context, filter, async (Context ctx, Filter<T, ID> filt, Func<T, int, ValueTask> onResult) => {
			var service = ((AutoService<T, ID>)_svc);
			return await ((AutoService<T, ID>)_svc).GetResults(ctx, filt, async (Context ctx2, T result, int index, object src, object srcB) => {
				var _onResult = src as Func<T, int, ValueTask>;
				await _onResult(result, index);
			}, onResult, null);

		}, writer, null, _includesStr, filter.IncludeTotal);

		filter.Release();

		if (!contentVT.IsCompleted)
		{
			var t = contentVT.AsTask();
			state.AddWaiter();
			t.ContinueWith(ContentAfterLoad, state);
		}
	}

	private void ContentAfterLoad(Task res, object st)
	{
		var gc = (GraphContext)st;
		gc.RemoveWaiter();
	}
}
