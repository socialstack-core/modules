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
	public class MappingService<T, SRC_ID, TARG_ID> : AutoService<T, uint> 
		where T:Mapping<SRC_ID, TARG_ID>, new()
		where SRC_ID: struct, IEquatable<SRC_ID>
		where TARG_ID: struct
	{

		/// <summary>
		/// ],"values":[
		/// </summary>
		private static readonly byte[] IncludesMapFooter = new byte[] {
			(byte)']', (byte)',', (byte)'"', (byte)'v',(byte)'a',(byte)'l',(byte)'u',(byte)'e',(byte)'s',(byte)'"',(byte)':',(byte)'['
		};

		/// <summary>
		/// E.g. "UserId". The field name of the source ID.
		/// </summary>
		private string srcIdFieldName;
		
		private NonUniqueIndex<T, SRC_ID> _cacheIndex;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public MappingService(string srcIdName) : base(new EventGroup<T>()) {
			srcIdFieldName = srcIdName;
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
					_cacheIndex = cache.GetIndex<SRC_ID>(srcIdFieldName) as NonUniqueIndex<T, SRC_ID>;
				}
			}
			
			// If cached, directly enumerate over the IDs via the cache.
			if (_cacheIndex != null)
			{
				// This mapping type is cached.
				var _enum = collectedIds.GetEnumerator();

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

				var _enum = collectedIds.GetEnumerator();

				while (_enum.HasMore())
				{
					// Get current value:
					idList.Add(_enum.Current());
				}

				var mappingEntries = await List(context, new Filter<T>().EqualsSet(srcIdFieldName, idList));

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
	
}