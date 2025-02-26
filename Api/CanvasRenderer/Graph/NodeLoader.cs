using Api.Permissions;
using Api.SocketServerLibrary;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Api.CanvasRenderer;


/// <summary>
/// Node loader. Has the role of ensuring nodes duplicated across multiple graphs are loaded just once.
/// </summary>
public class NodeLoader
{

	/// <summary>
	/// All nodes formed by the loader.
	/// </summary>
	public List<Executor> Nodes = new List<Executor>();

	/// <summary>
	/// Returns the maximum order value.
	/// </summary>
	/// <returns></returns>
	public int GetMaxOrder()
	{
		var order = 0;

		for (var i = 0; i < Nodes.Count; i++)
		{
			var curOrder = Nodes[i].Order;

			if (curOrder > order)
			{
				order = curOrder;
			}
		}

		return order;
	}

	/// <summary>
	/// Arranges global graph nodes in to tranches. These are groups of nodes which can be safely executed in parallel.
	/// </summary>
	/// <returns></returns>
	public ExecutorTranche[] CreateTranches()
	{
		if (Nodes.Count == 0)
		{
			return null;
		}

		var tranches = new ExecutorTranche[GetMaxOrder()];

		for (var i = 0; i < tranches.Length; i++)
		{
			tranches[i] = new ExecutorTranche(this);
		}

		for (var i = 0; i < Nodes.Count; i++)
		{
			var trancheIndex = Nodes[i].Order - 1;
			tranches[trancheIndex].Add(Nodes[i]);
		}

		return tranches;
	}

	/// <summary>
	/// Creates a node loader for a graph set using the specified primary content type.
	/// </summary>
	/// <param name="primaryType"></param>
	public NodeLoader(Type primaryType)
	{
		_primaryType = primaryType;
	}

	private Type _primaryType;

	/// <summary>
	/// Adds the given node. If any of them already existed in this loader then they will be substituted.
	/// </summary>
	/// <param name="node"></param>
	public Executor Add(Executor node)
	{
		foreach (var existing in Nodes)
		{
			if (existing.IsSame(node))
			{
				return existing;
			}
		}

		Nodes.Add(node);
		return node;
	}

	/// <summary>
	/// Current tranche method being constructed.
	/// </summary>
	public MethodBuilder Method;
	/// <summary>
	/// Body of the tranche method currently being constructed.
	/// </summary>
	public ILGenerator CodeBody;

	/// <summary>
	/// Emits an output writer ref to the stack.
	/// </summary>
	public void EmitWriter()
	{
		CodeBody.Emit(OpCodes.Ldarg_2);
	}

	private static MethodInfo _writeByte;
	private static MethodInfo _writeASCII;

	/// <summary>
	/// Emits an output writer ref to the stack.
	/// </summary>
	public void EmitWriteByte(byte v)
	{
		if (_writeByte == null)
		{
			_writeByte = typeof(Writer).GetMethod(
				"Write",
				System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
				new Type[] {
					 typeof(byte)
				}
			 );
		}

		CodeBody.Emit(OpCodes.Ldarg_2);
		CodeBody.Emit(OpCodes.Ldc_I4_S, v);
		CodeBody.Emit(OpCodes.Call, _writeByte);
	}

	/// <summary>
	/// Emits a WriteASCII call.
	/// </summary>
	/// <param name="s"></param>
	public void EmitWriteASCII(string s)
	{
		if (_writeASCII == null)
		{
			_writeASCII = typeof(Writer).GetMethod(
				"WriteASCII"
			);
		}

		CodeBody.Emit(OpCodes.Ldarg_2);
		CodeBody.Emit(OpCodes.Ldstr, s);
		CodeBody.Emit(OpCodes.Call, _writeASCII);
	}
	/// <summary>
	/// Emits a WriteS(int) call using the stack value as the source. You MUST emit the writer as well as your value.
	/// </summary>
	public void EmitWriteSCall()
	{
		var writeS = typeof(Writer).GetMethod("WriteS", BindingFlags.Instance | BindingFlags.Public, new Type[] { typeof(int) });
		CodeBody.Emit(OpCodes.Callvirt, writeS);
	}

	/// <summary>
	/// Loads the primary object from graph state on to the evaluation stack.
	/// </summary>
	public void EmitLoadPrimary()
	{
		// Load graph state (arg 1):
		CodeBody.Emit(OpCodes.Ldarg_1);

		// Load PrimaryObject field from it:
		var poField = typeof(GraphContext).GetField("PrimaryObject");

		CodeBody.Emit(OpCodes.Ldfld, poField);
	}

