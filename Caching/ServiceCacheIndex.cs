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

}