using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Api.CanvasRenderer;


/// <summary>
/// A collection of nodes which can execute in parallel.
/// </summary>
public class ExecutorTranche
{
	/// <summary>
	/// The nodes in this tranche.
	/// </summary>
	public List<Executor> Nodes;

	/// <summary>
	/// The loader that created this tranche.
	/// </summary>
	private NodeLoader _loader;

	/// <summary>
	/// Used during tranche compilation - the custom CanvasGeneratorNode.
	/// </summary>
	public TypeBuilder TypeBuilder;

	/// <summary>
	/// Creates a new tranche
	/// </summary>
	public ExecutorTranche(NodeLoader loader)
	{
		_loader = loader;
		Nodes = new List<Executor>();
	}

	/// <summary>
	/// Bakes the compiled tranche to a type, which inherits CanvasGeneratorGraphTranche, 
	/// and then instances it to ensure it is populated with the node info.
	/// </summary>
	/// <returns></returns>
	public CanvasGeneratorGraphTranche BakeCompiledType()
	{
		var bakedType = TypeBuilder.CreateType();
		TypeBuilder = null;
		var cgNode = (CanvasGeneratorGraphTranche)Activator.CreateInstance(bakedType);
		cgNode.Nodes = Nodes.ToArray();
		return cgNode;
	}

	/// <summary>
	/// Adds a node to this tranche.
	/// </summary>
	public void Add(Executor exec)
	{
		Nodes.Add(exec);
	}
	
}