using Api.SocketServerLibrary;
using System.Collections.Generic;

namespace Api.ErrorLogging;


/// <summary>
/// Block chain schema
/// </summary>
public class Schema
{
	/// The content types in the schema.
	public List<Definition> Definitions = new List<Definition>();

	/// <summary>
	/// Field Id for the "Timestamp" field
	/// </summary>
	public const ulong TimestampFieldDefId = 1;

	/// <summary>
	/// Field Id for the "Tag" field
	/// </summary>
	public const ulong TagFieldDefId = 4;

	/// <summary>
	/// Field Id for the "Message" field
	/// </summary>
	public const ulong MessageFieldDefId = 5;

	/// <summary>
	/// Field Id for the "ExceptionMessage" field
	/// </summary>
	public const ulong StackTraceFieldDefId = 6;

	/// <summary>
	/// Field Id for the "Meta" (json) field
	/// </summary>
	public const ulong MetaFieldDefId = 7;

	/// <summary>
	/// Id for the "Ok" definition.
	/// </summary>
	public const ulong OkId = 4;

	/// <summary>
	/// Id for the "Info" definition.
	/// </summary>
	public const ulong InfoId = 5;

	/// <summary>
	/// Id for the "Warn" definition.
	/// </summary>
	public const ulong WarnId = 6;

	/// <summary>
	/// Id for the "Error" definition.
	/// </summary>
	public const ulong ErrorId = 7;

	/// <summary>
	/// Id for the "Fatal" definition.
	/// </summary>
	public const ulong FatalId = 8;

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
	/// Creates default schema entries.
	/// </summary>
	public void CreateDefaults()
	{
		// Define the critical 6:
		Define("Blockchain.Transaction", 0);
		Define("Blockchain.Field", 1);
		Define("Blockchain.Type", 1);

		DefineField("Timestamp", "uint");
		DefineField("Name", "string");
		DefineField("DataType", "string");

		DefineField("Tag", "string"); // Tag, field 4.
		DefineField("Message", "string"); // Message, field 5.
		DefineField("StackTrace", "string"); // StackTrace, field 6.
		DefineField("Meta", "string"); // Meta, field 7.

		Define("Ok", 3); // OK log entry, type 4.
		Define("Info", 3); // INFO log entry, type 5.
		Define("Warn", 3); // WARN log entry, type 6.
		Define("Error", 3); // ERROR log entry, type 7.
		Define("Fatal", 3); // FATAL log entry, type 8.
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
}