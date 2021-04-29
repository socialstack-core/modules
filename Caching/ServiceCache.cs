using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Results;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.Startup{
	
	/// <summary>
	/// A cache for content which is frequently read but infrequently written.
	/// There is one of these per locale, stored by AutoService.
	/// </summary>
	public class ServiceCache<T, PT> 
		where T: Content<PT>, new()
		where PT : struct, IConvertible, IEquatable<PT>
	{
		/// <summary>
		/// True if this cache is in lazy loading mode.
		/// </summary>
		public bool LazyLoadMode;

		/// <summary>
		/// The raw index - this is for caches for a particular locale, and holds "raw" objects 
		/// (as they are in the database, with blanks where the default translation should apply).
		/// </summary>
		private ConcurrentDictionary<PT, T> Raw = new ConcurrentDictionary<PT, T>();

		/// <summary>
		/// The primary cached index. If localised, this holds objects that have had the primary locale applied to them.
		/// </summary>
		private ConcurrentDictionary<PT, T> Primary;

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
					indexInfo.Columns.Length == 0
				) {
					continue;
				}

				var firstCol = indexInfo.Columns[0].FieldInfo;
				var indexFieldType = firstCol.FieldType;

				if (indexInfo.Columns.Length > 1)
				{
					// Multi-column index. These are always string keys.
					indexFieldType = typeof(string);
				}

				// Instance each one next:
				var index = indexInfo.CreateIndex<T>();

				// Add to fast lookup:
				IndexLookup[indexId] = index;
				index.Id = indexId;

				if (indexInfo.IndexName == "Id")
				{
					index.Primary = true;

					// Primary - grab the underlying dictionary ref:
					Primary = index.GetUnderlyingStructure() as ConcurrentDictionary<PT, T>;
				}
				else
				{
					SecondaryIndices.Add(index);
				}

				Indices[indexInfo.IndexName] = index;
			}

			if (Primary == null)
			{
				Primary = new ConcurrentDictionary<PT, T>();
			}
		}

		/// <summary>
		/// Get the primary index.
		/// </summary>
		/// <returns></returns>
		public ConcurrentDictionary<PT, T> GetPrimary()
		{
			return Primary;
		}

		/// <summary>
		/// Get the raw lookup.
		/// </summary>
		/// <returns></returns>
		public ConcurrentDictionary<PT, T> GetRaw()
		{
			return Raw;
		}

		/// <summary>
		/// Gets a list of results from the cache, calling the given callback each time one is discovered.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="queryPair">Must have both queries set.</param>
		/// <param name="onResult"></param>
		/// <param name="srcA"></param>
		/// <param name="srcB"></param>
		public async ValueTask<int> GetResults(Context context, QueryPair<T, PT> queryPair, Func<Context, T, int, object, object, ValueTask> onResult, object srcA, object srcB)
		{
			// FilterA and FilterB are never null.
			var filterA = queryPair.QueryA;
			var filterB = queryPair.QueryB;

			var total = 0;
			var includeTotal = filterA.IncludeTotal;

			// TODO: index selection is now possible to avoid these full scans.
			// a list allocation is required if a sort is specified but there is no sorted index.
			if (filterA.SortField != null)
			{
				var set = new List<T>();

				foreach (var kvp in Primary)
				{
					if (filterA.Match(context, kvp.Value, filterA) && filterB.Match(context, kvp.Value, filterA))
					{
						set.Add(kvp.Value);
					}
				}

				total = set.Count;
				HandleSorting(set, filterA.SortField.FieldInfo, filterA.SortAscending);

				var rowStart = filterA.Offset;

				if (rowStart >= set.Count)
				{
					return total;
				}
				
				// Limit happens after sorting:
				if (filterA.PageSize != 0)
				{
					var max = rowStart + filterA.PageSize;

					if (max > total)
					{
						max = total;
					}

					for (var i = rowStart; i < max; i++)
					{
						await onResult(context, set[i], i - rowStart, srcA, srcB);
					}
				}
				else
				{
					// All:
					for (var i = rowStart; i < set.Count; i++)
					{
						await onResult(context, set[i], i - rowStart, srcA, srcB);
					}
				}
			}
			else
			{
				foreach (var kvp in Primary)
				{
					if (filterA.Match(context, kvp.Value, filterA) && filterB.Match(context, kvp.Value, filterA))
					{
						var pageFill = total - filterA.Offset;
						total++;

						if (pageFill < 0)
						{
							continue;
						}
						else if (filterA.PageSize != 0 && pageFill > filterA.PageSize)
						{
							if (includeTotal)
							{
								continue;
							}

							break;
						}

						await onResult(context, kvp.Value, pageFill, srcA, srcB);
					}
				}
			}

			return total;
		}

		private void HandleSorting(List<T> set, FieldInfo sort, bool ascend)
		{		
			set.Sort((a, b) => {
					
				var valA = sort.GetValue(a);
				var valB = sort.GetValue(b);
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
					if (ascend)
					{
						return comparison;
					}
					else
					{
						// Invert:
						return -comparison;
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
					Primary.Remove(id, out _);
					Raw.Remove(id, out _);
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