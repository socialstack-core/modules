using Api.Database;
using Api.Translate;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace Api.Startup{
	
	/// <summary>
	/// </summary>
	public abstract class ServiceCacheIndex<T> where T : class
	{
		/// <summary>
		/// The ID of this index. Does not persist across restarts etc.
		/// </summary>
		public int Id;

		/// <summary>
		/// True if this is the primary index.
		/// </summary>
		public bool Primary;

		/// <summary>
		/// Gets the underlying datastructure, usually a Dictionary.
		/// </summary>
		/// <returns></returns>
		public virtual object GetUnderlyingStructure()
		{
			return null;
		}

		/// <summary>
		/// Adds the given object to this index. Do not call this directly - add or remove an object from the ServiceCache instead.
		/// </summary>
		/// <param name="entry"></param>
		internal virtual void Add(T entry)
		{
		}

		/// <summary>
		/// Removes the given entry from the index. Do not call this directly - add or remove an object from the ServiceCache instead.
		/// </summary>
		/// <param name="entry"></param>
		internal virtual void Remove(T entry)
		{
		}

	}

	/// <summary>
	/// A service cache index which also declares its key type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="U"></typeparam>
	public abstract class ServiceCacheIndex<T, U> : ServiceCacheIndex<T>
		where T : class
	{

		/// <summary>
		/// Child index metadata to pass to a followup index when they form a tree.
		/// </summary>
		public ChildIndexMeta ChildIndexMeta;


		/// <summary>
		/// Creates a new service cache index with the given meta.
		/// </summary>
		/// <param name="childIndexMeta"></param>
		public ServiceCacheIndex(ChildIndexMeta childIndexMeta)
		{
			ChildIndexMeta = childIndexMeta;
		}

		/// <summary>
		/// Gets the key value for the given entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns></returns>
		public virtual U GetKeyValue(T entry)
		{
			throw new NotImplementedException();
		}

	}

	/// <summary>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="U"></typeparam>
	public class UniqueIndex<T, U> : ServiceCacheIndex<T, U> where T : class {

		private ConcurrentDictionary<U, T> Index = new ConcurrentDictionary<U, T>();

		/// <summary>
		/// Create a new unique index.
		/// </summary>
		/// <param name="childIndexMeta"></param>
		public UniqueIndex(ChildIndexMeta childIndexMeta) : base(childIndexMeta)
		{
		}

		/// <summary>
		/// Gets the underlying datastructure, usually a ConcurrentDictionary.
		/// </summary>
		/// <returns></returns>
		public override object GetUnderlyingStructure()
		{
			return Index;
		}

		/// <summary>
		/// Adds the given object to this index. Do not call this directly - add or remove an object from the ServiceCache instead.
		/// </summary>
		/// <param name="entry"></param>
		internal override void Add(T entry)
		{
			var keyValue = GetKeyValue(entry);

			if (keyValue == null)
			{
				// Can't add if key is null.
				Console.WriteLine("[WARN] Skipped adding object to a cache index because it had a null field (it was a " + entry.GetType() + ").");
				return;
			}

			// Add is used here as an exception if the given key value is already in the index.
			Index.TryAdd(keyValue, entry);
		}

		/// <summary>
		/// Removes the given entry from the index. Do not call this directly - add or remove an object from the ServiceCache instead.
		/// </summary>
		/// <param name="entry"></param>
		internal override void Remove(T entry)
		{
			var keyValue = GetKeyValue(entry);

			// Add is used here as an exception if the given key value is already in the index.
			Index.Remove(keyValue, out _);
		}

	}

	/// <summary>
	/// single linked list node for non-unique indices.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class IndexLinkNode<T> {
		/// <summary>
		/// Current node.
		/// </summary>
		public T Current;
		/// <summary>
		/// Next node.
		/// </summary>
		public IndexLinkNode<T> Next;
	}

	/// <summary>
	/// Tracks first and last node in a linked list non-unique index.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class IndexLinkedList<T> {

		/// <summary>
		/// First node.
		/// </summary>
		public IndexLinkNode<T> First;
		/// <summary>
		/// Last node.
		/// </summary>
		public IndexLinkNode<T> Last;

		/// <summary>
		/// Count of nodes in this list. Usually small.
		/// </summary>
		public int Count;
	}

	/// <summary>
	/// A non-alloc iterator for indexes.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public struct IndexEnum<T>
	{
		/// <summary>
		/// Current iteration node.
		/// </summary>
		public IndexLinkNode<T> Node;

		/// <summary>
		/// True if there's more.
		/// </summary>
		/// <returns></returns>
		public bool HasMore()
		{
			return Node != null;
		}

		/// <summary>
		/// Gets current value and advances.
		/// </summary>
		/// <returns></returns>
		public T Current()
		{
			var res = Node.Current;
			Node = Node.Next;
			return res;
		}
	}

	/// <summary>
	/// Used for multi-key indices, this is used to represent the upper level index mapping to a lower level one.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="UA"></typeparam>
	/// <typeparam name="UB"></typeparam>
	public class IndexIndex<T, UA, UB> : ServiceCacheIndex<T, UA> where T : class
	{
		private ConcurrentDictionary<UA, ServiceCacheIndex<T, UB>> Index = new ConcurrentDictionary<UA, ServiceCacheIndex<T, UB>>();

		/// <summary>
		/// Create a new index of indices.
		/// </summary>
		/// <param name="childIndexMeta"></param>
		public IndexIndex(ChildIndexMeta childIndexMeta) : base(childIndexMeta)
		{
		}

		/// <summary>
		/// Gets the underlying datastructure, usually a ConcurrentDictionary.
		/// </summary>
		/// <returns></returns>
		public override object GetUnderlyingStructure()
		{
			return Index;
		}
		
		internal override void Add(T entry)
		{
			var localKey = GetKeyValue(entry);

			if (localKey == null)
			{
				// Can't add if key is null.
				Console.WriteLine("[WARN] Skipped adding object to a cache index because it had a null field (it was a " + entry.GetType() + ").");
				return;
			}

			if (!Index.TryGetValue(localKey, out ServiceCacheIndex<T, UB> svcCache))
			{
				// Instance an svcCache:
				svcCache = (ServiceCacheIndex<T, UB>)Activator.CreateInstance(ChildIndexMeta.ChildType, ChildIndexMeta.Meta);
				Index[localKey] = svcCache;
			}

			svcCache.Add(entry);
		}
	}

	/// <summary>
	/// Used for describing a tree of multi-column indices.
	/// </summary>
	public class ChildIndexMeta {
		/// <summary>
		/// The child index type.
		/// </summary>
		public Type ChildType;
		/// <summary>
		/// The meta to pass to it.
		/// </summary>
		public ChildIndexMeta Meta;
	}

	/// <summary>
	/// Non unique index.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="U"></typeparam>
	public class NonUniqueIndex<T, U> : ServiceCacheIndex<T, U> where T : class
	{
		private ConcurrentDictionary<U, IndexLinkedList<T>> Index = new ConcurrentDictionary<U, IndexLinkedList<T>>();

		/// <summary>
		/// Create a new index of indices.
		/// </summary>
		/// <param name="childIndexMeta"></param>
		public NonUniqueIndex(ChildIndexMeta childIndexMeta) : base(childIndexMeta)
		{
		}

		/// <summary>
		/// Underlying index dictionary
		/// </summary>
		public ConcurrentDictionary<U, IndexLinkedList<T>> Dictionary
		{
			get
			{
				return Index;
			}
		}

		/// <summary>
		/// Gets the underlying datastructure, usually a ConcurrentDictionary.
		/// </summary>
		/// <returns></returns>
		public override object GetUnderlyingStructure()
		{
			return Index;
		}

		/// <summary>
		/// Gets the linked list of values for a particular key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public IndexLinkedList<T> GetIndexList(U key)
		{
			Index.TryGetValue(key, out IndexLinkedList<T> value);
			return value;
		}

		/// <summary>
		/// Gets a non-alloc enumeration tracker.
		/// </summary>
		/// <returns></returns>
		public IndexEnum<T> GetEnumeratorFor(U key)
		{
			Index.TryGetValue(key, out IndexLinkedList<T> value);

			return new IndexEnum<T>()
			{
				Node = value == null ? null : value.First
			};
		}
		
		/// <summary>
		/// Adds the given object to this index. Do not call this directly - add or remove an object from the ServiceCache instead.
		/// </summary>
		/// <param name="entry"></param>
		internal override void Add(T entry)
		{
			var keyValue = GetKeyValue(entry);

			if (keyValue == null)
			{
				// Can't add if key is null.
				Console.WriteLine("[WARN] Skipped adding object to a cache index because it had a null field (it was a " + entry.GetType() + ").");
				return;
			}

			var linkNode = new IndexLinkNode<T>() { Current = entry };

			if (Index.TryGetValue(keyValue, out IndexLinkedList<T> value))
			{
				lock (value)
				{
					value.Last.Next = linkNode;
					value.Last = linkNode;
					value.Count++;
				}
			}
			else
			{
				value = new IndexLinkedList<T>()
				{
					First = linkNode,
					Last = linkNode,
					Count = 1
				};

				Index[keyValue] = value;
			}
		}

		/// <summary>
		/// Removes the given entry from the index. Do not call this directly - add or remove an object from the ServiceCache instead.
		/// </summary>
		/// <param name="entry"></param>
		internal override void Remove(T entry)
		{
			var keyValue = GetKeyValue(entry);

			if (keyValue == null)
			{
				// Can't remove if key is null.
				return;
			}

			// Cache removal is a relatively expensive operation.
			if (Index.TryGetValue(keyValue, out IndexLinkedList<T> value))
			{
				IndexLinkNode<T> prev = null;
				var node = value.First;

				while (node != null)
				{
					if (node.Current == entry)
					{
						// Found it.

						lock (value)
						{
							if (prev == null)
							{
								value.First = node.Next;
							}
							else
							{
								prev.Next = node.Next;
							}

							if (node.Next == null)
							{
								value.Last = prev;
							}

							value.Count--;
						}

						break;
					}

					prev = node;
					node = node.Next;
				}

				if (value.First == null)
				{
					// Empty list - remove key entirely:
					Index.Remove(keyValue, out _);
				}
			}
		}

	}

}