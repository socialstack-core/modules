using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Signatures;
using Api.SocketServerLibrary;
using Api.SocketServerLibrary.Crypto;
using Api.Startup;
using Lumity.BlockChains;
using Microsoft.Extensions.Configuration;
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
	/// Distribution which occurs if this node is the BAS.
	/// </summary>
	private BlockDistributor _distributor;

	/// <summary>
	/// The keypair to use when signing transactions and blocks from this node.
	/// </summary>
	public KeyPair SelfKeyPair;

	/// <summary>
	/// Distribution which occurs if this node is the BAS.
	/// </summary>
	public BlockDistributor Distributor => _distributor;

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

		// The node ID (can be 0 at the start of the chain). As it is a special field it must occur before timestamp:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.NodeId);
		writer.WriteInvertibleCompressed(_project.SelfNodeId);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.NodeId);
		
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

		// +4 for the timestamp, defId, EntityId, node Id
		var fieldCount = differ(newFields, originalFields, writer) + 4;

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

		// +2 for the timestamp and node Id.
		var additionalFields = 2;

		if (optionalEntityId != 0)
		{
			// +3 for timestamp + ID + node Id.
			additionalFields = 3;

			// Got an ID to write:
			writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.IdDefId);
			writer.WriteInvertibleCompressed(optionalEntityId);
			writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.IdDefId);
		}

		// The node ID (can be 0 at the start of the chain). As it is a special field it must occur before timestamp:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.NodeId);
		writer.WriteInvertibleCompressed(_project.SelfNodeId);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.NodeId);
		
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

		// 4 fields:
		writer.WriteInvertibleCompressed(4);

		// The node ID (can be 0 at the start of the chain). As it is a special field it must occur before timestamp:
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.NodeId);
		writer.WriteInvertibleCompressed(_project.SelfNodeId);
		writer.WriteInvertibleCompressed(Lumity.BlockChains.Schema.NodeId);
		
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
		writer.WriteInvertibleCompressed(4);

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

		// Try loading local node key file if it exists:
		var keyFile = "nodeKey.json";

		if (File.Exists(keyFile))
		{
			var json = File.ReadAllText(keyFile);
			SelfKeyPair = KeyPair.FromSerialized(json);
		}
		else
		{
			SelfKeyPair = KeyPair.Generate();
			var json = SelfKeyPair.Serialize();
			File.WriteAllText(keyFile, json);
		}

		// Set self key:
		_project.SelfPrivateKey = SelfKeyPair.PrivateKeyBytes;
		_project.SelfPublicKey = SelfKeyPair.PublicKey.Q.GetEncoded(false);

		// Get config from appsettings file:
		var bc = Api.Configuration.AppSettings.GetSection("Blockchain");

		// To make handling the digest easier, it's simplest if the public key is known before load starts.
		var baseConfig = bc.Get<BlockChainProjectConfig>();

		if (baseConfig.PublicKey != null)
		{
			_project.PublicKey = Convert.FromBase64String(baseConfig.PublicKey);
			_project.PrivateKey = Convert.FromBase64String(baseConfig.PrivateKey);
		}
		else
		{
			// Generate the project key:
			_project.GenerateKeyPair();

			// Console.WriteLine("Project private key: " + Convert.ToBase64String(_project.PrivateKey));
			// Console.WriteLine("Project public key: " + Convert.ToBase64String(_project.PublicKey));
		}
		
		// Setup project hash:
		var sha3 = new Sha3Digest();
		sha3.BlockUpdate(_project.PublicKey, 0, _project.PublicKey.Length);

		Span<byte> pubHash = stackalloc byte[32];
		sha3.DoFinal(pubHash, 0);

		_project.PublicHash = Hex.Convert(pubHash);

		// Load now:
		_project.Load(dbPath, typeof(DatabaseTransactionReader), (TransactionReader reader) => {
			
			// Called when a reader has been instanced and is about to be used.

			// Hook up the txn reader with custom state:
			// reader.

		}, (BlockChain chain) => {

			// Called when a chain has been instanced and is about to load.

			// Hook up the chain with custom state:
			chain.SetServiceMeta(serviceMeta);

		});

		_project.Distribution = baseConfig.Distribution;
		
		// Create a distributor:
		_distributor = new BlockDistributor(_project);

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

