using Api.Contexts;
using Api.SocketServerLibrary;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Api.CanvasRenderer;


/// <summary>
/// A node in a canvas execution plan.
/// It can load one or more pieces of content, emit raw bytes, execute a graph etc.
/// A canvas execution plan is just a list of generator nodes.
/// </summary>
public class CanvasGeneratorNode
{
	/// <summary>
	/// Runs this node now, generating contextual canvas output into the writer specified by the context.
	/// </summary>
	/// <param name="state"></param>
	/// <returns></returns>
	public virtual ValueTask Generate(GraphContext state)
	{
		// Use specific CanvasGeneratorNode child types
		throw new NotImplementedException();
	}
}

/// <summary>
/// Runs a tranche of graph nodes simultaneously (stacks of nodes which do not have to wait on the DB for data will run sequentially).
/// </summary>
public class CanvasGeneratorGraphTranche : CanvasGeneratorNode
{
	/// <summary>
	/// The nodes in this tranche.
	/// </summary>
	public Executor[] Nodes;

	/// <summary>
	/// Overriden by generated methods.
	/// </summary>
	/// <param name="state"></param>
	public virtual void Execute(GraphContext state)
	{ }

	/// <summary>
	/// Overriden by generated methods.
	/// </summary>
	/// <param name="state"></param>
	/// <param name="writer"></param>
	public virtual void Output(GraphContext state, Writer writer)
	{ }

	/// <summary>
	/// Runs this generator now.
	/// </summary>
	/// <param name="state"></param>
	/// <returns></returns>
	public override async ValueTask Generate(GraphContext state)
	{
		// Reset its waiter just in case anything does need to wait.
		// Custom wait mechanic means we can get async behaviour but without having to emit async node code (at no overhead cost).
		state.ResetWaiter();

		// Execute each node now:
		Execute(state);

		// Wait for all tasks to complete and then returns:
		await state;

		// Perform any outputs now:
		Output(state, state.Writer);
	}
}

/// <summary>
/// A canvas generator node which simply emits constant bytes.
/// </summary>
public class CanvasGeneratorBytes : CanvasGeneratorNode
{
	private byte[] _data;

	/// <summary>
	/// A node which just outputs some bytes
	/// </summary>
	/// <param name="data"></param>
	public CanvasGeneratorBytes(byte[] data)
	{
		_data = data;
	}

	/// <summary>
	/// Runs this node now, generating contextual canvas output into the writer.
	/// </summary>
	/// <param name="state"></param>
	/// <returns></returns>
	public override ValueTask Generate(GraphContext state)
	{
		state.Writer.Write(_data, 0, _data.Length);
		return new ValueTask();
	}

}

