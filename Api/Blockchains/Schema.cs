using Api.SocketServerLibrary;
using System.Collections.Generic;

namespace Lumity.BlockChains;


/// <summary>
/// Block chain schema
/// </summary>
public class Schema
{
	/// The content types in the schema.
	public List<Definition> Definitions = new List<Definition>();

	/// <summary>
	/// Field Id for the "Timestamp" field on Blockchain.Transaction
	/// </summary>
	public const ulong TimestampDefId = 1;

	/// <summary>
	/// Field Id for the "Name" field
	/// </summary>
	public const ulong NameDefId = 2;

	/// <summary>
	/// Field Id for the "DataType" field on Blockchain.Field
	/// </summary>
	public const ulong DataTypeDefId = 3;

	/// <summary>
	/// Field Id for the "Immutable" common field, usable on fields.
	/// </summary>
	public const ulong ImmutableDefId = 4;
	
	/// <summary>
	/// Field Id for the "ByteOffset" common field, usable on fields.
	/// </summary>
	public const ulong ByteOffsetDefId = 19;
	
	/// <summary>
	/// Field Id for the "Signature" common field, usable on fields.
	/// </summary>
	public const ulong SignatureDefId = 20;

	/// <summary>
	/// Field Id for the "IfAlsoValid" common field.
	/// </summary>
	public const ulong IfAlsoValidDefId = 5;

	/// <summary>
	/// Field Id for the "IfNotModifiedSince" common field.
	/// </summary>
	public const ulong IfNotModifiedSinceDefId = 6;

	/// <summary>
	/// Field Id for the "IfGroupValid" common field.
	/// </summary>
	public const ulong IfGroupValidDefId = 7;

	/// <summary>
	/// Field Id for the "OwnerId" common field.
	/// </summary>
	public const ulong OwnerIdDefId = 8;

	/// <summary>
	/// Field Id for the "Id" common field.
	/// </summary>
	public const ulong IdDefId = 9;

	/// <summary>
	/// Field Id for the "BlockchainName" common field.
	/// </summary>
	public const ulong BlockchainNameDefId = 11;

	/// <summary>
	/// Field Id for the "EntityId" common field.
	/// </summary>
	public const ulong EntityDefId = 16;
	
	/// <summary>
	/// Field Id for the "DefinitionId" common field.
	/// </summary>
	public const ulong DefId = 17;

	/// <summary>
	/// Field Id for the "VariantTypeId" common field.
	/// </summary>
	public const ulong VariantTypeId = 21;

	/// <summary>
	/// Field Id for the "NodeId" field on any transaction, indicating which node it came from.
	/// </summary>
	public const ulong NodeId = 22;

	/// <summary>
	/// Field Id for the "AssemblerId" field on Blockchain.Meta
	/// </summary>
	public const ulong AssemblerDefId = 23;
	
	// - Standard definition IDs follow -

	/// <summary>
	/// Id to use when creating a new transaction type.
	/// </summary>
	public const ulong TransactionDefId = 1;

	/// <summary>
	/// Id to use when creating a new field.
	/// </summary>
	public const ulong FieldDefId = 2;

	/// <summary>
	/// Id to use when creating a new entity type.
	/// </summary>
	public const ulong EntityTypeId = 3;

	/// <summary>
	/// Id for project metadata.
	/// </summary>
	public const ulong ProjectMetaDefId = 4;
	
	/// <summary>
	/// Id of a fungible transfer.
	/// </summary>
	public const ulong TransferDefId = 5;
	
	/// <summary>
	/// Id of a block boundary.
	/// </summary>
	public const ulong BlockBoundaryDefId = 6;
	
	/// <summary>
	/// Id to use when setting fields on something.
	/// </summary>
	public const ulong SetFieldsDefId = 7;

	/// <summary>
	/// PublicKey field def id.
	/// </summary>
	public const ulong PublicKeyDefId = 12; 

	/// <summary>
	/// Id to use when archiving something.
	/// </summary>
	public const ulong ArchiveDefId = 8;

	/// <summary>
	/// Current field count.
	/// </summary>
	private List<FieldDefinition> Fields = new List<FieldDefinition>();

	/// <summary>
	/// Creates a new schema instance.
	/// </summary>
	public Schema()
	{
	}

