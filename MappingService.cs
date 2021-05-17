using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.SocketServerLibrary;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Startup
{
	/// <summary>
	/// The AutoService for mapping types.
	/// </summary>
	public class MappingService<SRC, TARG, SRC_ID, TARG_ID> : MappingService<SRC_ID, TARG_ID>
		where SRC: Content<SRC_ID>, new()
		where TARG : Content<TARG_ID>, new()
		where SRC_ID: struct, IEquatable<SRC_ID>, IConvertible
		where TARG_ID: struct, IEquatable<TARG_ID>, IConvertible
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
		public MappingService(AutoService<SRC, SRC_ID> src, AutoService<TARG, TARG_ID> targ, string srcIdName, string targetIdName, Type t) : base(t) {
			srcIdFieldName = srcIdName;
			targetIdFieldName = targetIdName;
			targetIdFieldEquals = targetIdName + "=?";
			srcIdFieldEquals = srcIdName + "=?";
			srcIdFieldNameEqSet = srcIdFieldName + "=[?]";
			targetIdFieldNameEqSet = targetIdFieldName + "=[?]";
			srcAndTargEq = srcIdName + "=? and " + targetIdName + "=?";
			Source = src;
			Target = targ;
		}

		/// <summary>
		/// Source service.
		/// </summary>
		public AutoService<SRC, SRC_ID> Source;

		/// <summary>
		/// Target service.
		/// </summary>
		public AutoService<TARG, TARG_ID> Target;

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
				_col.Collect(entity);
				return new ValueTask();
			},
				collector
			);
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
			var uniques = new Dictionary<TARG_ID, Mapping<SRC_ID, TARG_ID>>();

			await Where(srcIdFieldEquals).Bind(src).ListAll(context, (Context c, Mapping<SRC_ID, TARG_ID> map, int index, object a, object b) =>
			{
				uniques[map.TargetId] = map;
				return new ValueTask();
			});

			if (targetIds != null)
			{
				foreach (var id in targetIds)
				{
					if (uniques.Remove(id))
					{
						// Already exists
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
				await Delete(context, entry.Value, DataOptions.IgnorePermissions);
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
			if (_cache != null && _cacheIndex == null){
				
				var cache = GetCacheForLocale(0);
				
				if (cache != null)
				{
					// It's a cached mapping type.
					// Pre-obtain index ref now:
					_cacheIndex = cache.GetIndex<SRC_ID>(srcIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID>;
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
						mappingEntry.ToJson(writer);

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

					mappingEntry.ToJson(writer);
					
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
	public class MappingService<SRC_ID, TARG_ID> : AutoService<Mapping<SRC_ID, TARG_ID>, uint>
		where SRC_ID : struct, IEquatable<SRC_ID>, IConvertible
		where TARG_ID : struct, IEquatable<TARG_ID>, IConvertible
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
		/// Creates a mapping service using the given type as the mapping object.
		/// </summary>
		/// <param name="t"></param>
		public MappingService(Type t) : base(new EventGroup<Mapping<SRC_ID, TARG_ID>>(), t)
		{

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
			if (_cache != null && _cacheIndex == null)
			{

				var cache = GetCacheForLocale(0);

				if (cache != null)
				{
					// It's a cached mapping type.
					// Pre-obtain index ref now:
					_cacheIndex = cache.GetIndex<SRC_ID>(srcIdFieldName) as NonUniqueIndex<Mapping<SRC_ID, TARG_ID>, SRC_ID>;
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
						return true;
					}
				}

				return false;
			}
			else
			{
				return await Where(srcAndTargEq).Bind(src).Bind(targ).Any(context);
			}
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
}