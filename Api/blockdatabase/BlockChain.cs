using Api.SocketServerLibrary;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;
using Api.Startup;
using Api.BlockDatabase;
using Api.Database;
using System.Reflection;

namespace Lumity.BlockChains;


/// <summary>
/// Represents a block chain.
/// </summary>
public partial class BlockChain
{
	
	/// <summary>
	/// Caches by definition ID.
	/// </summary>
	private CacheSet[] _caches;

	private Dictionary<string, AutoServiceMeta> _serviceMeta;

	/// <summary>
	/// Sets up the service metadata for this chain such that caches can be pre-generated.
	/// </summary>
	/// <param name="serviceMeta"></param>
	public void SetServiceMeta(Dictionary<string, AutoServiceMeta> serviceMeta)
	{
		_serviceMeta = serviceMeta;
	}

	/// <summary>
	/// Gets the cache set for the given definition.
	/// </summary>
	/// <param name="definition"></param>
	/// <returns></returns>
	public CacheSet GetCacheForDefinition(Definition definition)
	{
		var defId = (int)definition.Id;
		defId -= (int)(Lumity.BlockChains.Schema.ArchiveDefId + 1); // First defId through here should be 0.

		if (_caches == null)
		{
			_caches = new CacheSet[defId + 5];
		}
		else if (defId >= _caches.Length)
		{
			Array.Resize(ref _caches, defId + 5);
		}

		var set = _caches[defId];

		if (set == null)
		{
			// Instance the set now.
			if (string.IsNullOrEmpty(definition.Name))
			{
				return null;
			}

			// Establish which service will be providing this definition.
			// It might be a mapping. It might not exist anymore if this type was deleted or is simply for some other system using the same chain.
			
			var indexOfMap = definition.Name.IndexOf("_map_");

			if (indexOfMap != -1)
			{
				// Possible mapping defn.

				// TypeA_TypeB_map_MapName

				var typeNames = definition.Name.Substring(0, indexOfMap).Split('_');

				// Lookup the type names.
				if (typeNames.Length == 2)
				{
					var srcType = GetMeta(typeNames[0], _serviceMeta);
					var dstType = GetMeta(typeNames[1], _serviceMeta);

					if (srcType != null && dstType != null)
					{
						// Create the mapping type:
						GenerateMappingMeta(srcType.IdType, dstType.IdType, definition.Name, _serviceMeta, defId);
					}
				}
			}
			else
			{
				// Lookup the type with name definition.Name
				var meta = GetMeta(definition.Name, _serviceMeta);

				if (meta != null)
				{
					_caches[defId] = set = meta.CacheSet;
				}
			}
		}

		if (set == null)
		{
			// Type doesn't exist. Generic empty cache set - this simply exists to
			// avoid load attempts being spammed on every transaction.
			set = new CacheSet(null);
			_caches[defId] = set;
		}

		return set;
	}
	
	private static MethodInfo _mappingCreateMethod;

	/// <summary>
	/// Generates mapping metadata for the given from->to ID types, with the given name and definition ID.
	/// Adds the result to the _caches and the given serviceMeta.
	/// </summary>
	/// <param name="srcIdType"></param>
	/// <param name="dstIdType"></param>
	/// <param name="entityName"></param>
	/// <param name="defId"></param>
	/// <param name="serviceMeta"></param>
	public AutoServiceMeta GenerateMappingMeta(Type srcIdType, Type dstIdType, string entityName, Dictionary<string, AutoServiceMeta> serviceMeta, int defId = 0)
	{
		if (_mappingCreateMethod == null)
		{
			_mappingCreateMethod = typeof(BlockChain).GetMethod(nameof(CreateCacheSet), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
		}

		var mappingType = typeof(Mapping<,>).MakeGenericType(srcIdType, dstIdType);

		// Store mapping info for src->dst
		var setupType = _mappingCreateMethod.MakeGenericMethod(new Type[] {
							mappingType,
							typeof(uint) // Always has a uint ID
						});

		var meta = new AutoServiceMeta()
		{
			IdType = typeof(uint),
			ServicedType = mappingType,
			EntityName = entityName,
			ContentFields = new ContentFields(mappingType)
		};

		var set = meta.CacheSet = (CacheSet)setupType.Invoke(this, new object[] {
			meta
		});

		serviceMeta[meta.EntityName.ToLower()] = meta;

		if (defId != 0)
		{
			_caches[defId] = set;
		}

		return meta;
	}

	/// <summary>
	/// Gets the svc meta for the given type name.
	/// </summary>
	/// <param name="serviceMeta"></param>
	/// <param name="contentTypeName"></param>
	/// <returns></returns>
	private static AutoServiceMeta GetMeta(string contentTypeName, Dictionary<string, AutoServiceMeta> serviceMeta)
	{
		if (!serviceMeta.TryGetValue(contentTypeName.ToLower(), out AutoServiceMeta result))
		{
			return null;
		}

		return result;
	}
	
	/// <summary>
	/// Creates a cache set of the given type.
	/// </summary>
	public static CacheSet CreateCacheSet<T,ID>(AutoServiceMeta meta)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		return new CacheSet<T, ID>(meta.ContentFields, meta.EntityName);
	}
	
}
