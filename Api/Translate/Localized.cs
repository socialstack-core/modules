using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Api.Translate;


/// <summary>
/// Base general interface for localized structs.
/// </summary>
public interface ILocalized
{
	/// <summary>
	/// Gets the value as text from this Localized value for the given locale in the given context.
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	string GetStringValue(Context context);
}

/// <summary>
/// Use this to declare a field of the specified type as localised.
/// The type is often string but you can localise anything else - IDs etc.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct Localized<T> : ILocalized, IEquatable<Localized<T>>, IComparable<Localized<T>>, IComparable
{
	/// <summary>
	/// Parses the given JSON as a Localized struct, then boxes it. 
	/// The result is never null but the source text can be.
	/// </summary>
	/// <param name="src"></param>
	/// <returns></returns>
	public static object ParseBoxed(string src)
	{
		if (string.IsNullOrEmpty(src))
		{
			return new Localized<T>();
		}

		return Parse(src);
	}

	/// <summary>
	/// Parses the given JSON as a Localized struct. 
	/// The source text can be null.
	/// </summary>
	/// <param name="src"></param>
	/// <returns></returns>
	public static Localized<T> Parse(string src)
	{
		var result = new Localized<T>();
		if (string.IsNullOrWhiteSpace(src))
			return result;

		ReadOnlySpan<char> span = src.AsSpan();
		int i = 0;

		SkipWhitespace(span, ref i);
		if (i >= span.Length || span[i++] != '{')
			return result;

		while (i < span.Length)
		{
			SkipWhitespace(span, ref i);
			if (span[i] == '}') break;

			// --- Parse key ---
			if (span[i++] != '"') break;
			var key = ParseJsonString(span, ref i);

			SkipWhitespace(span, ref i);
			if (span[i++] != ':') break;
			SkipWhitespace(span, ref i);

			// --- Parse value ---
			T value;
			if (typeof(T) == typeof(string))
			{
				if (span[i++] != '"') break;
				var val = ParseJsonString(span, ref i);
				value = (T)(object)val;
			}
			else if (typeof(T) == typeof(JsonString))
			{
				int valStart = i;
				i = FindSubJsonEnd(span, i);
				var valSpan = span.Slice(valStart, i - valStart).Trim();
				var val = new JsonString(new string(valSpan));
				value = (T)(object)val;
			}
			else
			{
				int valStart = i;
				while (i < span.Length && span[i] != ',' && span[i] != '}') i++;
				var valSpan = span.Slice(valStart, i - valStart).Trim();

				if (!TryConvertNumber(valSpan, out value))
					break;
			}

			result.SetInternalUseOnly(key, value);

			SkipWhitespace(span, ref i);
			if (i < span.Length && span[i] == ',')
			{
				i++;
				continue;
			}
			else if (i < span.Length && span[i] == '}')
			{
				break;
			}
		}

		return result;
	}

	private static int FindSubJsonEnd(ReadOnlySpan<char> span, int i)
	{
		int depth = 0;
		bool insideString = false;
		bool escaped = false;

		while (i < span.Length)
		{
			var current = span[i];

			if (insideString)
			{
				if (escaped)
				{
					// Ignore this char.
					escaped = false;
				}
				else if (current == '\\')
				{
					escaped = true;
				}
				else if (current == '"')
				{
					escaped = false;
					insideString = false;
				}
			}
			else if (depth == 0)
			{
				if (current == ',' || current == '}')
				{
					return i;
				}
				else if (current == '"')
				{
					insideString = true;
				}
				else if (current == '{' || current == '[')
				{
					depth++;
				}
			}
			else if (current == '}' || current == ']')
			{
				depth--;
			}
			else if (current == '{' || current == '[')
			{
				depth++;
			}
			else if (current == '"')
			{
				insideString = true;
			}

			i++;
		}

		return i;
	}

	private static string ParseJsonString(ReadOnlySpan<char> span, ref int i)
	{
		int start = i;
		var sb = new StringBuilder();

		while (i < span.Length)
		{
			char c = span[i++];
			if (c == '"') break;

			if (c == '\\' && i < span.Length)
			{
				char next = span[i++];
				switch (next)
				{
					case '"': sb.Append('"'); break;
					case '\\': sb.Append('\\'); break;
					case '/': sb.Append('/'); break;
					case 'b': sb.Append('\b'); break;
					case 'f': sb.Append('\f'); break;
					case 'n': sb.Append('\n'); break;
					case 'r': sb.Append('\r'); break;
					case 't': sb.Append('\t'); break;
					case 'u':
						if (i + 4 <= span.Length &&
							ushort.TryParse(span.Slice(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort cp))
						{
							sb.Append((char)cp);
							i += 4;
						}
						break;
					default:
						// Invalid escape sequence; preserve as-is
						sb.Append('\\').Append(next);
						break;
				}
			}
			else
			{
				sb.Append(c);
			}
		}

		return sb.ToString();
	}

	private static void SkipWhitespace(ReadOnlySpan<char> s, ref int i)
	{
		while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
	}

	private static bool TryConvertNumber(ReadOnlySpan<char> span, out T value)
	{
		object parsed = null;
		bool success = false;

		if (typeof(T) == typeof(int))
		{
			success = int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v);
			parsed = v;
		}
		else if (typeof(T) == typeof(uint))
		{
			success = uint.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v);
			parsed = v;
		}
		else if (typeof(T) == typeof(ulong))
		{
			success = ulong.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v);
			parsed = v;
		}
		else if (typeof(T) == typeof(ulong?))
		{
			if (span.SequenceEqual("null".AsSpan()))
			{
				value = (T)(object)null;
				return true;
			}
			success = ulong.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v);
			parsed = (ulong?)v;
		}
		else if (typeof(T) == typeof(float))
		{
			success = float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var v);
			parsed = v;
		}
		else if (typeof(T) == typeof(float?))
		{
			if (span.SequenceEqual("null".AsSpan()))
			{
				value = (T)(object)null;
				return true;
			}
			success = float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var v);
			parsed = (float?)v;
		}
		else if (typeof(T) == typeof(double))
		{
			success = double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var v);
			parsed = v;
		}
		else if (typeof(T) == typeof(bool))
		{
			success = span.Length == 1 && (span[0] == '1' || span[0] == '0');
			parsed = span[0] == '1';
		}
		else if (typeof(T) == typeof(double?))
		{
			if (span.SequenceEqual("null".AsSpan()))
			{
				value = (T)(object)null;
				return true;
			}
			success = double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var v);
			parsed = (double?)v;
		}
		else if (typeof(T) == typeof(decimal))
		{
			success = decimal.TryParse(span, NumberStyles.Number, CultureInfo.InvariantCulture, out var v);
			parsed = v;
		}
		else if (typeof(T) == typeof(decimal?))
		{
			if (span.SequenceEqual("null".AsSpan()))
			{
				value = (T)(object)null;
				return true;
			}
			success = decimal.TryParse(span, NumberStyles.Number, CultureInfo.InvariantCulture, out var v);
			parsed = (decimal?)v;
		}
		else
		{
			throw new NotSupportedException($"Type {typeof(T)} is not supported in Localized<T>.Parse.");
		}

		value = success ? (T)parsed : default;
		return success;
	}

	/// <summary>
	/// True if the localized objects are equal.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(Localized<T> other)
	{
		return Localized<T>.Equals(this, other);
	}

	/// <summary>
	/// True if the given object equals this localized instance.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public override bool Equals(object obj) => obj is Localized<T> other && Equals(other);

	/// <summary>
	/// Gets a hashcode for this localized instance.
	/// </summary>
	/// <returns></returns>
	public override int GetHashCode() => _values == null ? 0 : _values.GetHashCode();

	/// <summary>
	/// Equals operator for Localized types.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator ==(Localized<T> left, Localized<T> right) => Equals(left, right);

	/// <summary>
	/// Not equals operator for Localized types.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator !=(Localized<T> left, Localized<T> right) => !Equals(left, right);

	/// <summary>
	/// The underlying locale code to value lookup.
	/// </summary>
	private Dictionary<string, T> _values;

	/// <summary>
	/// Set a specific localised value.
	/// </summary>
	/// <param name="locale"></param>
	/// <param name="value"></param>
	public void Set(Locale locale, T value)
	{
		if (_values == null)
		{
			_values = new();
		}

		_values[locale.Code] = value;
	}

	/// <summary>
	/// Set a specific localised value.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="value"></param>
	public void Set(Context context, T value)
	{
		Set(context.LocaleId, value);
	}

	/// <summary>
	/// Set the value for "en".
	/// </summary>
	/// <param name="value"></param>
	public void SetFallback(T value)
	{
		if (_values == null)
		{
			_values = new();
		}

		_values["en"] = value;
	}

	/// <summary>
	/// Set a specific localised value.
	/// </summary>
	/// <param name="localeId"></param>
	/// <param name="value"></param>
	public void Set(uint localeId, T value)
	{
		var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ?
			ContentTypes.Locales[localeId - 1] : null);

		if (locale == null)
		{
			throw new System.Exception("Locale #" + localeId + " does not exist.");
		}

		if (_values == null)
		{
			_values = new();
		}

		_values[locale.Code] = value;
	}

	/// <summary>
	/// Gets the value for the current locale, returning true if it was able to do so.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool TryGet(Context context, out T value)
	{
		var localeId = context.LocaleId;
		var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ?
			ContentTypes.Locales[localeId - 1] : null);
		return TryGet(locale, out value);
	}

	/// <summary>
	/// Gets the value for the current locale.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="fallback"></param>
	/// <returns></returns>
	public T Get(Context context, bool fallback = true)
	{
		var localeId = context.LocaleId;
		var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ?
			ContentTypes.Locales[localeId - 1] : null);
		return Get(locale, fallback);
	}

	/// <summary>
	/// Gets the value for the specified locale.
	/// </summary>
	/// <param name="localeId"></param>
	/// <param name="fallback"></param>
	/// <returns></returns>
	public T Get(uint localeId, bool fallback = true)
	{
		var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ?
			ContentTypes.Locales[localeId - 1] : null);
		return Get(locale, fallback);
	}

	/// <summary>
	/// Only used by serialisers. Sets the underlying value whilst loading this type.
	/// Always use Set instead.
	/// </summary>
	/// <param name="localeCode"></param>
	/// <param name="value"></param>
	public void SetInternalUseOnly(string localeCode, T value)
	{
		if (_values == null)
		{
			_values = new();
		}

		_values[localeCode] = value;
	}

	/// <summary>
	/// Get a specific localised value.
	/// </summary>
	/// <param name="locale"></param>
	/// <param name="fallback"></param>
	/// <returns></returns>
	public T Get(Locale locale, bool fallback = true)
	{
		if (_values == null)
		{
			return default;
		}

		T val;

		if (locale != null)
		{
			if (_values.TryGetValue(locale.Code, out val))
			{
				return val;
			}

			if (locale.Id == 1)
			{
				// It's the primary locale. Don't fallback here.
				return default;
			}
		}

		if (!fallback)
		{
			return default;
		}

		if (_values.TryGetValue("en", out val))
		{
			return val;
		}

		return default;
	}

	/// <summary>
	/// Get by locale code.
	/// </summary>
	/// <param name="localeCode"></param>
	/// <returns></returns>
	public T Get(string localeCode)
	{
		if (_values == null)
		{
			return default;
		}

		_values.TryGetValue(localeCode, out T value);
		return value;
	}

	/// <summary>
	/// Get by locale code.
	/// </summary>
	/// <param name="localeCode"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool TryGet(string localeCode, out T value)
	{
		if (_values == null)
		{
			value = default;
			return false;
		}

		return _values.TryGetValue(localeCode, out value);
	}

	/// <summary>
	/// Get a specific localised value.
	/// </summary>
	/// <param name="locale"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool TryGet(Locale locale, out T value)
	{
		if (_values == null)
		{
			value = default;
			return false;
		}

		if (locale != null)
		{
			if (_values.TryGetValue(locale.Code, out value))
			{
				return true;
			}
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Gets the fallback locale value, or the default value otherwise.
	/// </summary>
	/// <returns></returns>
	public T GetFallback()
	{
		if (_values == null)
		{
			return default;
		}

		if (_values.TryGetValue("en", out T val))
		{
			return val;
		}

		return default;
	}

	/// <summary>
	/// Iterate through all the localised values in this set.
	/// </summary>
	public IReadOnlyDictionary<string, T> Values => _values;

	/// <summary>
	/// Creates a new localized instance.
	/// </summary>
	public Localized()
	{
	}

	/// <summary>
	/// Creates a new localized instance with a specific default value for the default locale.
	/// </summary>
	public Localized(T en)
	{
		_values = new();
		_values["en"] = en;
	}

	/// <summary>
	/// Creates a new localized instance with a specific default value for the given locale.
	/// </summary>
	public Localized(Locale initialLocale, T val)
	{
		_values = new();
		_values[initialLocale.Code] = val;
	}

	/// <summary>
	/// Builds this as a JSON string inside the given writer.
	/// </summary>
	/// <param name="writer"></param>
	public void ToJson(Writer writer)
	{
		writer.Write((byte)'{');

		if (_values != null)
		{
			bool first = true;
			foreach (var kvp in _values)
			{
				if (typeof(T) == typeof(JsonString))
				{
					var val = (JsonString)((object)kvp.Value);
					if (val.ValueOf() == null)
					{
						continue;
					}

					if (first)
					{
						first = false;
					}
					else
					{
						writer.Write((byte)',');
					}

					writer.Write((byte)'"');
					writer.WriteASCII(kvp.Key);
					writer.WriteASCII("\":");
					writer.Write(val);
				}
				else
				{
					if (first)
					{
						first = false;
					}
					else
					{
						writer.Write((byte)',');
					}

					writer.Write((byte)'"');
					writer.WriteASCII(kvp.Key);
					writer.WriteASCII("\":");

					if (typeof(T) == typeof(string))
					{
						writer.WriteEscaped((string)((object)kvp.Value));
					}
					else if (typeof(T) == typeof(bool))
					{
						var val = (bool)((object)kvp.Value);
						writer.WriteASCII(val ? "1" : "0");
					}
					else
					{
						writer.WriteS(Convert.ToString(kvp.Value, CultureInfo.InvariantCulture));
					}
				}
			}
		}

		writer.Write((byte)'}');
	}

	/// <summary>
	/// Builds this as a JSON string.
	/// </summary>
	/// <returns></returns>
	public string ToJson()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append('{');

		if (_values != null)
		{
			bool first = true;
			foreach (var kvp in _values)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					sb.Append(',');
				}

				sb.Append('"');
				sb.Append(kvp.Key);
				sb.Append("\":");
				AppendValue(sb, kvp.Value);
			}
		}

		sb.Append('}');
		return sb.ToString();
	}

	private static void AppendValue(StringBuilder sb, T value)
	{
		if (value == null)
		{
			sb.Append("null");
			return;
		}

		if (typeof(T) == typeof(string))
		{
			sb.Append('"');
			EscapeString(sb, value.ToString());
			sb.Append('"');
		}
		else if (typeof(T) == typeof(JsonString))
		{
			var val = value.ToString();
			if (val == null)
			{
				sb.Append("null");
			}
			else
			{
				sb.Append(val);
			}
		}
		else
		{
			// Format using invariant culture to preserve dot-decimal format, etc.
			sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
		}
	}

	private static void EscapeString(StringBuilder sb, string s)
	{
		if (string.IsNullOrEmpty(s))
			return;

		foreach (char c in s)
		{
			switch (c)
			{
				case '\\': sb.Append("\\\\"); break;
				case '"': sb.Append("\\\""); break;
				case '\b': sb.Append("\\b"); break;
				case '\f': sb.Append("\\f"); break;
				case '\n': sb.Append("\\n"); break;
				case '\r': sb.Append("\\r"); break;
				case '\t': sb.Append("\\t"); break;
				default:
					if (char.IsControl(c))
						sb.Append("\\u").Append(((int)c).ToString("x4"));
					else
						sb.Append(c);
					break;
			}
		}
	}

	/// <summary>
	/// Builds this as a JSON string.
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return ToJson();
	}

	/// <summary>
	/// Gets the value as text from this Localized value for the given locale in the given context.
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	public string GetStringValue(Context context)
	{
		var val = Get(context);
		return val == null ? null : val.ToString();
	}

	/// <summary>
	/// True if this localized set is empty.
	/// </summary>
	public bool IsEmpty => _values == null || _values.Count == 0;

	/// <summary>
	/// The number of entries in the set.
	/// </summary>
	public int Count => _values == null ? 0 : _values.Count;

	/// <summary>
	/// Compares the two given localized structs. Used by Diff.
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static bool Equals(Localized<T> a, Localized<T> b)
	{
		if (a._values == b._values)
		{
			return true;
		}

		var aCount = a.Count;
		var bCount = b.Count;

		if (aCount != bCount)
		{
			// Different counts.
			return false;
		}

		if (aCount == 0)
		{
			// They both must be empty.
			return true;
		}

		// Both are not empty, but can still be different.
		foreach (var kvp in a._values)
		{
			if (!b._values.TryGetValue(kvp.Key, out T bValue))
				return false;

			if (!object.Equals(kvp.Value, bValue))
				return false;
		}

		return true;
	}

	int IComparable<Localized<T>>.CompareTo(Localized<T> other)
	{
		var a = GetFallback() as IComparable;
		var b = other.GetFallback() as IComparable;

		if (a == null)
		{
			return b == null ? 0 : 1;
		}
		else if (b == null)
		{
			return -1;
		}
		
		return a.CompareTo(b);
	}

	int IComparable.CompareTo(object obj)
	{
		var objB = obj as Localized<T>?;

		if (!objB.HasValue)
		{
			return 1;
		}

		var a = GetFallback() as IComparable;
		var b = objB.Value.GetFallback() as IComparable;

		if (a == null)
		{
			return b == null ? 0 : 1;
		}
		else if (b == null)
		{
			return -1;
		}

		return a.CompareTo(b);
	}
}

/// <summary>
/// A localized converter that takes a generic template.
/// </summary>
/// <typeparam name="T"></typeparam>
public class LocalizedConverter<T> : JsonConverter<Localized<T>>
{
	/// <summary>
	/// A method to read the localized JSON
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="objectType"></param>
	/// <param name="existingValue"></param>
	/// <param name="hasExistingValue"></param>
	/// <param name="serializer"></param>
	/// <returns></returns>
    public override Localized<T> ReadJson(JsonReader reader, Type objectType, Localized<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Read raw JSON string of the object
        var obj = JObject.Load(reader);
        return Localized<T>.Parse(obj.ToString());
    }
	
	/// <summary>
	/// A method to write the localized JSON.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="value"></param>
	/// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, Localized<T> value, JsonSerializer serializer)
    {
    	serializer.Serialize(writer, value.Values);
    }
}