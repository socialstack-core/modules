using Api.SocketServerLibrary;
using System.Collections.Generic;

namespace Lumity.BlockChains;


/// <summary>
/// Schema for a particular field.
/// </summary>
public partial class FieldDefinition
{
	/// <summary>
	/// Global field ID.
	/// </summary>
	public ulong Id;

	/// <summary>
	/// Parent schema
	/// </summary>
	public Schema Schema;

	/// <summary>
	/// Immutable flags of this definition. 1=Definition itself can't be changed except for Immutable fields, 2=Can't instance, 4=Can't set.
	/// Note that the immutable set exception can only ever get stricter, i.e. it is not possible to make something non-immutable using this exclusion.
	/// </summary>
	public uint Immutable
	{
		get
		{
			return _immutable;
		}
		set
		{
			_immutable = value;
			CanSet = (_immutable & 4) == 0;
			CanInstance = (_immutable & 2) == 0;
			CanUpdateDefinition = (_immutable & 1) == 0;
		}
	}

	private uint _immutable;

	/// <summary>
	/// Derived from the immutable flags.
	/// </summary>
	public bool CanSet = true;
	/// <summary>
	/// Derived from the immutable flags.
	/// </summary>
	public bool CanInstance = true;
	/// <summary>
	/// Derived from the immutable flags.
	/// </summary>
	public bool CanUpdateDefinition = true;

	/// <summary>
	/// Standard name for the name of a field
	/// </summary>
	public string Name;

	/// <summary>
	/// Standard name for the data type of a field
	/// </summary>
	private string _dataType;

	/// <summary>
	/// Standard name for the data type of a field
	/// </summary>
	public string DataType {
		get {
			return _dataType;
		}
		set {
			_dataType = value;
			BuildDataTypeMeta();
		}
	}

	/// <summary>
	/// The field data size. If this is -1, there is a compressed number at the start of the field value.
	/// </summary>
	public int FieldDataSize => _fieldDataSize;

	/// <summary>
	/// True if the field data size is actually just the value to use (if a variable size is in the field). There may be some additional manipulation of that value required.
	/// </summary>
	public bool SizeIsValue => _sizeIsValue;
	
	/// <summary>
	/// True if the field value is nullable. Size is actually +1 as 0 indicates null, and a size of 1 indicates a 0 length entity.
	/// </summary>
	public bool IsNullable => _isNullable;

	/// <summary>
	/// The field data size. If this is -1, there is a compressed number at the start of the field value.
	/// </summary>
	private int _fieldDataSize;

	/// <summary>
	/// True if the field data size is actually just the value to use. There may be some additional manipulation required.
	/// </summary>
	private bool _sizeIsValue;

	/// <summary>
	/// True if the field value is nullable. Size is actually +1 as 0 indicates null, and a size of 1 indicates a 0 length entity.
	/// </summary>
	private bool _isNullable;

	/// <summary>
	/// Gets the atom reader to use for this field.
	/// </summary>
	/// <returns></returns>
	private void BuildDataTypeMeta()
	{
		// uint, int, string, bytes, float4, float8
		if (_dataType == "uint")
		{
			_fieldDataSize = -1;
			_sizeIsValue = true;
			_isNullable = false;
		}
		else if (_dataType == "int")
		{
			_fieldDataSize = -1;
			_sizeIsValue = true;
			_isNullable = false;
		}
		else if (_dataType == "float4")
		{
			_fieldDataSize = 4;
			_sizeIsValue = false;
			_isNullable = false;
		}
		else if (_dataType == "float8")
		{
			_fieldDataSize = 8;
			_sizeIsValue = false;
			_isNullable = false;
		}
		else if (_dataType == "string")
		{
			_fieldDataSize = -1;
			_sizeIsValue = false;
			_isNullable = true;
		}
		else if (_dataType == "bytes")
		{
			_fieldDataSize = -1;
			_sizeIsValue = false;
			_isNullable = true;
		}
		else
		{
			// This client does not recognise the data type.
			throw new System.Exception("Unknown data type used by a field: '" + _dataType + "'");
		}

	}

	/// <summary>
	/// Writes this field to the given writer.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="timestamp"></param>
	public void WriteCreate(Writer writer, ulong timestamp)
	{
		// Entity create tx:
		writer.WriteInvertibleCompressed(2);

		writer.WriteInvertibleCompressed(3);

		// Timestamp:
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(timestamp);
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);

		// Name:
		writer.WriteInvertibleCompressed(Schema.NameDefId);
		writer.WriteInvertibleUTF8(Name);
		writer.WriteInvertibleCompressed(Schema.NameDefId);

		// Data type:
		writer.WriteInvertibleCompressed(Schema.DataTypeDefId);
		writer.WriteInvertibleUTF8(DataType);
		writer.WriteInvertibleCompressed(Schema.DataTypeDefId);

		// Field count again (for readers travelling backwards):
		writer.WriteInvertibleCompressed(3);

		// Entity create again (for readers travelling backwards):
		writer.WriteInvertibleCompressed(2);
	}
}