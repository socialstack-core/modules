using Api.Contexts;
using Api.Database;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lumity.BlockChains;


/// <summary>
/// Used to read transactions and has an in-memory cache as its state storage.
/// </summary>
public class CachingTransactionReader : TransactionReader
{
	private Context _loadContext = new Context(1, 1, 1);
	private CacheSet[] _caches;
	private Dictionary<string, Type> _meta = new Dictionary<string, Type>();

	/// <summary>
	/// Adds a mapping of definition name -> the system type to use in the cache.
	/// </summary>
	/// <param name="definitionName"></param>
	/// <param name="typeToUse"></param>
	public void Map(string definitionName, Type typeToUse)
	{
		_meta[definitionName] = typeToUse;
	}

	/// <summary>
	/// Creates a cache set of the given type.
	/// </summary>
	public static CacheSet CreateCacheSet<T, ID>(string definitionName)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		// The contentFields provide the metadata about the fields in the target type.
		// It can be used to exclude fields if needed.
		var contentFields = new ContentFields(typeof(T));
		return new CacheSet<T, ID>(contentFields, definitionName);
	}

	/// <summary>
	/// MethodInfo for the CreateCacheSet method.
	/// </summary>
	private static MethodInfo _cacheSetCreateMethod;

	/// <summary>
	/// Gets the cache set for the given definition.
	/// </summary>
	/// <param name="definition"></param>
	/// <returns></returns>
	public CacheSet GetCacheForDefinition(Definition definition)
	{
		var defId = (int)definition.Id;
		defId -= (int)(Schema.ArchiveDefId + 1); // First defId through here should be 0.

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
			if (_meta.TryGetValue(definition.Name, out Type entityType))
			{
				// Got the entity type. Now create the cache set as well as the content fields info object for it:
				if (_cacheSetCreateMethod == null)
				{
					_cacheSetCreateMethod = typeof(CachingTransactionReader)
						.GetMethod(
							nameof(CreateCacheSet),
							BindingFlags.Static | BindingFlags.Public
						);
				}

				var setupType = _cacheSetCreateMethod.MakeGenericMethod(new Type[] {
					entityType,
					typeof(ulong) // Must always have a ulong ID
				});

				// Create the typed cacheSet now:
				set = (CacheSet)setupType.Invoke(this, new object[] {
					definition.Name
				});

				if (defId != 0)
				{
					_caches[defId] = set;
				}

			}
			else
			{
				// Type doesn't exist. Generic empty cache set - this simply exists to
				// avoid load attempts being spammed on every transaction.
				set = new CacheSet(null);
			}

			_caches[defId] = set;
		}

		return set;
	}

	/// <summary>
	/// Called when the given object is added into the given cache set.
	/// This reader is currently in the state of the transaction which triggered the add.
	/// </summary>
	/// <param name="caches"></param>
	/// <param name="obj"></param>
	protected virtual void OnCacheAdd(CacheSet caches, object obj)
	{
		
	}

	/// <summary>
	/// Called when the given object ID is removed from the given cache set.
	/// This reader is currently in the state of the transaction which triggered the remove.
	/// </summary>
	/// <param name="caches"></param>
	/// <param name="entityId"></param>
	protected virtual void OnCacheRemove(CacheSet caches, ulong entityId)
	{
		
	}
	
	/// <summary>
	/// Called when the given object is updated in the given cache. This reader is currently in the state of the transaction which triggered the update.
	/// </summary>
	/// <param name="caches"></param>
	/// <param name="obj"></param>
	protected virtual void OnCacheUpdate(CacheSet caches, object obj)
	{
		
	}

	/// <summary>
	/// Called when a transaction is in the buffer and has been validated by the default validation rules.
	/// Custom validation is handled via overriding InitialiseTransaction.
	/// </summary>
	/// <returns></returns>
	public override object ApplyTransaction()
	{
		// Based on the SS chain state loader and just runs entirely in memory for now.
		var defnId = Definition == null ? 0 : Definition.Id;
		CacheSet cache = null;
		FieldData[] fields = Fields;
		ulong txTimestamp = 0;
		object relevantObject = null;

		if (defnId > Schema.ArchiveDefId)
		{
			// An instance of something. This is the base instance (not a variant).

			// Set last instance timestamp:
			Definition.LastInstanceTimestamp = txTimestamp;

			cache = GetCacheForDefinition(Definition);

			if (cache == null)
			{
				// We don't care about this type. Skip it.
				return null;
			}

			var t = Activator.CreateInstance(cache.InstanceType);

			// For each field, get a suitable reader:
			for (var i = StartFieldsOffset; i < FieldCount; i++)
			{
				// Get the definition:
				var fieldDef = fields[i].Field;

				// Validation: Can instance this field
				if (!fieldDef.CanInstance)
				{
					// Invalid txn.
					return false;
				}

				// Ask the cache to map this field to type specific meta:
				var fieldMeta = cache.GetField(fieldDef);

				if (fieldMeta != null)
				{
					fieldMeta.FieldReader(t, fields, i, fieldDef.IsNullable);
				}
			}

			// Map timestamp to ticks:
			var createTimeUtc = Chain.TimestampToDateTime(txTimestamp);

			// Add to the primary cache - this also sets the Id and Created/EditedUtc fields:
			cache.Add(_loadContext, t, createTimeUtc, TransactionId);

			OnCacheAdd(cache, t);

			// The relevant object is the new instance:
			return t;
		}

		// Standard definitions. The correct way to identify these is that the definition.Name starts with "Blockchain." indicating a core definition.
		// However, all current core definitions have IDs that are <10 meaning we can use a fast switch statement to identify the process path.

		ulong currentDefId = 0;
		ulong currentEntityId = 0;
		int variantTypeId = 0;
		Definition definition = null;

		switch (defnId)
		{
			// Handling the defaults:
			case 0:
			case Schema.TransactionDefId:
			case Schema.FieldDefId:
			case Schema.EntityTypeId:
			case Schema.ProjectMetaDefId: // 4

				// Schema and project level. Default behaviour:
				return base.ApplyTransaction();

			case Schema.BlockBoundaryDefId: // 6

				// Block boundary.
				return base.ApplyTransaction();

			case Schema.SetFieldsDefId: // 7

				// Setting fields on an existing object. Used for updates usually. Note that SetField txns can occur on a variant too.

				// Special fields MAY be declared before Timestamp. After Timestamp is the user defined fields to set, which can include any field at all.
				// The first time the Timestamp field is encountered, it is the end of these special fields.
				// Note that DefinitionId is technically optional, but is always provided as it makes providing custom IDs possible.

				// Special fields may occur before the Timestamp.
				for (var i = 0; i < StartFieldsOffset; i++)
				{
					var fieldMeta = fields[i].Field;

					if (fieldMeta.Id == Schema.TimestampDefId)
					{
						// Timestamp. This will be used to set EditedUtc.
						txTimestamp = fields[i].NumericValue;

						if (currentEntityId != 0 && currentDefId != 0)
						{
							// Got both EntityId + DefinitionId.
							// Get the definition now:
							definition = Schema.Get((int)currentDefId);

							if (definition != null && currentDefId > Schema.ArchiveDefId)
							{
								cache = GetCacheForDefinition(definition);

								if (cache == null)
								{
									// We don't care about this type. Skip it.
									return null;
								}
							}
						}
					}
					else if (fieldMeta.Id == Schema.EntityDefId)
					{
						// EntityId
						currentEntityId = fields[i].NumericValue;
					}
					else if (fieldMeta.Id == Schema.DefId)
					{
						// DefinitionId
						currentDefId = fields[i].NumericValue;
					}
					else if (fieldMeta.Id == Schema.VariantTypeId)
					{
						// VariantTypeId
						variantTypeId = (int)fields[i].NumericValue;
					}
				}

				if (cache != null)
				{
					// Get the entity:
					object t = cache.Get(currentEntityId, 0);
					relevantObject = t;

					if (t != null)
					{
						for (var i = StartFieldsOffset; i < FieldCount; i++)
						{
							// Get the definition:
							var fieldDef = fields[i].Field;

							// Ask the cache to map this field to type specific meta:
							var fieldMeta = cache.GetField(fieldDef);

							if (fieldMeta != null)
							{
								fieldMeta.FieldReader(t, fields, i, fieldDef.IsNullable);
							}
						}
					}

					OnCacheUpdate(cache, t);

				}
				else if (definition != null)
				{
					// Setting fields on a field or definition.
				}

				break;
			case Schema.ArchiveDefId: // 8

				// Archived object(s). We use this to represent something that was deleted.
				// The targeted object could be an entity, variant or relationship (or technically even a part of the schema, but we don't use that here).

				// Just like SetFields, there MAY be special fields before Timestamp.
				// As with SetFields, DefinitionId is always provided for convenience.

				if (FieldCount >= 3) // DefinitionId, EntityId, Timestamp.
				{
					for (var i = 0; i < FieldCount; i++)
					{
						var fieldMeta = fields[i].Field;

						if (fieldMeta.Id == Schema.EntityDefId)
						{
							// EntityId
							currentEntityId = Fields[i].NumericValue;
						}
						else if (fieldMeta.Id == Schema.TimestampDefId)
						{
							// Timestamp field. It's the end of the special fields zone.

							// However archive txns don't have any user defined fields anyway.
							// startFieldsOffset = i + 1;

							if (currentEntityId != 0 && currentDefId != 0)
							{
								// Got both.
								// Get the definition now:
								definition = Schema.Get((int)currentDefId);
								if (definition != null)
								{
									cache = GetCacheForDefinition(definition);

									if (cache == null)
									{
										// We don't care about this type. Skip it.
										return null;
									}

									// Ask it to remove the object.
									cache.Remove(_loadContext, 0, currentEntityId);

									OnCacheRemove(cache, currentEntityId);

								}

							}

							// Stop processing fields.
							break;
						}
						else if (fieldMeta.Id == Schema.DefId)
						{
							// DefinitionId
							currentDefId = Fields[i].NumericValue;
						}
					}
				}

				break;
		}

		return relevantObject;
	}

}