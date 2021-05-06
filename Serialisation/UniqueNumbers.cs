
using System;
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
	public class IDCollector<T>: IDCollector where T:struct, IEquatable<T>
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
			var en = GetEnumerator();
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
		public IDCollectorEnum<T> GetEnumerator()
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