	/// <summary>
	/// Loads the primary object from graph state on to the evaluation stack.
	/// </summary>
	public void EmitLoadState()
	{
		// Load graph state (arg 1):
		CodeBody.Emit(OpCodes.Ldarg_1);
	}

	/// <summary>
	/// Loads the user context from graph state on to the evaluation stack.
	/// </summary>
	public void EmitLoadUserContext()
	{
		// Load graph state (arg 1):
		CodeBody.Emit(OpCodes.Ldarg_1);

		// Load user Context field from it:
		var ctxField = typeof(GraphContext).GetField("Context");

		CodeBody.Emit(OpCodes.Ldfld, ctxField);
	}

	private static int genTranchCounter = 1;

	/// <summary>
	/// A list of all internal json writer fields.
	/// This is important to ensure that after a graph is done executing, all internal json writers are released back to the pool.
	/// </summary>
	public List<FieldBuilder> _writerFields;

	/// <summary>
	/// Adds a writer field to the writer field list.
	/// </summary>
	/// <param name="fld"></param>
	public void AddWriterField(FieldBuilder fld)
	{
		if (_writerFields == null)
		{
			_writerFields = new List<FieldBuilder>();
		}

		_writerFields.Add(fld);
	}

	/// <summary>
	/// Defines a set delegate for the given field.
	/// </summary>
	/// <param name="fld"></param>
	/// <returns></returns>
	public SetGeneratedField DefineSetter(FieldBuilder fld)
	{
		var type = _modBuilder.DefineType("GraphSetter_" + fld.Name, TypeAttributes.Public);

		var mtd = type.DefineMethod("Set", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] {
			typeof(GraphContext),
			typeof(object)
		});

		var body = mtd.GetILGenerator();
		body.Emit(OpCodes.Ldarg_0); // the state
		body.Emit(OpCodes.Ldarg_1); // the value to set
		body.Emit(OpCodes.Stfld, fld); // set the field
		body.Emit(OpCodes.Ret); // done!

		var bakedType = type.CreateType();