	/// <summary>
	/// Creates default entries. 
	/// These usually originate from loading a blockchain, as they are as self-defining as possible. 
	/// This permits the schema to be fully non-English and flexible.
	/// </summary>
	public void CreateDefaults()
	{
		// Define the critical 6:
		Define("Blockchain.Transaction", 0);
		Define("Blockchain.Field", 1);
		Define("Blockchain.Type", 1);

		DefineField("Timestamp", "uint"); // Relative. Check Blockchain.Meta/StartYear. They default to being 10^9 (nanoseconds).
		DefineField("Name", "string");
		DefineField("DataType", "string");
		// DataTypes: uint, int, string, bytes, float4, float8. The string DataType is utf8; both uint and int are stored in a compressed format.

		// ID 1-3 for both fields and types are the ones which are internally important for loading the schema.
		// If the schema does not have them in it, they are treated as if they do exist. This is useful whilst the schema is loaded.

		// Common features:
		DefineField("Immutable", "uint"); // Has multiple modes which lock things to create a guarantee that something won't be created anymore. 1=This is locked,2=Instances of this are locked,3=Both. For example, if it's 1 and on a field, the fields name cannot be changed but field values can be. If it is 2, the name can be changed but field values cannot be (SetFields transaction would fail if using this field). Settable on at least Definition, FieldDefinition and instances. The default is 0, not immutable.
		DefineField("IfAlsoValid", "uint"); // If set, a transaction is valid if the given transaction is also valid. It is a relative offset (i.e. the value 3 means 3 transactions previous) and the referenced transaction is always in the same block.
		DefineField("IfNotModifiedSince", "uint"); // There must be no transactions using the source object since the given timestamp. The value can only be a timestamp.
		DefineField("IfGroupValid", "uint"); // The number of remaining transactions in the group, i.e. the first transaction in the group of 5 has a value of 4. The collective transactions are only valid if all of the other validations on transactions in the group pass. There are no 'gaps' in a group, and the entirety of a group is always in the same block. This is used where transactions are dependent on two or more transactions being successful, and if only one was, you want the other to also be invalid.
		DefineField("UserId", "uint"); // The owning entity of a given entity. Very commonly used to check if a particular user has the right to perform a transaction on a source entity.
		DefineField("Id", "uint"); // Used when importing a reference from another lumity blockchain. The ID is the transaction ID otherwise.
		
		// Definition for an entity:
		// NameDefId is used on these types

		// Definition for the blockchain meta:
		Define("Blockchain.Meta", 1); // 4
		DefineField("StartYear", "uint"); // Timestamps are the number of nanoseconds since the start of this year (10). If it is not specified, it is 2000.
		DefineField("BlockchainName", "string"); // Typicaly the name of the project (11)
		DefineField("PublicKey", "bytes"); // A public key (12)
		DefineField("Version", "uint"); // Blockchain version (optional: the default is 1) (13)
		DefineField("ServiceUrl", "string"); // The web location of the API which is currently running this chain (14)
		DefineField("ExecutableArchive", "bytes"); // The validator executable (an archive containing both source and a build) (15)

		// Defining some central transaction types:
		Define("Blockchain.Transfer", 1); // 5
		// EntityId and DefinitionId are present twice in a transfer; it is always source Id/Def followed by target Id/Def.
		DefineField("EntityId", "uint"); // A transaction ID (16)
		DefineField("DefinitionId", "uint"); // A definition ID. Optional but usually present as it makes indexing entities simpler. (17)

		DefineField("Quantity", "uint"); // Quantity being transferred (18)
		// Note: A transfer should also declare If Not Modified Since and use the latest known txID for the source entity.

		// Block boundary:
		Define("Blockchain.BlockBoundary", 1); // 6
		DefineField("ByteOffset", "uint"); // 19. Total byte offset
		DefineField("Signature", "bytes"); // 20. A signature created using the current assembler nodes private key and the hash of the block up to and including the byteOffset. The hash is seeded with the previous block hash, including the signature, or a hash of the project public key. The first block uses the project key.
		
		// Set fields (uses SourceEntityId).
		// This does mean you cannot change the Source Entity Id on other transactions, but that is fine - it would be meaningless to do so.
		Define("Blockchain.SetFields", 1); // 7
		// Declare Source Entity ID and DefinitionId
		// Also should declare If Not Modified Since (use the latest known txID for the source entity), or If Also Valid, or both.

		// Archive an entity (uses EntityId and DefinitionId). Used by CMS's to effectively mark something as gone but recoverable later if needed.
		Define("Blockchain.Archive", 1); // 8

		// Variants are used to declare that this transaction is setting fields on a 'variant' of the object.
		// Primarily for localised versions of something. I.e. you make your core thing in a default language, and then create variants of it for each language, overriding the fields which are variant.
		// This is used with the SetFields transaction, thus the target object is already known.
		// If the field is present, it MUST be present before EntityId and DefinitionId.
		DefineField("VariantTypeId", "uint"); // 21. Usually some form of locale reference but its meaning is up to the project.
		DefineField("NodeId", "uint"); // 22. When this is used as a special field, it indicates which node submitted the transaction.
		DefineField("AssemblerId", "uint"); // 23. Occurs in Blockchain.Meta transactions. Indicates a hand off has happened. Its default value is the first declared node.
		DefineField("TimestampPrecision", "uint"); // 24. Defaults to 10^9 (nanoseconds). The amount the timestamps on this chain go up by per second.

		// A transaction used to authenticate the node submitting it. Signature is required.
		// The source chain does not know what is being authenticated by design. 
		Define("Blockchain.Authenticate", 1); // 9
		DefineField("AuthenticationHash", "bytes"); // A hash which indicates to the remote that the transaction is for them and is valid.
		DefineField("TransactionHash", "bytes"); // First field in a transaction. The hash of everything else.
		DefineField("TransactionToken", "bytes"); // The transaction token used to form the hash. The hash is the first field in the txn and is of everything else, including this token.
		DefineField("NextTokenHash", "bytes"); // The hash of the next token to be used by this node.
	}

