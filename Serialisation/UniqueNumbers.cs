
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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
		/// Used to get the inverse of current value on stack
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public static bool NotStackValue(bool c)
        {
			return !c;
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

			lock (IDBlockPool<T>.PoolLock)
			{
				Last.Next = IDBlockPool<T>.First;
				IDBlockPool<T>.First = First;
				Last = null;
				First = null;
			}

			FullBlockCount = 0;
			CurrentFill = 0;
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

					Last = currentBlock.Next;
					if (!notMainBlock) // can we increment?
					{
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

			T currentValue = new T();
			var currentValueCount = 0;
			var currentBlock = First;
			var valueFillCount = 0;
			bool initialValueSet = false;
			bool fullyIterated = false;

			var newBlock = new IDBlock<T>();

			while (!fullyIterated)
			{
				// Let's iterate the current array
				for (int i = 0; i < currentBlock.Entries.Length; i++)
				{
					var curEntry = currentBlock.Entries[i];

					// Is our value 0? if so, we are at the end of the array
					if (currentBlock.Next == null && i == CurrentFill)
					{

						if ((!exact && currentValueCount >= minRepetitions && initialValueSet) || (exact && currentValueCount == minRepetitions && initialValueSet))
						{
							// Sort it into our newBlock
							Sort(newBlock, currentValue, 0, true, valueFillCount);
							valueFillCount++;
						}
						fullyIterated = true;
						break;
					}

					//  Is our curEntry the same as the currentValue?
					if (curEntry.Equals(currentValue))
					{
						// increment the count.
						currentValueCount++;
						continue;
					}

					// Its not, which means we need to handle our current value and set the new one.
					// Did we have enough of that value for it to qualify?
					if ((!exact && currentValueCount >= minRepetitions && initialValueSet) || (exact && currentValueCount == minRepetitions && initialValueSet))
					{
						// Sort it into our newBlock
						Sort(newBlock, currentValue, 0, true, valueFillCount);
						valueFillCount++;
					}

					// We took care of that value, move onto our new one
					currentValueCount = 1;
					currentValue = curEntry;
					initialValueSet = true;
				}
			}

			// Now that we have constructed our new block, let's release the old.
			Release();

			// Set First to our new Block
			First = newBlock;

			// Update count
			CurrentFill = valueFillCount % 64;
			FullBlockCount = valueFillCount / 64;

			// Set our Last Block
			Last = First;

            while (Last.Next != null)
            {
				Last = Last.Next;
            }

		}

		/// <summary>
		/// Used to debug the current state of the IDCollector
		/// </summary>
		public void Debug()
        {
			Console.WriteLine("IDCollector debug: ");

			if(First == null)
            {
				return;
            }

			// Let's start going through our entries
			PrintBlock(First);
			Console.Write("Full Block Count: ");
			Console.WriteLine(FullBlockCount);
			Console.Write("Current Fill: ");
			Console.WriteLine(CurrentFill);
		}

        /// <summary>
		/// 
		/// </summary>
		/// <param name="currentBlock"></param>
		public void PrintBlock(IDBlock<T> currentBlock)
        {
			Console.Write("[");
			for (var i = 0; i < currentBlock.Entries.Length; i++)
			{
				if (currentBlock.Entries[i].Equals(0))
                {
					// We hit the end. - no need to continue
					Console.WriteLine("]");
					return;
                }

				Console.Write(currentBlock.Entries[i] + ", ");
			}
			Console.WriteLine("]");

			// We safely hit the end - is there another block after this one?
			if (currentBlock.Next != null)
            {
				// There is an additional block
				PrintBlock(currentBlock.Next);
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