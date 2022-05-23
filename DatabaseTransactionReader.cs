using Api.Contexts;
using Api.Startup;
using Lumity.BlockChains;
using System;
using System.Collections.Generic;

namespace Api.BlockDatabase;


/// <summary>
/// Txn reader which uses the Socialstack cache as its state tracker.
/// </summary>
public class DatabaseTransactionReader : TransactionReader
{
	private Context _loadContext = new Context(1, 1, 1);

	/// <summary>
	/// Applies the content of the valid transaction in the reader buffer.
	/// Returns a "relevant object" which was updated by the transaction (such as a schema field or a particular entity).
	/// </summary>
	public override object ApplyTransaction()
	{
		// Read a transaction (forward direction).
		// Depending on what kind of transaction it was, will likely need to update the caches that we're building.
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

			cache = Chain.GetCacheForDefinition(Definition);

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
			
			case Schema.TransferDefId: // 5

				// Fungible transfer. Socialstack doesn't directly create these, but they are supported (validated) anyway.

				break;
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
								cache = Chain.GetCacheForDefinition(definition);
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

#warning todo: might be setting fields on a localised object with variantTypeId

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
									cache = Chain.GetCacheForDefinition(definition);

									if (cache != null)
									{
										// Ask it to remove the object.
										cache.Remove(_loadContext, 0, currentEntityId);
									}
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