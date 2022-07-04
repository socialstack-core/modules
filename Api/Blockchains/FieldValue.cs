namespace Lumity.BlockChains;

/// <summary>
/// Simple API for defining a field and a value.
/// </summary>
public struct FieldValue
{
	/// <summary>
	/// The field.
	/// </summary>
	public FieldDefinition Field;
	
	/// <summary>
	/// The field value.
	/// </summary>
	public object Value;


	/// <summary>
	/// Create a new field/value pair.
	/// </summary>
	/// <param name="field"></param>
	/// <param name="value"></param>
	public FieldValue(FieldDefinition field, object value)
	{
		Field = field;
		Value = value;
	}
}