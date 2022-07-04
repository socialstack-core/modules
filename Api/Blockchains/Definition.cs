using Api.SocketServerLibrary;
using System.Collections.Generic;

namespace Lumity.BlockChains;

/// <summary>
/// A definition of something. Can inherit other definitions.
/// </summary>
public partial class Definition
{
	/// <summary>
	/// ID of the definition. Simply the incremental number, starting from 1, of the definition as seen in the blockchain defined order.
	/// </summary>
	public ulong Id;

	/// <summary>
	/// Parent schema
	/// </summary>
	public Schema Schema;

	/// <summary>
	/// Inherited definition ID.
	/// </summary>
	public ulong InheritedId;

	/// <summary>
	/// Immutable flags of this definition. 1=Definition itself can't be changed except for Immutable itself, 2=Can't instance objects, 3=Can't do either.
	/// Note that the immutable set exception can only ever get stricter, i.e. it is not possible to make something non-immutable using this exclusion.
	/// </summary>
	public uint Immutable {
		get {
			return _immutable;
		}
		set {
			_immutable = value;
			CanInstance = (_immutable & 2) == 0;
			CanUpdateDefinition = (_immutable & 1) == 0;
		}
	}

	private uint _immutable;

	/// <summary>
	/// Latest instance timestamp. When NotModifiedSince is present on a create row, it is 
	/// referring to the last time something was created and can be used to regulate the creation of entities.
	/// </summary>
	public ulong LastInstanceTimestamp;

	/// <summary>
	/// Derived from the immutable flags.
	/// </summary>
	public bool CanInstance = true;
	/// <summary>
	/// Derived from the immutable flags.
	/// </summary>
	public bool CanUpdateDefinition = true;

	/// <summary>
	/// Standard name for the name of a content type (field 1 on the ContentType type)
	/// </summary>
	public string Name;

	/// <summary>
	/// Outputs the transactions required to create this type.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="timestamp"></param>
	public void WriteCreate(Writer writer, ulong timestamp)
	{
		// Creating a thing of the inherited ID:
		writer.WriteInvertibleCompressed(InheritedId);

		// 2 fields:
		writer.WriteInvertibleCompressed(2);

		// Timestamp:
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(timestamp);
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);

		// Name:
		writer.WriteInvertibleCompressed(Schema.NameDefId);
		writer.WriteInvertibleUTF8(Name);
		writer.WriteInvertibleCompressed(Schema.NameDefId);

		// 2 fields (again, for readers going backwards):
		writer.WriteInvertibleCompressed(2);
		writer.WriteInvertibleCompressed(InheritedId);
	}

}