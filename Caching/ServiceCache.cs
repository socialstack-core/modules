using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.Startup{
	
	/// <summary>
	/// A cache for content which is frequently read but infrequently written.
	/// There is one of these per locale, stored by AutoService.
	/// </summary>
	public class ServiceCache<T, PT> 
		where T: Content<PT>
		where PT:struct
	{
		/// <summary>
		/// True if this cache is in lazy loading mode.
		/// </summary>
		public bool LazyLoadMode;

		/// <summary>
		/// The raw index - this is for caches for a particular locale, and holds "raw" objects 
		/// (as they are in the database, with blanks where the default translation should apply).
		/// </summary>
		private Dictionary<PT, T> Raw = new Dictionary<PT, T>();

		/// <summary>
		/// The primary cached index. If localised, this holds objects that have had the primary locale applied to them.
		/// </summary>
		private Dictionary<PT, T> Primary;

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

				ServiceCacheIndex<T> index;

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

					var indexType = typeof(NonUniqueIndex<,>).MakeGenericType(new Type[] {
						typeof(T),
						indexFieldType
					});

					index = Activator.CreateInstance(indexType) as ServiceCacheIndex<T>;
				}

				// Add to fast lookup:
				IndexLookup[indexId] = index;
				index.Id = indexId;

				if (indexInfo.IndexName == "Id")
				{
					index.Primary = true;

					// Primary - grab the underlying dictionary ref:
					Primary = index.GetUnderlyingStructure() as Dictionary<PT, T>;
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
				Primary = new Dictionary<PT, T>();
			}
		}

		/// <summary>
		/// Get the primary index.
		/// </summary>
		/// <returns></returns>
		public Dictionary<PT, T> GetPrimary()
		{
			return Primary;
		}

		/// <summary>
		/// Get the raw lookup.
		/// </summary>
		/// <returns></returns>
		public Dictionary<PT, T> GetRaw()
		{
			return Raw;
		}

		/// <summary>
		/// Gets list with total number of results. Regularly used by pagination.
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public ListWithTotal<T> ListWithTotal(Filter<T> filter, List<ResolvedValue> values)
		{
			var results = List(filter, values, out int total);
			return new ListWithTotal<T>()
			{
				Results = results,
				Total = total
			};
		}

		/// <summary>
		/// List objects with the given filter.
		/// </summary>
		/// <param name="filter">Can be null.</param>
		/// <param name="values">Resolved value set</param>
		/// <param name="total">Total number of results, regardless of any pagination happening.</param>
		/// <returns></returns>
		public List<T> List(Filter<T> filter, List<ResolvedValue> values, out int total)
		{
			var set = new List<T>();

			if (filter == null || !filter.HasContent)
			{
				// Everything in whatever order the PK returns them in. Can be paginated.
				lock(Primary){
					foreach (var kvp in Primary)
					{
						set.Add(kvp.Value);
					}
				}
			}
			else
			{
				FilterFieldEqualsSet setNode = filter.Nodes.Count == 1 ? filter.Nodes[0] as FilterFieldEqualsSet : null;

				if (setNode != null && setNode.Field == "Id")
				{
					// Very common special case where we're getting a specific set of rows.
					// Directly hit the primary key to return results.
					foreach (var id in setNode.Values)
					{
						PT intId = default;

						if (id is PT pT)
						{
							intId = pT;
						}
						else
						{
							// Also a type conversion
							// Currently when this happens, id is a long, and PT is int.
							var idObj = (object)((int)((long)id));
							intId = (PT)idObj;
						}

						if (Primary.TryGetValue(intId, out T value))
						{
							set.Add(value);
						}
					}
				}
				else
				{
					var rootNode = filter.Construct();
					lock(Primary){
						foreach (var kvp in Primary)
						{
							if (rootNode.Matches(values, kvp.Value))
							{
								set.Add(kvp.Value);
							}
						}
					}
				}
			}

			if (filter != null && filter.Sorts != null && filter.Sorts.Count > 0)
			{
				HandleSorting(set, filter.Sorts);
			}

			total = set.Count;

			// Limit happens after sorting:
			if (filter != null && filter.PageSize != 0)
			{
				var rowStart = filter == null ? 0 : filter.PageIndex * filter.PageSize;

				if (rowStart >= set.Count)
				{
					return new List<T>();
				}

				var count = set.Count - rowStart;

				if (count > filter.PageSize)
				{
					count = filter.PageSize;
				}
				
				
				
				return set.GetRange(rowStart, count);
			}

			return set;
		}

		private void HandleSorting(List<T> set, List<FilterSort> sorts)
		{
			foreach (var sort in sorts)
			{
				if (sort.FieldInfo == null)
				{
					sort.FieldInfo = sort.Type.GetField(char.IsLower(sort.Field[0]) ? char.ToUpper(sort.Field[0]) + sort.Field.Substring(1) : sort.Field);

					if (sort.FieldInfo == null)
					{
						throw new Exception(sort.Field + " sort field doesn't exist on type '" + sort.Type.Name + "'");
					}
				}
			}

			set.Sort((a, b) => {

				for (var i = 0; i < sorts.Count; i++)
				{
					var sort = sorts[i];

					var valA = sort.FieldInfo.GetValue(a);
					var valB = sort.FieldInfo.GetValue(b);
					int comparison;

					if (valA == null)
					{
						comparison = (valB == null) ? 0 : 1;
					}
					else
					{
						comparison = (valA as IComparable).CompareTo(valB);
					}
					
					// If a and b compare equal, proceed to check the next sort node
					// Otherwise, return the compare value
					if (comparison != 0)
					{
						if (sort.Ascending)
						{
							return comparison;
						}
						else
						{
							// Invert:
							return -comparison;
						}
					}
				}

				return 0;
			});

		}

		/// <summary>
		/// The number of entries in the cache.
		/// </summary>
		/// <returns></returns>
		public int Count()
		{
			return Primary.Count;
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

			return result;
		}

		/// <summary>
		/// Attempts to get the raw object with the given ID from the cache.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public T GetRaw(PT id)
		{
			Raw.TryGetValue(id, out T value);
			return value;
		}

		/// <summary>
		/// Attempts to get the object with the given ID from the cache.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public T Get(PT id)
		{
			Primary.TryGetValue(id, out T value);
			return value;
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
		/// <param name="rawEntry"></param>
		public void Add(Context context, T entry, T rawEntry)
		{
			if (entry == null)
			{
				return;
			}

			// Remove:
			var prev = Remove(entry.GetId(), false);

			// Add:
			AddInternal(entry, rawEntry);

			OnChange?.Invoke(context, prev, entry);
		}

		/// <summary>
		/// Remove the given entry by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		public void Remove(Context context, PT id)
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
		private T Remove(PT id, bool fromPrimary)
		{
			if (Primary == null || !Primary.TryGetValue(id, out T value))
			{
				// Not cached anyway
				return null;
			}

			if (fromPrimary)
			{
				lock(Primary){
					Primary.Remove(id);
					Raw.Remove(id);
				}
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
		/// <param name="rawEntry"></param>
		private void AddInternal(T entry, T rawEntry)
		{
			lock(Primary){
				// Add to primary index, and the raw backing index:
				var id = entry.GetId();
				Primary[id] = entry;
				Raw[id] = rawEntry;
			}
			
			// Add to any secondary indices:
			foreach (var index in SecondaryIndices)
			{
				index.Add(entry);
			}
		}
	}
	
}