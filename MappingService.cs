using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.SocketServerLibrary;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Api.Startup
{
	/// <summary>
	/// The AutoService for mapping types.
	/// </summary>
	public partial class MappingService<SRC, TARG, SRC_ID, TARG_ID> : MappingService<SRC_ID, TARG_ID>
		where SRC: Content<SRC_ID>, new()
		where TARG : Content<TARG_ID>, new()
		where SRC_ID: struct, IEquatable<SRC_ID>, IConvertible, IComparable<SRC_ID>
		where TARG_ID: struct, IEquatable<TARG_ID>, IConvertible, IComparable<TARG_ID>
	{

		/// <summary>
		/// ],"values":[
		/// </summary>
		private static readonly byte[] IncludesMapFooter = new byte[] {
			(byte)']', (byte)',', (byte)'"', (byte)'v',(byte)'a',(byte)'l',(byte)'u',(byte)'e',(byte)'s',(byte)'"',(byte)':',(byte)'['
		};

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public MappingService(AutoService<SRC, SRC_ID> src, AutoService<TARG, TARG_ID> targ, Type t, string entityName) : base(t, entityName) {
			srcIdFieldName = "SourceId";
			targetIdFieldName = "TargetId";
			targetIdFieldEquals = "TargetId=?";
			srcIdFieldEquals = "SourceId=?";
			srcIdFieldNameEqSet = "SourceId=[?]";
			targetIdFieldNameEqSet = "TargetId=[?]";
			srcAndTargEq = "SourceId=? and TargetId=?";
			Source = src;
			Target = targ;
			_jsonWriter = MappingToJson.GetMapper(typeof(SRC_ID), typeof(TARG_ID));
		}

		private readonly MappingToJson _jsonWriter;

		/// <summary>
		/// Source service.
		/// </summary>
		public AutoService<SRC, SRC_ID> Source;

		/// <summary>
		/// Target service.
		/// </summary>
		public AutoService<TARG, TARG_ID> Target;

		/// <summary>
		/// The source type if this is a mapping service.
		/// </summary>
		public override Type MappingSourceType => typeof(SRC);

		/// <summary>
		/// The target type if this is a mapping service.
		/// </summary>
		public override Type MappingTargetType => typeof(TARG);
		
		/// <summary>
		/// Gets a list of source IDs by target ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <param name="onResult"></param>
		/// <param name="rSrc"></param>
		/// <returns></returns>
		public async ValueTask ListSourceIdByTarget(Context context, TARG_ID id, Func<Context, SRC_ID, object, ValueTask> onResult, object rSrc)
		{
			await Where(targetIdFieldEquals)
			.Bind(id)
			.ListAll(context, async (Context ctx, Mapping<SRC_ID, TARG_ID> entity, int index, object src, object rSrc) =>
				{
					// Passing in onResult prevents a delegate frame allocation.
					var _onResult = (Func<Context, SRC_ID, object, ValueTask>)src;

					// Emit the result:
					await _onResult(ctx, entity.SourceId, rSrc);
				},
				onResult,
				rSrc
			);
		}

		/// <summary>
		///  Creates a mapping from the given src to the given target. Only available on mapping services.
		///  It's more ideal to use the type specific overloads whenever possible (particularly as they're available on regular services, rather than this mapping service specific one).
		///  See also: CreateMappingIfNotExists, EnsureMapping
		/// </summary>
		/// <param name="context"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="opts"></param>
		/// <returns></returns>
		public override async ValueTask<bool> CreateMapping(Context context, object a, object b, DataOptions opts = DataOptions.Default)
		{
			var src = (SRC)a;
			var targ = (TARG)b;

			if (src == null || targ == null)
			{
				return false;
			}

			// Add it:
			var entry = Activator.CreateInstance(InstanceType) as Mapping<SRC_ID, TARG_ID>;
			entry.SourceId = src.Id;
			entry.TargetId = targ.Id;
			entry.CreatedUtc = DateTime.UtcNow;

			return await Create(context, entry, DataOptions.IgnorePermissions) != null;
		}
		
		/// <summary>
		/// Gets a list of source IDs by target ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <param name="onResult"></param>
		/// <param name="tSrc"></param>
		/// <returns></returns>
		public async ValueTask ListTargetIdBySource(Context context, IDCollector<SRC_ID> id, Func<Context, TARG_ID, object, ValueTask> onResult, object tSrc)
		{
			await Where(srcIdFieldNameEqSet)
			.Bind(id)
			.ListAll(context, async (Context ctx, Mapping<SRC_ID, TARG_ID> entity, int index, object src, object rSrc) =>
			{
				// Passing in onResult prevents a delegate frame allocation.
				var _onResult = (Func<Context, TARG_ID, object, ValueTask>)src;

				// Emit the result:
				await _onResult(ctx, entity.TargetId, rSrc);
			},
				onResult,
				tSrc
			);
		}
		
		/// <summary>
		/// Gets a list of source IDs by target ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <param name="onResult"></param>
		/// <param name="tSrc"></param>
		/// <returns></returns>
		public async ValueTask ListTargetIdBySource(Context context, SRC_ID id, Func<Context, TARG_ID, object, ValueTask> onResult, object tSrc)
		{
			await Where(srcIdFieldEquals)
			.Bind(id)
			.ListAll(context, async (Context ctx, Mapping<SRC_ID, TARG_ID> entity, int index, object src, object rSrc) =>
				{
					// Passing in onResult prevents a delegate frame allocation.
					var _onResult = (Func<Context, TARG_ID, object, ValueTask>)src;

					// Emit the result:
					await _onResult(ctx, entity.TargetId, rSrc);
				},
				onResult,
				tSrc
			);
		}

		/// <summary>
		/// Gets a list of source IDs by target ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <param name="collector"></param>
		/// <returns></returns>
		public async ValueTask CollectByTarget(Context context, IDCollector<SRC_ID> collector, TARG_ID id)
		{
			await Where(targetIdFieldEquals)
			.Bind(id)
			.ListAll(context, (Context ctx, Mapping<SRC_ID, TARG_ID> entity, int index, object src, object rSrc) =>
			{
				// Passing in onResult prevents a delegate frame allocation.
				var _col = (IDCollector<SRC_ID>)src;
				_col.Collect(entity);
				return new ValueTask();
			},
				collector
			);
		}

		/// <summary>
		/// Gets a list of source IDs by target ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <param name="collector"></param>
		/// <returns></returns>
		public async ValueTask CollectByTargetEquals(Context context, IDCollector<SRC_ID> collector, TARG_ID id)
		{
			await Where(targetIdFieldEquals)
			.Bind(id)
			.ListAll(context, (Context ctx, Mapping<SRC_ID, TARG_ID> entity, int index, object src, object rSrc) =>
			{
				// Passing in onResult prevents a delegate frame allocation.
				var _col = (IDCollector<SRC_ID>)src;
				_col.Collect(entity);
				return new ValueTask();
			},
				collector
			);

			collector.Eliminate(1, true);
		}

		/// <summary>
		/// Gets a list of source IDs by target ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="idSet"></param>
		/// <param name="collector"></param>
		/// <returns></returns>
		public async ValueTask CollectByTargetSet(Context context, IDCollector<SRC_ID> collector, IEnumerable<TARG_ID> idSet)
		{
			await Where(targetIdFieldNameEqSet)
			.Bind(idSet)
			.ListAll(context, (Context ctx, Mapping<SRC_ID, TARG_ID> entity, int index, object src, object rSrc) =>
			{
				// Passing in onResult prevents a delegate frame allocation.
				var _col = (IDCollector<SRC_ID>)src;
				_col.AddSorted(entity.SourceId);
				return new ValueTask();
			},
				collector
			);

			collector.Eliminate(1); // Only needs to appear once so let's eliminate the repeats. 
		}

		/// <summary>
		/// Gets a list of source IDs by target ID and eliminates values that do not have the number of entries to match idSet count
		/// </summary>
		/// <param name="context"></param>
		/// <param name="collector"></param>
		/// <param name="idSet"></param>
		/// <returns></returns>
		public async ValueTask CollectByTargetSetContains(Context context, IDCollector<SRC_ID> collector, IEnumerable<TARG_ID> idSet)
        {
			await Where(targetIdFieldNameEqSet)
			.Bind(idSet)
			.ListAll(context, (Context ctx, Mapping<SRC_ID, TARG_ID> entity, int index, object src, object rSrc) =>
			{
				// Passing in onResult prevents a delegate frame allocation.
				var _col = (IDCollector<SRC_ID>)src;
				_col.AddSorted(entity.SourceId);
				return new ValueTask();
			},
				collector
			);

			collector.Eliminate(idSet.Count());
		}

		/// <summary>
		/// Gets a list of source IDs by target ID and eliminates values that do not have the number of entries to match idSet count
		/// </summary>
		/// <param name="context"></param>
		/// <param name="collector"></param>
		/// <param name="idSet"></param>
		/// <returns></returns>
		public async ValueTask CollectByTargetSetEquals(Context context, IDCollector<SRC_ID> collector, IEnumerable<TARG_ID> idSet)
		{
			await Where(targetIdFieldNameEqSet)
			.Bind(idSet)
			.ListAll(context, (Context ctx, Mapping<SRC_ID, TARG_ID> entity, int index, object src, object rSrc) =>
			{
				// Passing in onResult prevents a delegate frame allocation.
				var _col = (IDCollector<SRC_ID>)src;
				_col.AddSorted(entity.SourceId);
				return new ValueTask();
			},
				collector
			);

			collector.Eliminate(idSet.Count(), true);
		}

		/// <summary>
		/// Delete an entity.
		/// </summary>
		public virtual async ValueTask<Mapping<SRC_ID, TARG_ID>> Delete(Context context, SRC_ID src, TARG_ID targ, DataOptions options = DataOptions.Default)
		{
			var entity = await Where(srcAndTargEq, options).Bind(src).Bind(targ).Last(context);
			if (entity != null)
			{
				await Delete(context, entity, options);
			}
			return entity;
		}

		/// <summary>
		/// Ensures the given set of target IDs are exactly what is present in the map.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="src"></param>
		/// <param name="targetIds"></param>
		/// <returns></returns>
		public override async ValueTask EnsureMapping(Context context, SRC_ID src, IEnumerable<TARG_ID> targetIds)
		{
			// First, get all entries by src.
			var uniques = new Dictionary<TARG_ID, EnsuredMapping<SRC_ID, TARG_ID>>();

			await Where(srcIdFieldEquals).Bind(src).ListAll(context, (Context c, Mapping<SRC_ID, TARG_ID> map, int index, object a, object b) =>
			{
				uniques[map.TargetId] = new EnsuredMapping<SRC_ID, TARG_ID>() { Mapping = map };
				return new ValueTask();
			});

			if (targetIds != null)
			{
				foreach (var id in targetIds)
				{
					if (uniques.TryGetValue(id, out	EnsuredMapping<SRC_ID, TARG_ID> ensured))
					{
						// Already exists
						if (!ensured.Readded)
						{
							// Recreate the struct:
							uniques[id] = new EnsuredMapping<SRC_ID, TARG_ID>() { Readded = true, Mapping = ensured.Mapping };
						}
						continue;
					}

					// Permitted to view this? Get will permission check for us:
					var entity = await Target.Get(context, id);

					if (entity == null)
					{
						continue;
					}

					// Add it:
					var entry = Activator.CreateInstance(InstanceType) as Mapping<SRC_ID, TARG_ID>;
					entry.SourceId = src;
					entry.TargetId = id;
					entry.CreatedUtc = DateTime.UtcNow;

					await Create(context, entry, DataOptions.IgnorePermissions);
				}
			}

			// Delete anything that remains in uniques:
			foreach (var entry in uniques)
			{
				if (entry.Value.Readded)
				{
					continue;
				}
				await Delete(context, entry.Value.Mapping, DataOptions.IgnorePermissions);
			}

		}

		/// <summary>
		/// Call this on the actual mapping service. S is the source ID type.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="mappingCollector"></param>
		/// <param name="idSet"></param>
		/// <param name="writer"></param>
		/// <returns></returns>
		public override async ValueTask OutputMap(Context context, IDCollector mappingCollector, IDCollector idSet, Writer writer) 
		{
			var collectedIds = idSet as IDCollector<SRC_ID>;

			// Get its locale 0 cache (it's a mapping type, so it's never localised):
			if (CacheAvailable && _cacheIndex == null){
				
				var cache = GetCacheForLocale(1);
				
				if (cache != null)
				{
					// It's a cached mapping type.
					// Pre-obtain index ref now:
					_cacheIndex = cache.GetIndex<SRC_ID>(srcIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID>;
					_reverseCacheIndex = cache.GetIndex<TARG_ID>(targetIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, TARG_ID>;
				}
			}
			
			// If cached, directly enumerate over the IDs via the cache.
			if (_cacheIndex != null)
			{
				// This mapping type is cached.
				var _enum = collectedIds.GetNonAllocEnumerator();

				var first = true;

				while (_enum.HasMore())
				{
					// Get current value:
					var val = _enum.Current();

					// Read that ID set from the cache:
					var indexEnum = _cacheIndex.GetEnumeratorFor(val);

					while (indexEnum.HasMore())
					{
						// Get current value:
						var mappingEntry = indexEnum.Current();

						if (first)
						{
							first = false;
						}
						else
						{
							writer.Write((byte)',');
						}

						// Output src, target
						_jsonWriter.ToJson(mappingEntry, writer);

						// Collect target:
						mappingCollector.Collect(mappingEntry);
					}
				}

			}
			else if(collectedIds.Count > 0)
			{
				// DB hit. Allocate list of IDs as well.
				var idList = new List<SRC_ID>(collectedIds.Count);

				var _enum = collectedIds.GetNonAllocEnumerator();

				while (_enum.HasMore())
				{
					// Get current value:
					idList.Add(_enum.Current());
				}

				var mappingEntries = await Where(srcIdFieldNameEqSet).Bind(idList).ListAll(context);

				var first = true;

				foreach (var mappingEntry in mappingEntries)
				{
					// Output src, target
					if (first)
					{
						first = false;
					}
					else
					{
						writer.Write((byte)',');
					}

					_jsonWriter.ToJson(mappingEntry, writer);
					
					// Collect:
					mappingCollector.Collect(mappingEntry);
				}
			}

			// In between map and values:
			writer.Write(IncludesMapFooter, 0, 12);
		}
		
	}

	/// <summary>
	/// An intermediate mapping service to make maps more generally accessible, for instances where you only know the src and target ID types.
	/// </summary>
	/// <typeparam name="SRC_ID"></typeparam>
	/// <typeparam name="TARG_ID"></typeparam>
	public partial class MappingService<SRC_ID, TARG_ID> : AutoService<Mapping<SRC_ID, TARG_ID>, uint>
		where SRC_ID : struct, IEquatable<SRC_ID>, IConvertible, IComparable<SRC_ID>
		where TARG_ID : struct, IEquatable<TARG_ID>, IConvertible, IComparable<TARG_ID>
	{

		/// <summary>
		/// E.g. "UserId". The field name of the source ID.
		/// </summary>
		protected string srcIdFieldName;

		/// <summary>
		/// E.g. "TagId". The field name of the tag ID.
		/// </summary>
		protected string targetIdFieldName;

		/// <summary>
		/// Src=? and Targ=?
		/// </summary>
		protected string srcAndTargEq;

		/// <summary>
		/// TargetName=?
		/// </summary>
		protected string targetIdFieldEquals;

		/// <summary>
		/// SrcName=?
		/// </summary>
		protected string srcIdFieldEquals;

		/// <summary>
		/// SrcName=[?]
		/// </summary>
		protected string srcIdFieldNameEqSet;

		/// <summary>
		/// TargetName=[?]
		/// </summary>
		protected string targetIdFieldNameEqSet;

		/// <summary>
		/// Quick ref to cache index, if it is cached.
		/// </summary>
		protected NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID> _cacheIndex;
		
		/// <summary>
		/// Quick ref to reverse cache index, if it is cached.
		/// </summary>
		protected NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, TARG_ID> _reverseCacheIndex;

		/// <summary>
		/// Creates a mapping service using the given type as the mapping object.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="entityName"></param>
		public MappingService(Type t, string entityName) : base(new EventGroup<Mapping<SRC_ID, TARG_ID>>(), t, entityName)
		{
			// Mapping services are cached by default:
			Cache();
		}

		/// <summary>
		/// Deletes all rows by target ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public async ValueTask DeleteByTarget(Context context, TARG_ID id, DataOptions options = DataOptions.Default)
		{
			var set = await Where(targetIdFieldEquals, options)
			.Bind(id)
			.ListAll(context);

			if (set != null && set.Count > 0)
			{
				foreach (var entry in set)
				{
					await Delete(context, entry, options);
				}
			}
		}

		/// <summary>
		/// True if this is a mapping service.
		/// </summary>
		public override bool IsMapping
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Deletes a mapping by the src and target IDs.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="src"></param>
		/// <param name="targ"></param>
		/// <returns></returns>
		public async ValueTask<bool> DeleteByIds(Context context, SRC_ID src, TARG_ID targ)
		{
			var mapping = await GetByIds(context, src, targ);

			if (mapping != null)
			{
				return await Delete(context, mapping);
			}

			return false;
		}

		/// <summary>
		/// Gets the index for source values.
		/// </summary>
		/// <returns></returns>
		public NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID> GetSourceIndex()
		{
			if (CacheAvailable && _cacheIndex == null)
			{

				var cache = GetCacheForLocale(1);

				if (cache != null)
				{
					// It's a cached mapping type.
					// Pre-obtain index ref now:
					_cacheIndex = cache.GetIndex<SRC_ID>(srcIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID>;
					_reverseCacheIndex = cache.GetIndex<TARG_ID>(targetIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, TARG_ID>;
				}
			}

			return _cacheIndex;
		}

		/// <summary>
		/// Adds source IDs to the given collector if the source contains any of the given target IDs.
		/// </summary>
		/// <param name="collector"></param>
		/// <param name="idSet"></param>
		/// <returns></returns>
		public void SourceContainsAny(IDCollector<SRC_ID> collector, IDCollector<TARG_ID> idSet)
		{
			if (CacheAvailable && _cacheIndex == null)
			{
				var cache = GetCacheForLocale(1);

				if (cache != null)
				{
					// It's a cached mapping type.
					// Pre-obtain index ref now:
					_cacheIndex = cache.GetIndex<SRC_ID>(srcIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID>;
					_reverseCacheIndex = cache.GetIndex<TARG_ID>(targetIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, TARG_ID>;
				}
			}

			if (_cacheIndex == null || _reverseCacheIndex == null)
			{
				throw new PublicException("Mappings must be cached to use containsAll or containsAny filters on them.", "filter_invalid");
			}

			var idSetCount = idSet.Count;

			if (idSetCount == 0)
			{
				// Note to filter developers: the result for this particular scenario is either going to be:
				// - Every source
				// - Every source not in the mapping index
				// The above scenarios must be handled outside the mapping service itself.
				throw new PublicException("An empty set of IDs was used where it is not supported.", "filter_invalid");
			}

			// Optimisation: If the idSet has exactly 1 entry, we'll use the reverse lookup instead.
			if (idSetCount == 1)
			{
				// For each target..
				var targetIterator = _reverseCacheIndex.GetEnumeratorFor(idSet.First.Entries[0]);

				while (targetIterator.HasMore())
				{
					// Get current source:
					var src = targetIterator.Current();
					
					// src is a valid result.
					collector.Add(src.SourceId);
				}
			}
			else
			{
				// For each source..
				foreach (var kvp in _cacheIndex.Dictionary)
				{
					var src = kvp.Key;
					var targets = kvp.Value;

					// Targets must contain at least one of the given IDs.
					var idsToCheck = idSet.GetNonAllocEnumerator();
					
					while (idsToCheck.HasMore())
					{
						var idToCheck = idsToCheck.Current();
						var targToCheck = targets.First;

						while (targToCheck != null)
						{
							if (targToCheck.Current.TargetId.Equals(idToCheck))
							{
								// Valid source
								collector.Add(src);
								goto NextSource;
							}

							targToCheck = targToCheck.Next;
						}
					}

					NextSource:
					continue;
				}
			}
		}

		/// <summary>
		/// Adds IDs to the given collector for any sources which contain all of the given IDs.
		/// </summary>
		/// <param name="collector"></param>
		/// <param name="idSet">MUST be sorted in ascending order.</param>
		/// <param name="exactMatch">True if it must match all and only all - there are no additional ones.</param>
		/// <returns></returns>
		public void SourceContainsAll(IDCollector<SRC_ID> collector, IDCollector<TARG_ID> idSet, bool exactMatch)
		{
			if (CacheAvailable && _cacheIndex == null)
			{

				var cache = GetCacheForLocale(1);

				if (cache != null)
				{
					// It's a cached mapping type.
					// Pre-obtain index ref now:
					_cacheIndex = cache.GetIndex<SRC_ID>(srcIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID>;
					_reverseCacheIndex = cache.GetIndex<TARG_ID>(targetIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, TARG_ID>;
				}
			}

			if (_cacheIndex == null || _reverseCacheIndex == null)
			{
				throw new PublicException("Mappings must be cached to use containsAll or containsAny filters on them.", "filter_invalid");
			}
			
			var idSetCount = idSet.Count;

			if (idSetCount == 0)
			{
				// Note to filter developers: the result for this particular scenario is either going to be:
				// - Every source
				// - Every source not in the mapping index
				// The above scenarios must be handled outside the mapping service itself.
				throw new PublicException("An empty set of IDs was used where it is not supported.", "filter_invalid");
			}

			// Optimisation: If the idSet has exactly 1 entry, we'll use the reverse lookup instead.
			if (idSetCount == 1)
			{
				// For each target..
				var targetIterator = _reverseCacheIndex.GetEnumeratorFor(idSet.First.Entries[0]);

				while (targetIterator.HasMore())
				{
					// Get current source:
					var src = targetIterator.Current();

					if (!exactMatch)
					{
						// src is a valid result.
						collector.Add(src.SourceId);
					}
					else
					{
						// src is a valid result only if it has nothing else.
						// Check if the forward lookup has any secondary records:
						var fwdLookup = _cacheIndex.GetEnumeratorFor(src.SourceId);

						if (fwdLookup.HasMore())
						{
							// This first record can only be the targetId if there are no more because of the reverse lookup.
							if (fwdLookup.Node.Next == null)
							{
								// src is a valid result.
								collector.Add(src.SourceId);
							}
						}
					}
				}
			}
			else
			{
				// For each source..
				foreach (var kvp in _cacheIndex.Dictionary)
				{
					var src = kvp.Key;
					var targets = kvp.Value;

					if (exactMatch)
					{
						// If the counts aren't an exact match, we must fail the source.
						if (targets.Count != idSetCount)
						{
							continue;
						}
					}

					// Targets must contain all the given IDs.
					// if there are any misses, we fail the source.
					var idsToCheck = idSet.GetNonAllocEnumerator();
					var success = true;

					while (idsToCheck.HasMore())
					{
						var idToCheck = idsToCheck.Current();

						var targToCheck = targets.First;
						var contains = false;

						while (targToCheck != null)
						{
							if (targToCheck.Current.TargetId.Equals(idToCheck))
							{
								contains = true;
								break;
							}
							targToCheck = targToCheck.Next;
						}

						if (!contains)
						{
							// Source has failed.
							success = false;
							break;
						}
					}

					if (success)
					{
						// src is a valid result.
						collector.Add(src);
					}
				}
			}
		}
		
		/// <summary>
		/// Gets a mapping by the src and target IDs.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="src"></param>
		/// <param name="targ"></param>
		/// <returns></returns>
		public async ValueTask<Mapping<SRC_ID, TARG_ID>> GetByIds(Context context, SRC_ID src, TARG_ID targ)
		{
			if (CacheAvailable && _cacheIndex == null)
			{

				var cache = GetCacheForLocale(1);

				if (cache != null)
				{
					// It's a cached mapping type.
					// Pre-obtain index ref now:
					_cacheIndex = cache.GetIndex<SRC_ID>(srcIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID>;
					_reverseCacheIndex = cache.GetIndex<TARG_ID>(targetIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, TARG_ID>;
				}
			}

			if (_cacheIndex != null)
			{
				// Using an index scan
				var indexEnum = _cacheIndex.GetEnumeratorFor(src);

				while (indexEnum.HasMore())
				{
					// Get current value:
					var mappingEntry = indexEnum.Current();

					if (mappingEntry.TargetId.Equals(targ))
					{
						return mappingEntry;
					}
				}

				return null;
			}
			else
			{
				return await Where(srcAndTargEq).Bind(src).Bind(targ).First(context);
			}
		}

		/// <summary>
		/// Gets a cache iterator for the given source ID (if the cache is active, and it exists).
		/// </summary>
		/// <param name="src"></param>
		/// <returns></returns>
		public IndexLinkedList<Mapping<SRC_ID, TARG_ID>> GetRawCacheList(SRC_ID src)
		{
			if (!CacheAvailable)
			{
				return null;
			}

			if (_cacheIndex == null)
			{

				var cache = GetCacheForLocale(1);

				if (cache != null)
				{
					// It's a cached mapping type.
					// Pre-obtain index ref now:
					_cacheIndex = cache.GetIndex<SRC_ID>(srcIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID>;
					_reverseCacheIndex = cache.GetIndex<TARG_ID>(targetIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, TARG_ID>;
				}

				if (_cacheIndex == null)
				{
					return null;
				}
			}

			// Using an index scan
			return _cacheIndex.GetIndexList(src);
		}

		/// <summary>
		/// Gets a cache iterator for the given source ID (if the cache is active, and it exists).
		/// </summary>
		/// <param name="src"></param>
		/// <returns></returns>
		public IndexEnum<Mapping<SRC_ID, TARG_ID>> GetSourceFromCache(SRC_ID src)
		{
			var cacheList = GetRawCacheList(src);

			if (cacheList == null)
			{
				return default(IndexEnum<Mapping<SRC_ID, TARG_ID>>);
			}

			return new IndexEnum<Mapping<SRC_ID, TARG_ID>>()
			{
				Node = cacheList.First
			};
		}
		
		/// <summary>
		/// True if the given mapping entry exists in this services cache. Note that if the cache is not active, this returns false.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="targ"></param>
		/// <returns></returns>
		public bool ExistsInCache(SRC_ID src, TARG_ID targ)
		{
			if (!CacheAvailable)
			{
				return false;
			}

			if (_cacheIndex == null)
			{

				var cache = GetCacheForLocale(1);

				if (cache != null)
				{
					// It's a cached mapping type.
					// Pre-obtain index ref now:
					_cacheIndex = cache.GetIndex<SRC_ID>(srcIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID>;
					_reverseCacheIndex = cache.GetIndex<TARG_ID>(targetIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, TARG_ID>;
				}

				if (_cacheIndex == null)
				{
					return false;
				}
			}

			// Using an index scan
			var indexEnum = _cacheIndex.GetEnumeratorFor(src);

			while (indexEnum.HasMore())
			{
				// Get current value:
				var mappingEntry = indexEnum.Current();

				if (mappingEntry.TargetId.Equals(targ))
				{
					return true;
				}
			}

			return false;
		}
		
		/// <summary>
		/// Returns true if a mapping from src_id => targ_id exists.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="src"></param>
		/// <param name="targ"></param>
		/// <returns></returns>
		public async ValueTask<bool> CheckIfExists(Context context, SRC_ID src, TARG_ID targ)
		{
			return (await GetByIds(context, src, targ)) != null;
		}
		
		/// <summary>
		/// Returns true if a mapping from src_id => targ_id was just created. False if it already existed.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="src"></param>
		/// <param name="targ"></param>
		/// <returns></returns>
		public async ValueTask<bool> CreateIfNotExists(Context context, SRC_ID src, TARG_ID targ)
		{
			if (await CheckIfExists(context, src, targ))
			{
				return false;
			}

			// Add it:
			var entry = Activator.CreateInstance(InstanceType) as Mapping<SRC_ID, TARG_ID>;
			entry.SourceId = src;
			entry.TargetId = targ;
			entry.CreatedUtc = DateTime.UtcNow;

			await Create(context, entry, DataOptions.IgnorePermissions);

			return true;
		}
		
		/// <summary>
		/// Ensures the given set of target IDs are exactly what is present in the map.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="src"></param>
		/// <param name="targetIds"></param>
		/// <returns></returns>
		public virtual ValueTask EnsureMapping(Context context, SRC_ID src, IEnumerable<TARG_ID> targetIds)
		{
			throw new NotImplementedException("Don't instance a MappingService<,> directly. Use the more concrete MappingService<,,,> instead.");
		}
	}

	/// <summary>
	/// Used to output JSON for a mapping entity. The default is (uint,uint)
	/// </summary>
	public class MappingToJson
	{
		/// <summary>
		/// Gets a shared JSON mapper for the given ID type pair.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static MappingToJson GetMapper(Type a, Type b)
		{
			if (a == typeof(uint))
			{
				return b == typeof(uint) ? uintuint : uintulong;
			}

			return b == typeof(uint) ? ulonguint : ulongulong;
		}

		private static MappingToJson uintuint = new MappingToJson();
		private static MappingToJson uintulong = new MappingToJsonUintUlong();
		private static MappingToJson ulongulong = new MappingToJsonUlongUlong();
		private static MappingToJson ulonguint = new MappingToJsonUlongUint();

		/// <summary>
		/// Writes the given mapping into the given writer.
		/// </summary>
		/// <param name="mapping"></param>
		/// <param name="writer"></param>
		public virtual void ToJson(Content<uint> mapping, Writer writer)
		{
			var mappingRow = (Mapping<uint, uint>)mapping;
			writer.WriteS(mappingRow.Id);
			writer.Write((byte)',');
			writer.WriteS(mappingRow.SourceId);
			writer.Write((byte)',');
			writer.WriteS(mappingRow.TargetId);
		}

	}

	/// <summary>
	/// Converts (uint,ulong) mappings.
	/// </summary>
	public class MappingToJsonUintUlong : MappingToJson
	{
		/// <summary>
		/// Writes the given mapping into the given writer.
		/// </summary>
		/// <param name="mapping"></param>
		/// <param name="writer"></param>
		public override void ToJson(Content<uint> mapping, Writer writer)
		{
			var mappingRow = (Mapping<uint, ulong>)mapping;
			writer.WriteS(mappingRow.Id);
			writer.Write((byte)',');
			writer.WriteS(mappingRow.SourceId);
			writer.Write((byte)',');
			writer.WriteS(mappingRow.TargetId);
		}

	}

	/// <summary>
	/// Converts (ulong,ulong) mappings.
	/// </summary>
	public class MappingToJsonUlongUlong : MappingToJson
	{
		/// <summary>
		/// Writes the given mapping into the given writer.
		/// </summary>
		/// <param name="mapping"></param>
		/// <param name="writer"></param>
		public override void ToJson(Content<uint> mapping, Writer writer)
		{
			var mappingRow = (Mapping<ulong, ulong>)mapping;
			writer.WriteS(mappingRow.Id);
			writer.Write((byte)',');
			writer.WriteS(mappingRow.SourceId);
			writer.Write((byte)',');
			writer.WriteS(mappingRow.TargetId);
		}

	}
	
	/// <summary>
	/// Converts (ulong,uint) mappings.
	/// </summary>
	public class MappingToJsonUlongUint : MappingToJson
	{
		/// <summary>
		/// Writes the given mapping into the given writer.
		/// </summary>
		/// <param name="mapping"></param>
		/// <param name="writer"></param>
		public override void ToJson(Content<uint> mapping, Writer writer)
		{
			var mappingRow = (Mapping<ulong, uint>)mapping;
			writer.WriteS(mappingRow.Id);
			writer.Write((byte)',');
			writer.WriteS(mappingRow.SourceId);
			writer.Write((byte)',');
			writer.WriteS(mappingRow.TargetId);
		}

	}

	/// <summary>
	/// Used to track mappings whilst they are being added/ removed.
	/// </summary>
	public struct EnsuredMapping<SRC_ID, TARG_ID>
		where SRC_ID:struct
		where TARG_ID: struct
	{
		/// <summary>
		/// True if this mapping was 'readded'.
		/// </summary>
		public bool Readded;
		/// <summary>
		/// The mapping itself.
		/// </summary>
		public Mapping<SRC_ID, TARG_ID> Mapping;
	}

}