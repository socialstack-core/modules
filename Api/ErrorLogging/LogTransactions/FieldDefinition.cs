using Api.SocketServerLibrary;
using System.Collections.Generic;

namespace Api.ErrorLogging;


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
}