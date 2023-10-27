
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Api.Startup
{
	/// <summary>
	/// ID Collector.
	/// </summary>
	public class IDCollector
	{
		/// <summary>
		/// The fieldInfo for NextCollector.
		/// </summary>
		private static FieldInfo _nextCollectorField;

		/// <summary>
		/// The fieldInfo for NextCollector.
		/// </summary>
		public static FieldInfo NextCollectorFieldInfo
		{
			get
			{
				if (_nextCollectorField == null)
				{
					_nextCollectorField = typeof(IDCollector).GetField("NextCollector");
				}

				return _nextCollectorField;
			}
		}

		/// <summary>
		/// Next collector.
		/// </summary>
		public IDCollector NextCollector;

		/// <summary>
		/// The field that pools collectors of this type.
		/// </summary>
		public ContentField Pool;

		/// <summary>
		/// Collects a field value from the given entity.
		/// </summary>
		/// <param name="entity"></param>
		public virtual void Collect(object entity)
		{
			
		}

		/// <summary>
		/// Release collector to pool.
		/// </summary>
		public virtual void Release()
		{

		}
	}

	/// <summary>
	/// Used by dynamic includes. Holds multiple sub-collectors based on reading a type field.
	/// </summary>
	public class MultiIdCollector : IDCollector
	{
		/// <summary>
		/// The number of slots used in the CollectorsByType set.
		/// </summary>
		public int CollectorFill;

		/// <summary>
		/// A buffer of collectors by type.
		/// </summary>
		public IDCollectorWithType[] CollectorsByType = new IDCollectorWithType[2];

		// The generated Collect method will read the ID (a ulong) and the type (a string)
		// Then call Add(type, id)

		/// <summary>
		/// Releases the collectors.
		/// </summary>
		public override void Release()
		{
			for (var i = 0; i < CollectorFill; i++)
			{
				var collector = CollectorsByType[i].Collector;

				// Release its buffers:
				collector.Release();

				// And put it in the relevant pool:
				if (collector is IDCollector<uint> uintCollector)
				{
					IDCollector<uint>.ReleaseGenericCollector(uintCollector);
				}
				else
				{
					IDCollector<ulong>.ReleaseGenericCollector((IDCollector<ulong>)collector);
				}
			}

			CollectorFill = 0;

			// Re-add to this pool:
			if (Pool != null)
			{
				Pool.AddToPool(this);
			}

		}

		/// <summary>
		/// Adds the given typed ID to the set.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="id"></param>
		public void Add(string type, ulong id)
		{
			// is the type already in here?
			IDCollector collector = null;
			AutoService svc = null;

			for (var i = 0; i < CollectorFill; i++)
			{
				svc = CollectorsByType[i].Service;
				if (svc.EntityName == type)
				{
					// Got it
					collector = CollectorsByType[i].Collector;
					break;
				}
			}

			if (collector == null)
			{
				// Add it now
				svc = Services.Get(type + "Service");

				if (svc == null)
				{
					// Unknown type - ignore it.
					return;
				}

				if (CollectorFill == CollectorsByType.Length)
				{
					// Add 5 slots:
					Array.Resize(ref CollectorsByType, CollectorFill + 5);
				}

				// Rent a collector based on the service's ID type.
				if (svc.IdType == typeof(uint))
				{
					collector = IDCollector<uint>.RentGenericCollector();
				}
				else if (svc.IdType == typeof(ulong))
				{
					collector = IDCollector<ulong>.RentGenericCollector();
				}

				CollectorsByType[CollectorFill] = new IDCollectorWithType() {
					Collector = collector,
					Service = svc
				};

				CollectorFill++;

			}

			if (svc.IdType == typeof(uint))
			{
				((IDCollector<uint>)collector).Add((uint)id);
			}
			else if (svc.IdType == typeof(ulong))
			{
				((IDCollector<ulong>)collector).Add(id);
			}
		}

	}

	/// <summary>
	/// Holds a collector for a particular service.
	/// </summary>
	public struct IDCollectorWithType
	{
		/// <summary>
		/// The collector. Will be an IDCollector[ID] where typeof(ID) == Service.IdType.
		/// </summary>
		public IDCollector Collector;
		/// <summary>
		/// The service being collected for.
		/// </summary>
		public AutoService Service;
	}

	/// <summary>
	/// ID collector enumeration cursor.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public struct IDCollectorEnum<T> where T : struct
	{
		/// <summary>
		/// Current block.
		/// </summary>
		public IDBlock<T> Block;

		/// <summary>
		/// Fill of the last block.
		/// </summary>
		public int LastBlockFill;

		/// <summary>
		/// Index in the current block. Maxes at 64, then block is advanced to the next one.
		/// </summary>
		public int Index;

		/// <summary>
		/// True if there's more.
		/// </summary>
		/// <returns></returns>
		public bool HasMore()
		{
			if (Block == null || Block.Next == null)
			{
				// We're in the last block. Index must be less than the filled amount.
				return Index < LastBlockFill;
			}

			return true;
		}

		/// <summary>
		/// Reads the current value and advances by one.
		/// </summary>
		/// <returns></returns>
		public T Current()
		{
			var result = Block.Entries[Index++];

			if (Index == 64 && Block.Next != null)
			{
				Index = 0;
				Block = Block.Next;
			}

			return result;
		}
	}

	/// <summary>
	/// Collects IDs of the given type. Uses a pool of buffers for fast, non-allocating performance.
	/// The ID collector itself can also be pooled.
	/// </summary>
	public class IDCollector<T>: IDCollector, IEnumerable<T> where T:struct, IEquatable<T>, IComparable<T>
	{
		/// <summary>
		/// Generic pool lock
		/// </summary>
		private static object GenericCollectorPoolLock = new object();

		/// <summary>
		/// First ID collector in the pool for this field.
		/// </summary>
		private static IDCollector<T> FirstInGenericPool;

		/// <summary>
		/// Global pool of non-field specific uint collectors.
		/// </summary>
		/// <returns></returns>
		public static IDCollector<T> RentGenericCollector()
		{
			IDCollector<T> instance = null;

			lock (GenericCollectorPoolLock)
			{
				if (FirstInGenericPool != null)
				{
					// Pop from the pool:
					instance = FirstInGenericPool;
					FirstInGenericPool = (IDCollector<T>)instance.NextCollector;
				}
			}

			if (instance == null)
			{
				// Instance one:
				instance = new IDCollector<T>();
			}

			instance.NextCollector = null;
			return instance;
		}

		/// <summary>
		/// Global pool of non-field specific ulong collectors.
		/// </summary>
		/// <returns></returns>
		public static void ReleaseGenericCollector(IDCollector<T> collector)
		{
			lock (GenericCollectorPoolLock)
			{
				collector.NextCollector = FirstInGenericPool;
				FirstInGenericPool = collector;
			}
		}

		/// <summary>
		/// Linked list of blocks in this collector.
		/// </summary>
		public IDBlock<T> First;
		/// <summary>
		/// Linked list of blocks in this collector.
		/// </summary>
		public IDBlock<T> Last;

		/// <summary>
		/// Number of full blocks. Id count = (FullBlockCount * 64) + Count
		/// </summary>
		private int FullBlockCount;

		/// <summary>
		/// Current block fill.
		/// </summary>
		private int CurrentFill;

		/// <summary>
		/// True if there's exactly 1 entry.
		/// </summary>
		public bool OneEntry {
			get {
				return CurrentFill == 1;
			}
		}

		/// <summary>
		/// Total number added so far.
		/// </summary>
		public int Count {
			get {
				return (FullBlockCount * 64) + CurrentFill;
			}
		}

		/// <summary>
		/// True if any value in the collector matches the given one.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool MatchAny(T id)
		{
			var en = GetNonAllocEnumerator();
			while (en.HasMore())
			{
				var toCheck = en.Current();

				if (id.Equals(toCheck))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets a non-alloc enumeration tracker. Only use this if 
		/// </summary>
		/// <returns></returns>
		public IDCollectorEnum<T> GetNonAllocEnumerator()
		{
			return new IDCollectorEnum<T>()
			{
				Block = First,
				Index = 0,
				LastBlockFill = CurrentFill
			};
		}

		/// <summary>
		/// Quick ref to prev value to avoid a very common situation of adding the same ID repeatedly.
		/// </summary>
		private T PrevValue;

		/// <summary>
		/// Returns all ID blocks and the collector itself back to host pools.
		/// </summary>
		public override void Release()
		{
			if (Last == null)
			{
				// None anyway
				return;
			}

			// Re-add to this pool:
			if (Pool != null)
			{
				Pool.AddToPool(this);
			}

			ReleaseBlocks();

			FullBlockCount = 0;
			CurrentFill = 0;
		}

		/// <summary>
		/// Releases all ID blocks.
		/// </summary>
		public void ReleaseBlocks()
        {
			lock (IDBlockPool<T>.PoolLock)
			{
				Last.Next = IDBlockPool<T>.First;
				IDBlockPool<T>.First = First;
				Last = null;
				First = null;
			}
		}

		/// <summary>
		/// Release a single ID block
		/// </summary>
		public void ReleaseBlock(IDBlock<T> block)
        {
			lock (IDBlockPool<T>.PoolLock)
			{
				block.Next = IDBlockPool<T>.First;
				IDBlockPool<T>.First = block;
			}
		}

		/// <summary>
		/// Adds the given ID to the set.
		/// </summary>
		public void Add(T id)
		{
			if (First == null)
			{
				First = Last = IDBlockPool<T>.Get();
			}
			else
			{
				if (PrevValue.Equals(id) || id.Equals(default))
				{
					// Skip common scenario of everything being the same ID.
					return;
				}

				if (CurrentFill == 64)
				{
					// Add new block:
					var newBlock = IDBlockPool<T>.Get();
					Last.Next = newBlock;
					Last = newBlock;
					CurrentFill = 0;
					FullBlockCount++;
				}
			}

			Last.Entries[CurrentFill++] = id;
			PrevValue = id;
		}

		/// <summary>
		/// Adds the given ID to the set and sorts it for you.
		/// </summary>
		public void AddSorted(T id)
		{
			if (First == null)
			{
				First = Last = IDBlockPool<T>.Get();
				var index = CurrentFill++;

				Last.Entries[index] = id;
				PrevValue = id;

				// And done - we just added the first id.
				return;
			}
			else
			{
				// Pass this onto sort.
				Sort(First, id);
			}
		}

		/// <summary>
		/// Sorts the value into the set.
		/// </summary>
		public bool Sort(IDBlock<T> currentBlock, T currentValue, int currentIndex = 0, bool notMainBlock = false, int fillCount = 0) // pass notMainBlock = true if you are not sorting into the main IDBLock list
		{
			var sorted = false;
			// Let's iterate over the values of our currentBlock
			for(int i = currentIndex; i < currentBlock.Entries.Length; i++)
            {
				var curEntryValue = currentBlock.Entries[i];
				// If we hit a 0 value, we are on the very last value in the entry and the linked list.
				var curFill = CurrentFill;
				if(notMainBlock)
                {
					curFill = fillCount;
                }

				if (currentBlock.Next == null && i == curFill)
                {
					// Let's put our value here and we are done!
					currentBlock.Entries[i] = currentValue;

					if(!notMainBlock) // can we increment?
                    {
						CurrentFill++;
					}

					// And done!
					return true;
				}

				// Is our current value greater or lesser?
				if (currentValue.CompareTo(currentBlock.Entries[i]) < 0)
				{
					// Its lesser - We need to replace this value and move on up with the new value. 
					var newValue = currentBlock.Entries[i];
					currentBlock.Entries[i] = currentValue;

					sorted = Sort(currentBlock, newValue, i);
					break;
				}
			}

			// Are we sorted after this block? if not, let go to the next one.
			if(!sorted)
            {
				// Is there a next block?
				if(currentBlock.Next == null)
                {
					currentBlock.Next = IDBlockPool<T>.Get();
					
					if (!notMainBlock) // can we increment?
					{
						Last = currentBlock.Next;
						CurrentFill = 0;
						FullBlockCount++;
					}
				}

				sorted = Sort(currentBlock.Next, currentValue, 0);
            }

			return sorted;
        }

		/// <summary>
		/// Used to eliminate takes the sorted array or repetitions
		/// and turns it into a set with no repitions and removes values that don't have the 
		/// minimum repetiions.
		/// </summary>
		/// <param name="minRepetitions"></param>
		/// <param name="exact"></param> this value is true if minRepetitions must match the count exactly.
		public void Eliminate(int minRepetitions, bool exact = false)
		{
			if (First == null)
			{
				return;
			}

			T currentValue = default;
			int currentValueCount = 0;
			bool initialValueSet = false;
			var uneliminatedBlock = First;
			var uneliminatedFillCount = CurrentFill;
			CurrentFill = FullBlockCount = 0;

			First = Last = null;

			while (uneliminatedBlock != null)
			{
				var currentBlockFill = uneliminatedBlock.Next != null ? 64 : uneliminatedFillCount;
				for (var i = 0; i < currentBlockFill; i++)
				{
					var curEntry = uneliminatedBlock.Entries[i];

					if (initialValueSet && curEntry.Equals(currentValue))
					{
						// increment the count.
						currentValueCount++;
					}
					else
					{
						if (initialValueSet)
						{
							if ((!exact && currentValueCount >= minRepetitions) || (exact && currentValueCount == minRepetitions))
							{
								Add(currentValue);
							}
						}
                        else
                        {
							initialValueSet = true;
						}
						currentValue = curEntry;
						currentValueCount = 1;
					}
				}

				var next = uneliminatedBlock.Next;
				ReleaseBlock(uneliminatedBlock);

				uneliminatedBlock = next;
            }

			// Add the final value we just iterated over.
			if (initialValueSet)
			{
				if ((!exact && currentValueCount >= minRepetitions) || (exact && currentValueCount == minRepetitions))
				{
					Add(currentValue);
				}
			}
		}

		/// <summary>
		/// Used to debug the current state of the IDCollector
		/// </summary>
		public void Debug()
        {
			if(First == null)
            {
				return;
            }

			// Let's start going through our entries
			var sb = new StringBuilder();
			sb.Append("IDCollector debug: ");
			PrintBlock(First, sb);
			sb.Append("Full Block Count: ");
			sb.Append(FullBlockCount);
			sb.Append("\r\nCurrent Fill: ");
			sb.Append(CurrentFill);
			Log.Info("", sb.ToString());
		}

        /// <summary>
		/// 
		/// </summary>
		/// <param name="currentBlock"></param>
		public void PrintBlock(IDBlock<T> currentBlock, StringBuilder sb)
        {
			sb.Append("[");
			for (var i = 0; i < currentBlock.Entries.Length; i++)
			{
				if (currentBlock.Entries[i].Equals(0))
                {
					// We hit the end. - no need to continue
					sb.Append("]\r\n");
					return;
                }

				sb.Append(currentBlock.Entries[i]);
				sb.Append(", ");
			}
			sb.Append("]\r\n");

			// We safely hit the end - is there another block after this one?
			if (currentBlock.Next != null)
            {
				// There is an additional block
				PrintBlock(currentBlock.Next, sb);
            }
		}

		/// <summary>
		/// Gets an enumerator
		/// </summary>
		/// <returns></returns>
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			var enumerator  = GetNonAllocEnumerator();

			while (enumerator.HasMore())
			{
				var next = enumerator.Current();
				yield return next;
			}
		}

		/// <summary>
		/// Gets an enumerator
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			var enumerator = GetNonAllocEnumerator();

			while (enumerator.HasMore())
			{
				var next = enumerator.Current();
				yield return next;
			}
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class IDBlock<T> where T : struct
	{
		/// <summary>
		/// The IDs
		/// </summary>
		public T[] Entries = new T[64];

		/// <summary>
		/// Next in the chain.
		/// </summary>
		public IDBlock<T> Next;
	}

	/// <summary>
	/// This pools the allocation of blocks of IDs.
	/// </summary>
	public static class IDBlockPool<T> where T: struct
	{

		/// <summary>
		/// A lock for thread safety.
		/// </summary>
		public static readonly object PoolLock = new object();

		/// <summary>
		/// The current front of the pool.
		/// </summary>
		public static IDBlock<T> First;

		/// <summary>
		/// Get a block from the pool, or instances once.
		/// </summary>
		public static IDBlock<T> Get()
		{
			IDBlock<T> result;

			lock (PoolLock)
			{
				if (First == null)
				{
					return new IDBlock<T>();
				}

				result = First;
				First = result.Next;

			}

			result.Next = null;
			return result;
		}

	}

}