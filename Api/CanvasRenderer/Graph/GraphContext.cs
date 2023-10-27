using Api.Contexts;
using Api.SocketServerLibrary;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Api.CanvasRenderer;


/// <summary>
/// A user specific context for storing state whilst a graph is running. 
/// Originate from a pool which contains IL generated classes which derive from this one.
/// </summary>
public class GraphContext : INotifyCompletion
{
	/// <summary>
	/// User context.
	/// </summary>
	public Context Context;

	/// <summary>
	/// The PO of the page, if there is one.
	/// </summary>
	public object PrimaryObject;

	/// <summary>
	/// The writer into which the JSON is written.
	/// If writing in a Task or callback, you MUST lock this as there can be more than one such task writing to it simultaneously.
	/// Otherwise, directly writing to it during the Generate(state) call is permitted.
	/// </summary>
	public Writer Writer;

	/// <summary>
	/// Total number of async tasks that we are currently waiting for before we can proceed.
	/// </summary>
	private int _waitingFor;
	
	/// <summary>
	/// Creates a default graph context. This is usually the base class of an IL generated context object.
	/// </summary>
	public GraphContext()
	{
	}

	/// <summary>
	/// Overriden in generated types. Releases any writers in this context object after a graph is done generating.
	/// </summary>
	public virtual void ReleaseBuffers()
	{
	
	}

	/// <summary>
	/// True if there is nothing we are waiting for in the current context.
	/// </summary>
	public bool IsCompleted => _waitingFor == 0;

	/// <summary>
	/// Reset the waiting mechanism.
	/// </summary>
	public void ResetWaiter()
	{
		_continuation = null;
		_waitingFor = 0;
	}

	/// <summary>
	/// Async await.
	/// </summary>
	/// <returns></returns>
	public GraphContext GetResult()
	{
		return this;
	}

	private Action _continuation;

	/// <summary>
	/// Async await.
	/// </summary>
	/// <param name="continuation"></param>
	public void OnCompleted(Action continuation)
	{
		if (_waitingFor == 0)
		{
			if (continuation != null)
			{
				// Run inline.
				continuation();
			}
		}
		else
		{
			_continuation = continuation;
		}
	}

	/// <summary>
	/// await async.
	/// </summary>
	/// <returns></returns>
	public GraphContext GetAwaiter()
	{
		return this;
	}

	/// <summary>
	/// Adds a waiter.
	/// </summary>
	public void AddWaiter()
	{
		Interlocked.Increment(ref _waitingFor);
	}

	/// <summary>
	/// Removes a waiter. If it becomes 0 then the continuation is permitted.
	/// </summary>
	public void RemoveWaiter()
	{
		bool isComplete = Interlocked.Decrement(ref _waitingFor) == 0;

		if (isComplete)
		{
			var c = _continuation;
			_continuation = null;
			if (c != null)
			{
				c();
			}
		}
	}
}