		return bakedType.GetMethod("Set").CreateDelegate<SetGeneratedField>();
	}

	private ModuleBuilder _modBuilder;

	/// <summary>
	/// Starts the compilation process of all the given tranches, plus the GraphContext state object.
	/// Call BakeCompiledType on each tranche afterwards when all state fields are added.
	/// </summary>
	/// <param name="tranches"></param>
	public async ValueTask CompileTranches(ExecutorTranche[] tranches)
	{
		AssemblyName assemblyName = new AssemblyName("CanvasGenerator_" + genTranchCounter);
		genTranchCounter++;
		AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
		ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
		_modBuilder = moduleBuilder;


		// Start generating the context type:
		_graphCtxType = moduleBuilder.DefineType("GraphContextGen", TypeAttributes.Public, typeof(GraphContext));

		// Mark the very first data map loader.
		DmIsFirst = true;

		for (var i = 0; i < tranches.Length; i++)
		{
			tranches[i].TypeBuilder = await CompileTranche(moduleBuilder, i, tranches[i]);
		}

		if (_writerFields != null && _writerFields.Count > 0)
		{

			// Next, generate the writer clear method.
			var releaser = _graphCtxType.DefineMethod("ReleaseBuffers",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
				typeof(void), // Tranche execute methods return nothing.
				Array.Empty<Type>()
			);

			var code = releaser.GetILGenerator();

			var release = typeof(Writer).GetMethod("Release");

			for (var i = 0; i < _writerFields.Count; i++)
			{
				var itsNull = code.DefineLabel();
				var afterEverything = code.DefineLabel();
				code.Emit(OpCodes.Ldarg_0);
				code.Emit(OpCodes.Ldfld, _writerFields[i]);
				code.Emit(OpCodes.Dup);

				// if(writer != null)
				code.Emit(OpCodes.Ldnull);
				code.Emit(OpCodes.Ceq);
				code.Emit(OpCodes.Brtrue, itsNull);

				// Writer is on stack due to the dup, and it is not null.
				// Release it.

				// writer.Release();
				code.Emit(OpCodes.Call, release);

				// Setfld to null:
				code.Emit(OpCodes.Ldarg_0);
				code.Emit(OpCodes.Ldnull);
				code.Emit(OpCodes.Stfld, _writerFields[i]);

				// Loading a null to then pop it to avoid a branch.
				code.Emit(OpCodes.Ldnull);

				code.MarkLabel(itsNull);

				// A null is on the stack due to the dup. Pop it.
				code.Emit(OpCodes.Pop);
				code.MarkLabel(afterEverything);

			}

			code.Emit(OpCodes.Ret);

		}

	}

	/// <summary>
	/// True if the next encountered DataMapLoader is the first one. It does not emit a comma.
	/// </summary>
	public bool DmIsFirst;
	private int _stateFieldId = 1;

	/// <summary>
	/// Defines a field in the state class (the instance of GraphContext).
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public FieldBuilder DefineStateField(Type type)
	{
		return _graphCtxType.DefineField("sf_" + _stateFieldId++, type, FieldAttributes.Public);
	}

	/// <summary>
	/// Bakes all the compiled types, indicating that compilation has completed.
	/// </summary>
	/// <returns></returns>
	public Type BakeCompiledTypes()
	{
		return _graphCtxType.CreateType();
	}

	private TypeBuilder _graphCtxType;

	/// <summary>
	/// Starts compiling a tranche.
	/// </summary>
	/// <param name="moduleBuilder"></param>
	/// <param name="trancheIndex"></param>
	/// <param name="tranche"></param>
	private async ValueTask<TypeBuilder> CompileTranche(ModuleBuilder moduleBuilder, int trancheIndex, ExecutorTranche tranche)
	{
		var typeBuilder = moduleBuilder.DefineType("Tranche_" + trancheIndex, TypeAttributes.Public, typeof(CanvasGeneratorGraphTranche));

		var exec = typeBuilder.DefineMethod(
			"Execute",
			MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
			typeof(void), // Tranche execute methods return nothing.
			new Type[] {
				typeof(GraphContext) // Tranche methods take 1 arg: the graph context.
			}
		);

		var output = typeBuilder.DefineMethod(
			"Output",
			MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
			typeof(void), // Tranche execute methods return nothing.
			new Type[] {
				typeof(GraphContext), // The graph context which also includes the canvas writer.
				typeof(Writer)
			}
		);

		CodeBody = exec.GetILGenerator();

		// For each node in the tranche, emit it in to the loader.
		for (var i = 0; i < tranche.Nodes.Count; i++)
		{
			_currentNodeIndex = i;
			await tranche.Nodes[i].Compile(this);
		}

		CodeBody.Emit(OpCodes.Ret);

		// Compile output method too. It outputs any datamap entries from the nodes, after they have executed.
		// This means this particular method has sole access to the writer which greatly helps with threading.
		CodeBody = output.GetILGenerator();

		// For each node in the tranche, emit it in to the loader.
		for (var i = 0; i < tranche.Nodes.Count; i++)
		{
			_currentNodeIndex = i;
			tranche.Nodes[i].CompileOutput(this);
		}


		CodeBody.Emit(OpCodes.Ret);

		return typeBuilder;
	}

	private int _currentNodeIndex;

	/// <summary>
	/// Emits a reference on to the stack to the current canvas node.
	/// Note that it is not the "this" (arg0) ref as multiple nodes are generated into 1 tranche method.
	/// </summary>
	public void EmitCurrentNode()
	{
		// "this" is a CanvasGeneratorGraphTranche. Need to do the equiv of this.Nodes[_currentNodeIndex].
		CodeBody.Emit(OpCodes.Ldarg_0); // this

		var nodesField = typeof(CanvasGeneratorGraphTranche).GetField("Nodes");
		CodeBody.Emit(OpCodes.Ldfld, nodesField);

		// Emit the index:
		CodeBody.Emit(OpCodes.Ldc_I4, _currentNodeIndex);
		CodeBody.Emit(OpCodes.Ldelem_Ref);
	}

	/// <summary>
	/// Reads from linked input fields.
	/// </summary>
	/// <param name="field"></param>
	/// <param name="node"></param>
	/// <param name="defaultPermitted"></param>
	public Type EmitLoadInput(string field, Executor node, bool defaultPermitted = false)
	{
		if (!node.Links.TryGetValue(field, out NodeLink link))
		{
			if (defaultPermitted)
			{
				return null;
			}
			throw new ArgumentException("Can only use this on dynamic graph links. " + field + " does not exist as a dynamic link.");
		}

		// If the link is the value "null", also return null.
		if (link.SourceNode == null)
		{
			return null;
		}

		// Emit an output read from the upstream node:
		return link.SourceNode.EmitOutputRead(this, link.Field);
	}

	/// <summary>
	/// Gets the current primary content type for the graph.
	/// </summary>
	/// <returns></returns>
	public Type GetPrimaryType()
	{
		return _primaryType;
	}

}