using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lumity.BlockChains;


/// <summary>
/// Used to track transactions that "this" node is waiting to complete.
/// </summary>
public class PendingTransaction : INotifyCompletion
{
	/// <summary>
	/// First pending txn in the pool.
	/// </summary>
	private static PendingTransaction FirstCached;

	/// <summary>
	/// Thread safety for the writer pool.
	/// </summary>
	private static readonly object _poolLock = new object();

	/// <summary>
	/// Gets a writer which builds a series of the buffers from this pool.
	/// </summary>
	/// <returns></returns>
	public static PendingTransaction GetPooled()
	{
		PendingTransaction result;

		lock (_poolLock)
		{
			if (FirstCached == null)
			{
				result = new PendingTransaction();
				return result;
			}

			result = FirstCached;
			FirstCached = result.Next;
		}

		return result;
	}

	/// <summary>
	/// Pending transactions form a linked list.
	/// </summary>
	public PendingTransaction Next;
	
	/// <summary>
	/// The timestamp of this txn. A node always generates unique ascending 
	/// timestamps such that this can be used to identify when a particular txn has completed.
	/// </summary>
	public ulong Timestamp;

	private bool _completed;

	/// <summary>
	/// True when the pending task is completed.
	/// </summary>
	public bool IsCompleted => _completed;

	/// <summary>
	/// The completion callback.
	/// </summary>
	private Action _callback;

	/// <summary>
	/// Resets this pending txn such that it can be awaited again.
	/// </summary>
	public void Reset()
	{
		_completed = false;
		_callback = null;
	}

	/// <summary>
	/// Most relevant object. This can be a Definition that was just created, a FieldDefinition or a particular entity instance.
	/// </summary>
	public object RelevantObject;
	
	/// <summary>
	/// The discovered transaction ID.
	/// </summary>
	public ulong TransactionId;

	/// <summary>
	/// True if the txn was valid.
	/// </summary>
	public bool Valid;

	/// <summary>
	/// Called when the pending transaction is done and the await function should then run.
	/// </summary>
	public void Done()
	{
		_completed = true;
		_callback?.Invoke();
	}

	/// <summary>
	/// The await result; just returns itself.
	/// </summary>
	/// <returns></returns>
	public PendingTransaction GetResult()
	{
		return (_completed) ? this : null;
	}

	/// <summary>
	/// Called when it is complete.
	/// </summary>
	/// <param name="callback"></param>
	public void OnCompleted(Action callback)
	{
		_callback = callback;
	}

	/// <summary>
	/// Used for awaiting the pending txn.
	/// </summary>
	/// <returns></returns>
	public PendingTransaction GetAwaiter()
	{
		return this;
	}

	/// <summary>Release this pending transaction object back to the pool.</summary>
	public void Release()
	{
		lock (_poolLock)
		{
			// Shove into the pool:
			Next = FirstCached;
			FirstCached = this;
		}
	}

}