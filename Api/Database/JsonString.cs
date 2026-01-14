using System;

namespace Api.Database;

/// <summary>
/// Exists as a type alias for textual JSON fields.
/// </summary>
public struct JsonString : IEquatable<JsonString>, IEquatable<string>
{
	/// <summary>
	/// The raw value.
	/// </summary>
	private string _value;

	/// <summary>
	/// Creates a new json string.
	/// </summary>
	/// <param name="val"></param>
	public JsonString(string val)
	{
		if (string.IsNullOrEmpty(val))
		{
			_value = null;
		}
		else
		{
			_value = val;
		}
	}
	
	/// <summary>
	/// Added a check for null value
	/// so a value can be assigned.
	/// </summary>
	public bool HasValue => _value != null;

	/// <summary>
	/// True if the jsonString values are equal.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(JsonString other)
	{
		return _value == other._value;
	}

	/// <summary>
	/// True if the jsonString values are equal.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(string other)
	{
		return _value == other;
	}

	/// <summary>
	/// True if the given object equals this jsonString instance.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public override bool Equals(object obj) => obj is JsonString other && _value == other._value;

	/// <summary>
	/// Gets a hashcode for this JsonString instance.
	/// </summary>
	/// <returns></returns>
	public override int GetHashCode() => _value == null ? 0 : _value.GetHashCode();

	/// <summary>
	/// Equals operator, used by Diff.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool Equals(JsonString left, JsonString right)
	{
		return left._value == right._value;
	}

	/// <summary>
	/// Equals operator for jsonstrings.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator ==(JsonString left, JsonString right) => left._value == right._value;

	/// <summary>
	/// Not equals operator for jsonstrings.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator !=(JsonString left, JsonString right) => (left._value != right._value);

	/// <summary>
	/// The JSON string - can be null.
	/// </summary>
	/// <returns></returns>
	public string ValueOf()
	{
		return _value;
	}

	/// <summary>
	/// The JSON string itself.
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return _value;
	}
}