	/// <summary>
	/// Whilst a schema is loading from scratch, some critical definitions don't exist yet.
	/// This creates temporary ones to use whilst it instances them.
	/// </summary>
	/// <param name="defId"></param>
	/// <returns></returns>
	public Definition GetTemporaryCriticalDefinition(int defId)
	{
		switch (defId)
		{
			case 1:
				return new Definition()
				{
					Name = "Blockchain.Transaction",
					InheritedId = 0,
					Schema = this,
					Id = 1
				};
			case 2:
				return new Definition()
				{
					Name = "Blockchain.Field",
					InheritedId = 1,
					Schema = this,
					Id = 2
				};
			case 3:
				return new Definition()
				{
					Name = "Blockchain.Type",
					InheritedId = 1,
					Schema = this,
					Id = 3
				};
		}

		return null;
	}

	/// <summary>
	/// Whilst a schema is loading from scratch, some critical fields don't exist yet.
	/// This creates temporary ones to use whilst it instances them.
	/// </summary>
	/// <param name="fieldId"></param>
	/// <returns></returns>
	public FieldDefinition GetTemporaryCriticalField(int fieldId)
	{
		switch(fieldId)
		{
			case 1:
				// Timestamp
				return new FieldDefinition()
				{
					Name = "Timestamp",
					DataType = "uint",
					Schema = this,
					Id = 1
				};
			case 2:
				// Name
				return new FieldDefinition()
				{
					Name = "Name",
					DataType = "string",
					Schema = this,
					Id = 2
				};
			case 3:
				// DataType
				return new FieldDefinition()
				{
					Name = "DataType",
					DataType = "string",
					Schema = this,
					Id = 3
				};
		}

		return null;
	}

	/// <summary>
	/// Defines a field on the given definition.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="dataType"></param>
	/// <returns></returns>
	public FieldDefinition DefineField(string name, string dataType)
	{
		var fld = new FieldDefinition()
		{
			Name = name,
			DataType = dataType,
			Schema = this
		};

		Fields.Add(fld);
		fld.Id = (ulong)Fields.Count;
		return fld;
	}

	/// <summary>
	/// Find a definition by exact name match. Case sensitive.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public Definition FindDefinition(string name)
	{
		for (var i = 0; i < Definitions.Count; i++)
		{
			var def = Definitions[i];
			if (def.Name == name)
			{
				return def;
			}
		}

		return null;
	}

	/// <summary>
	/// Find a field definition by exact name match. Case sensitive.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="type"></param>
	/// <returns></returns>
	public FieldDefinition FindField(string name, string type)
	{
		for (var i = 0; i < Fields.Count; i++)
		{
			var def = Fields[i];
			if (def.Name == name && def.DataType == type)
			{
				return def;
			}
		}

		return null;
	}

	/// <summary>
	/// Defines an entity type in the schema of the blockchain.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public Definition Define(string name)
	{
		return Define(name, 3);
	}

	/// <summary>
	/// Defines something in the schema of the blockchain.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="inheritId">Id of the content type to inherit from.</param>
	/// <returns></returns>
	public Definition Define(string name, ulong inheritId)
	{
		var type = new Definition() {
			Name = name,
			InheritedId = inheritId,
			Schema = this
		};
		Definitions.Add(type);
		type.Id = (ulong)Definitions.Count;
		return type;
	}

	/// <summary>
	/// Gets a definition by the given ID.
	/// </summary>
	/// <param name="id"></param>
	public Definition Get(int id)
	{
		if (id == 0 || id > Definitions.Count)
		{
			return null;
		}
		return Definitions[id - 1];
	}

	/// <summary>
	/// Gets a field definition by the given ID.
	/// </summary>
	/// <param name="id"></param>
	public FieldDefinition GetField(int id)
	{
		if (id == 0 || id > Fields.Count)
		{
			return null;
		}
		return Fields[id - 1];
	}

	/// <summary>
	/// Outputs the transactions required to define this whole schema.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="timestamp"></param>
	public void Write(Writer writer, ulong timestamp)
	{
		// Create each ctype:
		for (var i = 0; i < Definitions.Count; i++)
		{
			// Entity create tx:
			Definitions[i].WriteCreate(writer, timestamp);
		}

		// And each field:
		for (var i = 0; i < Fields.Count; i++)
		{
			// Create it:
			Fields[i].WriteCreate(writer, timestamp);
		}
	}
}