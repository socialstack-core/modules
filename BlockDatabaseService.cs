using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Lumity.BlockChains;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.BlockDatabase;

/// <summary>
/// Handles creation of blocks.
/// </summary>
[LoadPriority(1)]
public partial class BlockDatabaseService : AutoService
{
	/// <summary>
	/// Gets the blockchain of a given type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public BlockChain GetChain(ChainType type)
	{
		return _project.GetChain(type);
	}

	/// <summary>
	/// Writes a diff of the given object as a SetFields transaction.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="newFields"></param>
	/// <param name="originalFields"></param>
	/// <param name="differ">Differ write function</param>
	/// <param name="definition">ID of the object being written.</param>
	/// <param name="chain">Chain to write to.</param>
	/// <param name="entityId">Chain to write to</param>
	public async ValueTask<TransactionResult> WriteDiff<T>(T newFields, T originalFields, Func<T, T, Writer, int> differ, Definition definition, BlockChain chain, ulong entityId)
	{
		var writer = Writer.GetPooled();
		writer.Start(null);

		var now = chain.Timestamp;

		// Create a buffer which will be written out repeatedly:

		// SetFields txn:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.SetFieldsDefId);

		// Field count is currently unknown, so we'll write a single byte which will limit these txn's to ~250 fields in one go.
		// Not a limit of the chain - is just a limit of this particular function which aims to not have to move bytes around later.
		// If you happen to need more, just make multiple txns.
		writer.WriteInvertibleCompressed(250);

		var fieldCountBuffer = writer.LastBuffer;
		var fieldCountOffset = writer.CurrentFill - 1;

		// DefinitionId:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.DefId);
		writer.WriteInvertibleCompressed(definition.Id);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.DefId);

