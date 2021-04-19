using Api.Database;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.Startup{
	
	/// <summary>
	/// </summary>
	public class ServiceCacheIndex<T> where T : class
	{
		/// <summary>
		/// The ID of this index. Does not persist across restarts etc.
		/// </summary>
		public int Id;
		/// <summary>
		/// The columns that are used to form the value for this index.
		/// </summary>
		public System.Reflection.FieldInfo[] Columns;

		/// <summary>
		/// If single column, the fieldInfo.
		/// </summary>
		public System.Reflection.FieldInfo OneColumn;

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
		/// Looks up a value for the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public virtual T Get(object key)
		{
			return null;
		}

		/// <summary>
		/// Look up a non-unique result set for the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public virtual IEnumerable<T> GetAll(object key)
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
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="U"></typeparam>
	public class UniqueIndex<T, U> : ServiceCacheIndex<T> where T : class {

		private Dictionary<U, T> Index = new Dictionary<U, T>();

		/// <summary>
		/// Gets the underlying datastructure, usually a Dictionary.
		/// </summary>
		/// <returns></returns>
		public override object GetUnderlyingStructure()
		{
			return Index;
		}

		/// <summary>
		/// Look up a non-unique result set for the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public override IEnumerable<T> GetAll(object key)
		{
			Index.TryGetValue((U)key, out T result);
			yield return result;
		}

		/// <summary>
		/// Looks up a value for the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public override T Get(object key)
		{
			Index.TryGetValue((U)key, out T result);
			return result;
		}

		/// <summary>
		/// Gets the key value for the given entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns></returns>
		public object GetKeyValue(T entry)
		{
			if (OneColumn != null)
			{
				return OneColumn.GetValue(entry);
			}

			var keyValue = "";

			for (var i = 0; i < Columns.Length; i++)
			{
				if (i != 0)
				{
					keyValue += "_";
				}
				keyValue += Columns[i].GetValue(entry).ToString();
			}

			return keyValue;
		}

		/// <summary>
		/// Adds the given object to this index. Do not call this directly - add or remove an object from the ServiceCache instead.
		/// </summary>
		/// <param name="entry"></param>
		internal override void Add(T entry)
		{
			var keyValue = (U)GetKeyValue(entry);

			// Add is used here as an exception if the given key value is already in the index.
			Index.Add(keyValue, entry);
		}

		/// <summary>
		/// Removes the given entry from the index. Do not call this directly - add or remove an object from the ServiceCache instead.
		/// </summary>
		/// <param name="entry"></param>
		internal override void Remove(T entry)
		{
			var keyValue = (U)GetKeyValue(entry);

			// Add is used here as an exception if the given key value is already in the index.
			Index.Remove(keyValue);
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
	/// Non unique index.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="U"></typeparam>
	public class NonUniqueIndex<T, U> : ServiceCacheIndex<T> where T : class
	{

		private Dictionary<U, IndexLinkedList<T>> Index = new Dictionary<U, IndexLinkedList<T>>();

		/// <summary>
		/// Gets the underlying datastructure, usually a Dictionary.
		/// </summary>
		/// <returns></returns>
		public override object GetUnderlyingStructure()
		{
			return Index;
		}

		/// <summary>
		/// Gets a non-alloc enumeration tracker. Only use this if 
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
		/// Gets the key value for the given entry.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns></returns>
		public object GetKeyValue(T entry)
		{
			if (OneColumn != null)
			{
				return OneColumn.GetValue(entry);
			}

			var keyValue = "";

			for (var i = 0; i < Columns.Length; i++)
			{
				if (i != 0)
				{
					keyValue += "_";
				}
				keyValue += Columns[i].GetValue(entry).ToString();
			}

			return keyValue;
		}

		/// <summary>
		/// Adds the given object to this index. Do not call this directly - add or remove an object from the ServiceCache instead.
		/// </summary>
		/// <param name="entry"></param>
		internal override void Add(T entry)
		{
			var keyValue = (U)GetKeyValue(entry);

			var linkNode = new IndexLinkNode<T>() { Current = entry };

			if (Index.TryGetValue(keyValue, out IndexLinkedList<T> value))
			{
				lock (value)
				{
					value.Last.Next = linkNode;
					value.Last = linkNode;
				}
			}
			else
			{
				value = new IndexLinkedList<T>()
				{
					First = linkNode,
					Last = linkNode
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
			var keyValue = (U)GetKeyValue(entry);

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

						break;
					}

					prev = node;
					node = node.Next;
				}

				if (value.First == null)
				{
					// Empty list - remove key entirely:
					Index.Remove(keyValue);
				}
			}
		}

	}

}