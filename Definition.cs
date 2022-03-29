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