		// EntityId
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.EntityDefId);
		writer.WriteInvertibleCompressed(entityId);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.EntityDefId);

		// Timestamp, denoting the end of the 'special fields' section of SetFields:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(now);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.TimestampDefId);

		// +3 for the timestamp, defId, EntityId
		var fieldCount = differ(newFields, originalFields, writer) + 3;

		// Note that 0 is also valid. Whilst it should be rare it would have the likely intended side effect of updating the timestamp (EditedUtc) only.

		if (fieldCount > 250)
		{
			throw new Exception("Requested transaction is too big. This mechanism is currently limited to outputting single byte field counts only (a max of 250). Your transaction needed " + fieldCount);
		}

		// Update the field count:
		fieldCountBuffer.Bytes[fieldCountOffset] = (byte)fieldCount;

		// Field count again (for readers going backwards):
		writer.WriteInvertibleCompressed((ulong)fieldCount);

		// Definition again (for readers going backwards):
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.SetFieldsDefId);

		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		// If we're the BAS, add directly to the chain.
		// Otherwise, add remotely.
		return await chain.Write(now, first, last);
	}

	/// <summary>
	/// Writes the given object as a transaction using the given meta for the type T.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="obj"></param>
	/// <param name="fieldWriter"></param>
	/// <param name="definition"></param>
	/// <param name="chain"></param>
	/// <param name="optionalEntityId">Optionally will write an Id field. Otherwise, the entity ID is the resulting transaction ID.</param>
	public async ValueTask<TransactionResult> Write<T>(T obj, Func<T, Writer, int> fieldWriter, Definition definition, BlockChain chain, ulong optionalEntityId = 0)
	{
		var writer = Writer.GetPooled();
		writer.Start(null);

		var now = chain.Timestamp;

		// Create a buffer which will be written out repeatedly:

		// Creating an instance of the definition:
		writer.WriteInvertibleCompressed(definition.Id);

		// Field count is currently unknown, so we'll write a single byte which will limit these txn's to ~250 fields in one go.
		// Not a limit of the chain - is just a limit of this particular function which aims to not have to move bytes around later.
		// If you happen to need more, just make multiple txns.
		writer.WriteInvertibleCompressed(250);

		var fieldCountBuffer = writer.LastBuffer;
		var fieldCountOffset = writer.CurrentFill - 1;

		// +1 for the timestamp.
		var additionalFields = 1;

		if (optionalEntityId != 0)
		{
			// +2 for timestamp + ID.
			additionalFields = 2;

			// Got an ID to write:
			writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.IdDefId);
			writer.WriteInvertibleCompressed(optionalEntityId);
			writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.IdDefId);
		}

		// Timestamp:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(now);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.TimestampDefId);

		// +1 for timestamp:
		var fieldCount = fieldWriter(obj, writer) + additionalFields;

		if (fieldCount > 250)
		{
			throw new Exception("Requested transaction is too big. This mechanism is currently limited to outputting single byte field counts only (a max of 250). Your transaction needed " + fieldCount);
		}

		// Update the field count:
		fieldCountBuffer.Bytes[fieldCountOffset] = (byte)fieldCount;

		// Field count again (for readers going backwards):
		writer.WriteInvertibleCompressed((ulong)fieldCount);
		
		// Definition again (for readers going backwards):
		writer.WriteInvertibleCompressed(definition.Id);

		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		// If we're the BAS, add directly to the chain.
		// Otherwise, add remotely.
		return await chain.Write(now, first, last);
	}

	/// <summary>
	/// Writes the given object as a transaction using the given meta for the type T.
	/// </summary>
	/// <param name="sourceEntityId"></param>
	/// <param name="definition"></param>
	/// <param name="chain"></param>
	public async ValueTask<TransactionResult> WriteArchived(ulong sourceEntityId, Definition definition, BlockChain chain)
	{
		var writer = Writer.GetPooled();
		writer.Start(null);

		var now = chain.Timestamp;

		// Create a buffer which will be written out repeatedly:

		// Creating an archive entry:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.ArchiveDefId);

		// 3 fields:
		writer.WriteInvertibleCompressed(3);

		// DefinitionId:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.DefId);
		writer.WriteInvertibleCompressed(definition.Id);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.DefId);

		// EntityId:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.EntityDefId);
		writer.WriteInvertibleCompressed(sourceEntityId);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.EntityDefId);

		// Timestamp, denoting the end of the special fields in a txn:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(now);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.TimestampDefId);

		// Field count again (for readers going backwards):
		writer.WriteInvertibleCompressed(3);

		// Definition again (for readers going backwards):
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.ArchiveDefId);

		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		return await chain.Write(now, first, last);
	}

	/*
	public T GetResult<T, ID>(
		Context context, ID id, Type instanceType, BlockTableMeta meta
	)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		T result = null;

		meta.Chain.LoadForwards((TransactionReader reader) => {

			var defn = reader.Definition;

			if (defn == null || defn.Id != meta.Id)
			{
				return;
			}

			// Got a record we're interested in.
			// For now it's the whole thing.
			var t = (T)Activator.CreateInstance(instanceType);

			meta.ReadObject(t, reader);

			if (t.Id.Equals(id))
			{
				result = t;
				reader.Halt = true;
			}

		});

		return result;
	}

	public int GetResults<T, ID>(
		Context context, QueryPair<T, ID> queryPair, Type instanceType, BlockTableMeta meta
	)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		var index = 0;

		meta.Chain.LoadForwards((TransactionReader reader) => {

			var defn = reader.Definition;

			if (defn == null || defn.Id != meta.Id)
			{
				return;
			}

			// Got a record we're interested in.
			// For now it's the whole thing.
			var t = (T)Activator.CreateInstance(instanceType);

			meta.ReadObject(t, reader);

			queryPair.OnResult(context, t, index, queryPair.SrcA, queryPair.SrcB);
			index++;
		});

		return index;
	}
	*/

	private void BuildServiceMeta(List<Type> serviceTypes)
	{
		var set = new Dictionary<string, AutoServiceMeta>();

		var chainFieldIO = new ChainFieldIO();
		var createMethod = typeof(BlockChain).GetMethod(nameof(BlockChain.CreateCacheSet), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
		
		foreach (var type in serviceTypes)
		{
			// First get the AS type - it is AutoService<T,ID> or null:
			var autoServiceType = Services.GetAutoServiceType(type);

			if (autoServiceType == null)
			{
				continue;
			}

			// This is an autoService with a content type.
			// Get the content type and ID type:
			var genericTypes = autoServiceType.GenericTypeArguments;

			var meta = new AutoServiceMeta() {
				ServicedType = genericTypes[0],
				IdType = genericTypes[1],
				EntityName = genericTypes[0].Name,
				ContentFields = new ContentFields(genericTypes[0])
			};

			// Invoke createMethod:
			var setupType = createMethod.MakeGenericMethod(new Type[] {
						meta.ServicedType,
						meta.IdType
					});

			meta.CacheSet = (CacheSet)setupType.Invoke(this, new object[] {
				meta
			});

			foreach (var field in meta.ContentFields.List)
			{
				if (field.FieldInfo != null)
				{
					// Establish its data type:
					var fieldType = field.FieldType;

					// Get metadata attributes:
					var metaAttribs = field.FieldInfo.GetCustomAttributes(typeof(DatabaseFieldAttribute), true);
					DatabaseFieldAttribute fieldMeta = null;

					if (metaAttribs.Length > 0)
					{
						fieldMeta = (DatabaseFieldAttribute)metaAttribs[0];
					}

					if (fieldMeta != null)
					{
						if (fieldMeta.Ignore)
						{
							// Ignore this field.
							continue;
						}
					}

					// Next, which chain is this field intended for?
					// This depends on if the field has JsonIgnore or is "hidden" by default by the permission system fields.
					var jsonIgnoreAttrib = field.FieldInfo.GetCustomAttribute<Newtonsoft.Json.JsonIgnoreAttribute>();

					if (jsonIgnoreAttrib != null)
					{
						// Private chain field. Values for this field will be stored on the private chain only.
						field.IsPrivate = true;
					}
					else
					{
						var permsAttrib = field.FieldInfo.DeclaringType.GetCustomAttribute<Permissions.PermissionsAttribute>();

						if (permsAttrib != null && permsAttrib.HideFieldByDefault)
						{
							field.IsPrivate = true;
						}

						permsAttrib = field.FieldInfo.GetCustomAttribute<Permissions.PermissionsAttribute>();

						if (permsAttrib != null)
						{
							// This may override the declaration on the type:
							field.IsPrivate = permsAttrib.HideFieldByDefault;
						}
					}

					var nullableType = Nullable.GetUnderlyingType(fieldType);

					if (nullableType != null)
					{
						fieldType = nullableType;
					}

					if (_typeMap.TryGetValue(fieldType, out BlockDatabaseType dbType))
					{
						field.DataType = dbType.TypeName;
						field.IsUnsigned = dbType.IsUnsigned;
					}
					else
					{
						throw new Exception(
							"Field '" + field.Name + "' on '" + field.FieldInfo.DeclaringType +
							"' is a " + field.FieldType.Name + " which isn't currently supported as a field type."
						);
					}

					chainFieldIO.AddField(field);
				}
			}

			set[meta.EntityName.ToLower()] = meta;
		}

		chainFieldIO.Bake();

		// After the bake call, each ContentField has a reader/ writer set.

		serviceMeta = set;
	}

	private Dictionary<string, AutoServiceMeta> serviceMeta;

	/// <summary>
	/// Gets the svc meta for the given type name.
	/// </summary>
	/// <param name="contentTypeName"></param>
	/// <returns></returns>
	private AutoServiceMeta GetMeta(string contentTypeName)
	{
		if (!serviceMeta.TryGetValue(contentTypeName.ToLower(), out AutoServiceMeta result))
		{
			return null;
		}

		return result;
	}

	private Context _loadContext = new Context(1, 1, 1);

	/// <summary>
	/// Field information for the BlockChainProject type which allow setting fields on the project itself.
	/// </summary>
	private List<ContentField> _projectFieldSetters;

	/// <summary>
	/// Applies the content of the given transaction to the caches.
	/// </summary>
	/// <param name="reader"></param>
	public bool ApplyTransaction(TransactionReader reader)
	{
		// Read a transaction (forward direction).
		// Depending on what kind of transaction it was, will likely need to update the caches that we're building.
		var defnId = reader.Definition == null ? 0 : reader.Definition.Id;
		CacheSet cache = null;
		FieldData[] fields;
		int startFieldsOffset;
		ulong txTimestamp = 0;
		object relevantObject = null;

		// Locate Timestamp field.
		fields = reader.Fields;
		startFieldsOffset = 0;
		var isValid = true;
		ulong? ifNotModifiedSince = null;

		for (var i = 0; i < reader.FieldCount; i++)
		{
			var fieldMeta = fields[i].Field;

			if (fieldMeta.Id == Lumity.BlockChains.Schema.TimestampDefId)
			{
				// Timestamp. This will be used to set EditedUtc.
				txTimestamp = reader.Fields[i].NumericValue;
				reader.Timestamp = txTimestamp;

				// Timestamp marks the end of the special fields.
				startFieldsOffset = i + 1;
				break;
			}
			else if (fieldMeta.Id == Lumity.BlockChains.Schema.IdDefId)
			{
				// Use the declared ID as if it was the txnID:
				reader.TransactionId = fields[i].NumericValue;
			}
			else if (fieldMeta.Id == Lumity.BlockChains.Schema.NodeId)
			{
				// Get the node ID:
				reader.NodeId = fields[i].NumericValue;
			}
			else if (fieldMeta.Id == Lumity.BlockChains.Schema.IfAlsoValidDefId)
			{
				// NumericValue is the number of bytes to remove from the txId to get to the target txId.
				if (!reader.IsTransactionInBlockValid(reader.TransactionByteOffset - fields[i].NumericValue))
				{
					// This txn isn't valid.
					isValid = false;
				}
			}
			else if (fieldMeta.Id == Lumity.BlockChains.Schema.IfNotModifiedSinceDefId)
			{
				ifNotModifiedSince = fields[i].NumericValue;
			}
		}

		if (!isValid)
		{
			return false;
		}

		if (defnId > Lumity.BlockChains.Schema.ArchiveDefId)
		{
			// An instance of something. This is the base instance (not a variant).

			// Validation: Check if this can be instanced.
			if (!reader.Definition.CanInstance)
			{
				// Invalid txn.
				return false;
			}

			if (ifNotModifiedSince.HasValue)
			{
				// LastInstanceTimestamp must not be greater than the given value:
				if (reader.Definition.LastInstanceTimestamp > ifNotModifiedSince.Value)
				{
					// Invalid transaction.
					return false;
				}
			}

			// Set last instance timestamp:
			reader.Definition.LastInstanceTimestamp = txTimestamp;

			cache = reader.Chain.GetCacheForDefinition(reader.Definition, serviceMeta);

			var t = Activator.CreateInstance(cache.InstanceType);

			// For each field, get a suitable reader:
			for (var i = startFieldsOffset; i < reader.FieldCount; i++)
			{
				// Get the definition:
				var fieldDef = fields[i].Field;

				// Ask the cache to map this field to type specific meta:
				var fieldMeta = cache.GetField(fieldDef);

				if (fieldMeta != null)
				{
					// Validation: Can instance this field
					if (!fieldDef.CanInstance)
					{
						// Invalid txn.
						return false;
					}

					fieldMeta.FieldReader(t, fields, i, fieldDef.IsNullable);
				}
			}

			// Map timestamp to ticks:
			var createTimeUtc = reader.Chain.TimestampToDateTime(txTimestamp);

			// Add to the primary cache - this also sets the Id and Created/EditedUtc fields:
			cache.Add(_loadContext, t, createTimeUtc, reader.TransactionId);

			relevantObject = t;
		}
		else
		{

			// Standard definitions. The correct way to identify these is that the definition.Name starts with "Blockchain." indicating a core definition.
			// However, all current core definitions have IDs that are <10 meaning we can use a fast switch statement to identify the process path.

			ulong currentDefId = 0;
			ulong currentEntityId = 0;
			int variantTypeId = 0;
			Definition definition = null;

			switch (defnId)
			{
				// 0 to 3 are handled by the schema system.
				case Lumity.BlockChains.Schema.ProjectMetaDefId: // 4

					// General project metadata.
					// Works the same as SetFields but specifically targets the project metadata.
					var metadata = reader.Project;

					fields = reader.Fields;
					startFieldsOffset = 0;

					if (reader.FieldCount > 0)
					{
						// Special fields may occur before the Timestamp.
						for (var i = 0; i < reader.FieldCount; i++)
						{
							var fieldMeta = fields[i].Field;

							if (fieldMeta.Id == Lumity.BlockChains.Schema.TimestampDefId)
							{
								// Timestamp.

								// Timestamp marks the end of the special fields.
								startFieldsOffset = i + 1;
								break;
							}
						}

						for (var i = startFieldsOffset; i < reader.FieldCount; i++)
						{
							// Get the definition:
							var fieldDef = fields[i].Field;

							if (!fieldDef.CanSet)
							{
								// Invalid txn.
								return false;
							}
						}

						// Setting fields on metadata
						if (_projectFieldSetters == null)
						{
							// Generate the field writers:
							var chainIO = new ChainFieldIO();
							chainIO.GenerateForType(typeof(BlockChainProject));
							_projectFieldSetters = chainIO.Fields;
						}

						for (var i = startFieldsOffset; i < reader.FieldCount; i++)
						{
							// Get the definition:
							var fieldDef = fields[i].Field;

							// Find by that definition name (doesn't happen very often at all so this simple search is fine):
							for (var t = 0; t < _projectFieldSetters.Count; t++) {

								if (_projectFieldSetters[t].Name == fieldDef.Name)
								{
									_projectFieldSetters[t].FieldReader(metadata, fields, i, fieldDef.IsNullable);
									break;
								}

							}
						}

					}

					metadata.Updated();
					relevantObject = metadata;

					break;
				case Lumity.BlockChains.Schema.TransferDefId: // 5

					// Fungible transfer. Socialstack doesn't directly create these, but they are supported anyway.

					break;
				case Lumity.BlockChains.Schema.BlockBoundaryDefId: // 6

					// Block boundary. When one of these is encountered you MUST validate it.

					break;
				case Lumity.BlockChains.Schema.SetFieldsDefId: // 7

					// Setting fields on an existing object. Used for updates usually. Note that SetField txns can occur on a variant too.

					// Special fields MAY be declared before Timestamp. After Timestamp is the user defined fields to set, which can include any field at all.
					// The first time the Timestamp field is encountered, it is the end of these special fields.
					// Note that DefinitionId is technically optional, but is always provided as it makes providing custom IDs possible.
					fields = reader.Fields;
					startFieldsOffset = 0;

					if (reader.FieldCount > 2)
					{
						// Special fields may occur before the Timestamp.
						for (var i = 0; i < reader.FieldCount; i++)
						{
							var fieldMeta = fields[i].Field;

							if (fieldMeta.Id == Lumity.BlockChains.Schema.TimestampDefId)
							{
								// Timestamp. This will be used to set EditedUtc.
								txTimestamp = reader.Fields[i].NumericValue;

								if (currentEntityId != 0 && currentDefId != 0)
								{
									// Got both EntityId + DefinitionId.
									// Get the definition now:
									definition = reader.Schema.Get((int)currentDefId);

									if (definition != null && currentDefId > Lumity.BlockChains.Schema.ArchiveDefId)
									{
										cache = reader.Chain.GetCacheForDefinition(definition, serviceMeta);
									}
								}

								// Timestamp marks the end of the special fields.
								startFieldsOffset = i + 1;
								break;
							}
							else if (fieldMeta.Id == Lumity.BlockChains.Schema.EntityDefId)
							{
								// EntityId
								currentEntityId = reader.Fields[i].NumericValue;
							}
							else if (fieldMeta.Id == Lumity.BlockChains.Schema.DefId)
							{
								// DefinitionId
								currentDefId = reader.Fields[i].NumericValue;
							}
							else if (fieldMeta.Id == Lumity.BlockChains.Schema.VariantTypeId)
							{
								// VariantTypeId
								variantTypeId = (int)reader.Fields[i].NumericValue;
							}
						}

						for (var i = startFieldsOffset; i < reader.FieldCount; i++)
						{
							// Get the definition:
							var fieldDef = fields[i].Field;

							if (!fieldDef.CanSet)
							{
								// Invalid txn.
								return false;
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
								for (var i = startFieldsOffset; i < reader.FieldCount; i++)
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
					}

					break;
				case Lumity.BlockChains.Schema.ArchiveDefId: // 8

					// Archived object(s). We use this to represent something that was deleted.
					// The targeted object could be an entity, variant or relationship (or technically even a part of the schema, but we don't use that here).

					// Just like SetFields, there MAY be special fields before Timestamp.
					// As with SetFields, DefinitionId is always provided for convenience.
					fields = reader.Fields;

					if (reader.FieldCount >= 3) // DefinitionId, EntityId, Timestamp.
					{
						for (var i = 0; i < reader.FieldCount; i++)
						{
							var fieldMeta = fields[i].Field;

							if (fieldMeta.Id == Lumity.BlockChains.Schema.EntityDefId)
							{
								// EntityId
								currentEntityId = reader.Fields[i].NumericValue;

							}
							else if (fieldMeta.Id == Lumity.BlockChains.Schema.TimestampDefId)
							{
								// Timestamp field. It's the end of the special fields zone.

								// However archive txns don't have any user defined fields anyway.
								// startFieldsOffset = i + 1;

								if (currentEntityId != 0 && currentDefId != 0)
								{
									// Got both.
									// Get the definition now:
									definition = reader.Schema.Get((int)currentDefId);
									if (definition != null)
									{
										cache = reader.Chain.GetCacheForDefinition(definition, serviceMeta);

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
							else if (fieldMeta.Id == Lumity.BlockChains.Schema.DefId)
							{
								// DefinitionId
								currentDefId = reader.Fields[i].NumericValue;
							}
						}
					}

					break;
			}
		}

		reader.Chain.UpdatePending(txTimestamp, reader.TransactionId, reader.NodeId, relevantObject, true);
		return true;
	}

	/// <summary>
	/// Instanced automatically. Supports dependency injection.
	/// </summary>
	public BlockDatabaseService()
	{
		// Create system type -> chain type map:
		_typeMap = new Dictionary<Type, BlockDatabaseType>();
		_typeMap[typeof(string)] = new BlockDatabaseType("string");
		_typeMap[typeof(byte[])] = new BlockDatabaseType("bytes");
		_typeMap[typeof(bool)] = new BlockDatabaseType("uint", true);
		_typeMap[typeof(sbyte)] = new BlockDatabaseType("int");
		_typeMap[typeof(byte)] = new BlockDatabaseType("uint", true);
		_typeMap[typeof(int)] = new BlockDatabaseType("int");
		_typeMap[typeof(uint)] = new BlockDatabaseType("uint", true);
		_typeMap[typeof(short)] = new BlockDatabaseType("int");
		_typeMap[typeof(ushort)] = new BlockDatabaseType("uint", true);
		_typeMap[typeof(long)] = new BlockDatabaseType("int");
		_typeMap[typeof(ulong)] = new BlockDatabaseType("uint", true);
		_typeMap[typeof(float)] = new BlockDatabaseType("float4");
		_typeMap[typeof(DateTime)] = new BlockDatabaseType("uint", true);
		_typeMap[typeof(double)] = new BlockDatabaseType("float8");

		// Build all service metadata now:
		BuildServiceMeta(Services.AllServiceTypes);

		// Get path to data directory:
		string dbPath = SetupDirectory();

		// One Lumity project per socialstack instance:
		_project = new BlockChainProject();

		// Load now:
		_project.Load(dbPath, ApplyTransaction);

		var setupHandlersMethod = typeof(Init).GetMethod(nameof(Init.SetupServiceHandlers), BindingFlags.Static  | BindingFlags.Public);
		
		// Now that the caches are loaded, can apply each cache to each service instance.
		// This happens in the BeforeLoad event handler.
		Events.Service.BeforeCreate.AddEventListener(async (Context ctx, AutoService service) => {

			// Service only services pass through here too. Make sure we have a service with an entity:
			if (service.EntityName == null)
			{
				return service;
			}
				
			// Check if a cache exists for this service:
			var svcMeta = GetMeta(service.EntityName);

			if (svcMeta == null)
			{
				if (!service.IsMapping)
				{
					throw new Exception("Missing metadata for '" + service.EntityName + "' indicates a fault in the block loader. Fully runtime generated types aren't supported here yet.");
				}

				// Generate mapping meta now. The definition is probably not known at this point.
				// If it doesn't exist, it is generated by the setupHandlersMethod below.
				svcMeta = _project.GetChain(ChainType.Public).GenerateMappingMeta(service.MappingSourceIdType, service.MappingTargetIdType, service.EntityName, serviceMeta);
			}

			// Apply the content fields which include various bits of block metadata such as type/ field definitions.
			service.SetContentFields(svcMeta.ContentFields);
			
			// If type derives from DatabaseRow, we have a thing we'll potentially need to reconfigure.
			if (ContentTypes.IsAssignableToGenericType(service.ServicedType, typeof(Content<>)))
			{
				var servicedType = service.ServicedType;

				// Add data load events:
				var setupType = setupHandlersMethod.MakeGenericMethod(new Type[] {
						servicedType,
						service.IdType
					});

				await (ValueTask)setupType.Invoke(null, new object[] {
						this,
						service
					});
			}

			// Must only apply the cache after we've set handlers up as cache load events may trigger the creation of some default content.
			await service.ApplyCache(svcMeta.CacheSet);
			
			return service;
		}, 3); 
		
	}

	private Dictionary<Type, BlockDatabaseType> _typeMap;
	private BlockChainProject _project;

	/// <summary>
	/// The project for this site.
	/// </summary>
	public BlockChainProject Project => _project;

	/// <summary>
	/// Gets a definition from system type. Cache the result when possible. Null if not found.
	/// </summary>
	/// <param name="systemType"></param>
	/// <param name="chainType"></param>
	/// <returns></returns>
	public Definition GetDefinition(Type systemType, ChainType chainType)
	{
		var name = systemType.Name;
		return _project.GetChain(chainType).Schema.FindDefinition(name);
	}

	/// <summary>
	/// Sets up the db directory
	/// </summary>
	/// <returns></returns>
	private string SetupDirectory()
	{
		var contentPath = "Content/database/";

		contentPath = Path.GetFullPath(contentPath);

		if (!Directory.Exists(contentPath))
		{
			Directory.CreateDirectory(contentPath);
		}

		return contentPath;
	}

}

/// <summary>
/// Stores info about auto services before they have started.
/// </summary>
public class AutoServiceMeta
{
	/// <summary>
	/// The content type for this service. Can be null.
	/// </summary>
	public Type ServicedType;

	/// <summary>
	/// The ID type for this service. Can be null.
	/// </summary>
	public Type IdType;

	/// <summary>
	/// The entity name to use.
	/// </summary>
	public string EntityName;

	/// <summary>
	/// The cache set for this autoservice.
	/// </summary>
	public CacheSet CacheSet;

	/// <summary>
	/// The content fields set for this auto services content type.
	/// </summary>
	public ContentFields ContentFields;
}

