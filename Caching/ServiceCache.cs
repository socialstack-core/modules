using Api.Contexts;
using Api.Database;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.Startup{
	
	/// <summary>
	/// A cache for content which is frequently read but infrequently written.
	/// There is one of these per locale, stored by AutoService.
	/// </summary>
	public class ServiceCache<T> where T:DatabaseRow, new()
	{
		/// <summary>
		/// True if this cache is in lazy loading mode.
		/// </summary>
		public bool LazyLoadMode;

		/// <summary>
		/// The primary cached index.
		/// </summary>
		private Dictionary<int, T> Primary;

		/// <summary>
		/// The indices of the cache. The name of the index is the same as it is in the database 
		/// (usually just the field name for single field indices, or Field1_Field2_.. for multi-field indices).
		/// </summary>
		private Dictionary<string, ServiceCacheIndex<T>> Indices;

		/// <summary>
		/// A lookup of the indices by its ServiceCacheIndex.Id.
		/// </summary>
		private ServiceCacheIndex<T>[] IndexLookup;

		/// <summary>
		/// The secondary indices as a set.
		/// </summary>
		private List<ServiceCacheIndex<T>> SecondaryIndices;

		/// <summary>
		/// Runs when an item has been updated.
		/// If the first object is null and the second is not null, this is a first time add.
		/// If they're both set, it's an update.
		/// If the first is not null and the second is set, it's a removal of some kind.
		/// </summary>
		public Action<Context, T, T> OnChange;

		/// <summary>
		/// Fields of the type.
		/// </summary>
		private FieldMap Fields;

		/// <summary>
		/// Creates a new service cache using the given indices.
		/// </summary>
		/// <param name="indices"></param>
		public ServiceCache(List<DatabaseIndexInfo> indices)
		{
			Indices = new Dictionary<string, ServiceCacheIndex<T>>();
			SecondaryIndices = new List<ServiceCacheIndex<T>>();
			Fields = new FieldMap(typeof(T));
			IndexLookup = new ServiceCacheIndex<T>[indices.Count];

			var indexId = -1;

			foreach (var indexInfo in indices)
			{
				indexId++;

				if (indexInfo.Columns == null || 
					indexInfo.Columns.Length == 0 ||
					indexInfo.ColumnFields == null ||
					indexInfo.ColumnFields.Length != indexInfo.Columns.Length
				) {
					continue;
				}

				var firstCol = indexInfo.ColumnFields[0];
				var indexFieldType = firstCol.FieldType;

				if (indexInfo.Columns.Length > 1)
				{
					// Multi-column index. These are always string keys.
					indexFieldType = typeof(string);
				}

				ServiceCacheIndex<T> index = null;

				if (indexInfo.Unique)
				{
					// Unique index - it's a Dictionary<IndexFieldType, T>:
					var indexType = typeof(UniqueIndex<,>).MakeGenericType(new Type[] {
						typeof(T),
						indexFieldType
					});

					index = Activator.CreateInstance(indexType) as ServiceCacheIndex<T>;
				}
				else
				{
					// Non-unique index - it's a Dictionary<IndexFieldType, LinkedListOfT>
					Console.WriteLine("[NOTICE] Non-unique cache indices are not currently supported.");
					continue;
				}

				// Add to fast lookup:
				IndexLookup[indexId] = index;
				index.Id = indexId;

				if (indexInfo.IndexName == "Id")
				{
					index.Primary = true;

					// Primary - grab the underlying dictionary ref:
					Primary = index.GetUnderlyingStructure() as Dictionary<int, T>;
				}
				else
				{
					SecondaryIndices.Add(index);
				}

				if (indexInfo.Columns.Length == 1)
				{
					index.OneColumn = firstCol;
				}
				else
				{
					index.Columns = indexInfo.ColumnFields;
				}
				Indices[indexInfo.IndexName] = index;
			}

			if (Primary == null)
			{
				Primary = new Dictionary<int, T>();
			}
		}

		/// <summary>
		/// Gets the ID of the index with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public int GetIndexId(string name)
		{
			if (Indices.TryGetValue(name, out ServiceCacheIndex<T> value))
			{
				return value.Id;
			}
			return -1;
		}

		/// <summary>
		/// Gets an object with the given key value from the given index.
		/// </summary>
		public T GetUsingIndex(int indexId, object key)
		{
			var indexToUse = IndexLookup[indexId];
			if (indexToUse == null)
			{
				return null;
			}

			var result = indexToUse.Get(key);
			if (result == null)
			{
				return null;
			}

			return Clone(result);
		}

		/// <summary>
		/// Attempts to get the object with the given ID from the cache.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="clone">True if the object (specifically, its known fields) should be cloned.</param>
		/// <returns></returns>
		public T Get(int id, bool clone = true)
		{
			if (Primary.TryGetValue(id, out T value) && clone)
			{
				value = Clone(value);
			}
			return value;
		}

		/// <summary>
		/// Clones the given value's core fields. This is to exactly mimic an original database response.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private T Clone(T value)
		{
			// Create a new object by cloning it and returning that.
			// This is important to avoid race conditions by event handlers on popular content, as well as during updates.
			// For example, when you update an object, it first gets it and then applies changed fields.
			// That object, however, may have originated from the cache and is now being directly manipulated.
			var result = new T();

			// For each field in the type:
			for (var i = 0; i < Fields.Count; i++)
			{
				var fieldMeta = Fields[i];

				// Transfer the field:
				fieldMeta.TargetField.SetValue(
					result,
					fieldMeta.TargetField.GetValue(value)
				);
			}

			return result;
		}

		/// <summary>
		/// Attempt to get an index by the given index name. This is usually the exact name of the column,  case sensitive.
		/// For multi-column indices, they're separated by _ (For example, "FirstName_LastName").
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public ServiceCacheIndex<T> GetIndex<U>(string name)
		{
			Indices.TryGetValue(name, out ServiceCacheIndex<T> value);
			return value;
		}

		/// <summary>
		/// The given entity was created, updated or just needs to be added to the cache because it was lazy loaded.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="entry"></param>
		public void Add(Context context, T entry)
		{
			if (entry == null || entry.Id <= 0)
			{
				return;
			}

			// Remove:
			var prev = Remove(entry.Id, false);

			// Add:
			AddInternal(entry);

			OnChange?.Invoke(context, prev, entry);
		}

		/// <summary>
		/// Remove the given entry by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		public void Remove(Context context, int id)
		{
			var prev = Remove(id, true);

			if (prev != null)
			{
				OnChange?.Invoke(context, prev, null);
			}
		}

		/// <summary>
		/// Removes the given object from the index.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="fromPrimary">Also remove from the primary</param>
		private T Remove(int id, bool fromPrimary)
		{
			if (!Primary.TryGetValue(id, out T value))
			{
				// Not cached anyway
				return null;
			}

			if (fromPrimary)
			{
				Primary.Remove(id);
			}

			// Remove the given value from all indices.
			foreach (var index in SecondaryIndices)
			{
				index.Remove(value);
			}

			return value;
		}

		/// <summary>
		/// Adds the given entry to the index.
		/// </summary>
		/// <param name="entry"></param>
		private void AddInternal(T entry)
		{
			// Add to primary index:
			Primary[entry.Id] = entry;

			// Add to any secondary indices:
			foreach (var index in SecondaryIndices)
			{
				index.Add(entry);
			}
		}
	}
	
}