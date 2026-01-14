using Api.SocketServerLibrary;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Api.Database;


/// <summary>
/// Holds the set of mapping data for a given entity, such as its list of tags. 
/// Use the mapping APIs to read/ write them.
/// </summary>
public struct MappingData
{
	/// <summary>
	/// Parses the given JSON as a MappingData struct. 
	/// The source text can be null.
	/// </summary>
	/// <param name="src"></param>
	/// <returns></returns>
	public static MappingData Parse(string src)
	{
		var result = new MappingData();
		if (string.IsNullOrWhiteSpace(src) || src == "null")
		{
			return result;
		}

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
			if (span[i++] != '[') break;
			var value = ParseJsonArray(span, ref i);
			result.Set(key, value);

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
	
	private static List<ulong> ParseJsonArray(ReadOnlySpan<char> span, ref int i)
	{
		var result = new List<ulong>();

		while (i < span.Length)
		{
			SkipWhitespace(span, ref i);

			if (i >= span.Length)
				break;

			if (span[i] == ']')
			{
				i++;
				break;
			}

			int start = i;

			// Find the end of the current number
			while (i < span.Length && span[i] != ',' && span[i] != ']')
				i++;

			var token = span.Slice(start, i - start).Trim();

			if (token.Length > 0 && ulong.TryParse(token, out ulong value))
			{
				result.Add(value);
			}

			// If current char is ',', skip it
			if (i < span.Length && span[i] == ',')
				i++;
		}

		return result;
	}
	private static void SkipWhitespace(ReadOnlySpan<char> s, ref int i)
	{
		while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
	}

	/// <summary>
	/// The underlying mapping name to ID set lookup.
	/// </summary>
	private Dictionary<string, List<ulong>> _values;

	/// <summary>
	/// Checks if the given item is in the specified mapping.
	/// </summary>
	/// <typeparam name="ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <param name="item"></param>
	public bool Has<ID>(string mappingName, Content<ID> item)
		where ID : struct, IEquatable<ID>, IConvertible, IComparable<ID>
	{
		if (item == null)
		{
			throw new ArgumentNullException("item");
		}

		return Has<ID>(mappingName, item.Id);
	}

	/// <summary>
	/// True if the given mapping has the given ID.
	/// </summary>
	/// <typeparam name="ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	public bool Has<ID>(string mappingName, ID id)
		where ID : struct, IEquatable<ID>, IConvertible, IComparable<ID>
	{
		if (typeof(ID) == typeof(uint))
		{
			var uid = id as uint?;
			return Has(mappingName, uid.Value);
		}
		else if (typeof(ID) == typeof(ulong))
		{
			var uid = id as ulong?;
			return Has(mappingName, uid.Value);
		}
		else
		{
			throw new NotImplementedException("Unknown ID type " + typeof(ID));
		}
	}

	/// <summary>
	/// Checks if the given mapping contains the specified ID.
	/// </summary>
	/// <param name="mappingName"></param>
	/// <param name="id"></param>
	public bool Has(string mappingName, ulong id)
	{
		if (_values == null)
		{
			return false;
		}

		mappingName = mappingName.ToLower();

		if (!_values.TryGetValue(mappingName, out List<ulong> set))
		{
			return false;
		}

		return set.Contains(id);
	}
	/// <summary>
	/// True if *all* of the given set are *not* present in the specified mapping. If the set is empty, this is true.
	/// </summary>
	/// <typeparam name="ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <param name="ids"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public bool HasNone<ID>(string mappingName, IEnumerable<ID> ids)
		where ID : struct, IEquatable<ID>, IConvertible, IComparable<ID>
	{
		if (_values == null)
		{
			// Faster than a .count():
			return !ids.FirstOrDefault().Equals(default(ID));
		}

		mappingName = mappingName.ToLower();

		if (!_values.TryGetValue(mappingName, out List<ulong> set))
		{
			// Faster than a .count():
			return !ids.FirstOrDefault().Equals(default(ID));
		}

		if (typeof(ID) == typeof(uint))
		{
			foreach (var id in ids)
			{
				if (set.Contains((id as uint?).Value))
				{
					return false;
				}
			}

			return true;
		}
		else if (typeof(ID) == typeof(ulong))
		{
			foreach (var id in ids)
			{
				if (set.Contains((id as ulong?).Value))
				{
					return false;
				}
			}

			return true;
		}
		else
		{
			throw new NotImplementedException("Unknown ID type " + typeof(ID));
		}
	}

	/// <summary>
	/// True if *all* of the given set are present in the specified mapping. If the set is empty, this is true.
	/// </summary>
	/// <typeparam name="ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <param name="ids"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public bool HasAll<ID>(string mappingName, IEnumerable<ID> ids)
		where ID : struct, IEquatable<ID>, IConvertible, IComparable<ID>
	{
		if (_values == null)
		{
			// Faster than a .count():
			return !ids.FirstOrDefault().Equals(default(ID));
		}

		mappingName = mappingName.ToLower();

		if (!_values.TryGetValue(mappingName, out List<ulong> set))
		{
			// Faster than a .count():
			return !ids.FirstOrDefault().Equals(default(ID));
		}

		if (typeof(ID) == typeof(uint))
		{
			foreach (var id in ids)
			{
				if (!set.Contains((id as uint?).Value))
				{
					return false;
				}
			}

			return true;
		}
		else if (typeof(ID) == typeof(ulong))
		{
			foreach (var id in ids)
			{
				if (!set.Contains((id as ulong?).Value))
				{
					return false;
				}
			}

			return true;
		}
		else
		{
			throw new NotImplementedException("Unknown ID type " + typeof(ID));
		}
	}

	/// <summary>
	/// True if *any* of the given set are present in the specified mapping. If the set is empty, this is always false.
	/// </summary>
	/// <typeparam name="ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <param name="ids"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public bool HasAny<ID>(string mappingName, IEnumerable<ID> ids)
		where ID : struct, IEquatable<ID>, IConvertible, IComparable<ID>
	{
		if (_values == null)
		{
			return false;
		}

		mappingName = mappingName.ToLower();

		if (!_values.TryGetValue(mappingName, out List<ulong> set))
		{
			return false;
		}

		if (typeof(ID) == typeof(uint))
		{
			foreach (var id in ids)
			{
				if (set.Contains((id as uint?).Value))
				{
					return true;
				}
			}

			return false;
		}
		else if (typeof(ID) == typeof(ulong))
		{
			foreach (var id in ids)
			{
				if (set.Contains((id as ulong?).Value))
				{
					return true;
				}
			}

			return false;
		}
		else
		{
			throw new NotImplementedException("Unknown ID type " + typeof(ID));
		}
	}

	/// <summary>
	/// Ensures the given content item's ID is removed from the specified map.
	/// </summary>
	/// <typeparam name="ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <param name="item"></param>
	public void Remove<ID>(string mappingName, Content<ID> item)
		where ID : struct, IEquatable<ID>, IConvertible, IComparable<ID>
	{
		if (item == null)
		{
			return;
		}

		Remove<ID>(mappingName, item.Id);
	}
	
	/// <summary>
	/// Removes an empty mapping 
	/// </summary>
	/// <param name="mappingName"></param>
	public void Remove(string mappingName)
	{
		if (_values == null)
		{
			return;
		}
		_values.Remove(mappingName.ToLower());
		if(_values.Count == 0){
			_values = null;
		}
	}



	/// <summary>
	/// Ensures the given content ID is removed from the specified map.
	/// </summary>
	/// <typeparam name="ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <param name="id"></param>
	/// <exception cref="NotImplementedException"></exception>
	public void Remove<ID>(string mappingName, ID id)
		where ID : struct, IEquatable<ID>, IConvertible, IComparable<ID>
	{
		if (typeof(ID) == typeof(uint))
		{
			var uid = id as uint?;
			Remove(mappingName, (ulong)uid.Value);
		}
		else if (typeof(ID) == typeof(ulong))
		{
			var uid = id as ulong?;
			Remove(mappingName, (ulong)uid.Value);
		}
		else
		{
			throw new NotImplementedException("Unknown ID type " + typeof(ID));
		}
	}

	/// <summary>
	/// Ensures the given ID is removed from the specified mapping.
	/// </summary>
	/// <param name="mappingName"></param>
	/// <param name="id"></param>
	public void Remove(string mappingName, ulong id)
	{
		if (_values == null)
		{
			return;
		}

		mappingName = mappingName.ToLower();

		if (!_values.TryGetValue(mappingName, out List<ulong> set))
		{
			return;
		}

		set.Remove(id);
	}

	/// <summary>
	/// Ensures the given set of item IDs are added to the specified map.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <param name="items"></param>
	public void AddAll<T, ID>(string mappingName, IEnumerable<T> items)
		where T : Content<ID>
		where ID : struct, IEquatable<ID>, IConvertible, IComparable<ID>
	{
		if (items == null)
		{
			return;
		}

		foreach (var item in items)
		{
			Add(mappingName, item);
		}
	}
	
	/// <summary>
	/// Ensures the given set of item IDs are added to the specified map.
	/// </summary>
	/// <typeparam name="ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <param name="items"></param>
	public void AddAll<ID>(string mappingName, IEnumerable<ID> items)
		where ID : struct, IEquatable<ID>, IConvertible, IComparable<ID>
	{
		if (items == null)
		{
			return;
		}

		foreach (var item in items)
		{
			if (typeof(ID) == typeof(uint))
			{
				var uid = item as uint?;
				Add(mappingName, (ulong)uid.Value);
			}
			else if (typeof(ID) == typeof(ulong))
			{
				var uid = item as ulong?;
				Add(mappingName, (ulong)uid.Value);
			}
		}
	}

	/// <summary>
	/// Gets the first value in the given named mapping.
	/// </summary>
	/// <param name="mappingName"></param>
	/// <returns></returns>
	public ulong GetFirst(string mappingName)
	{
		var mappingSet = Get(mappingName);

		if (mappingSet == null || mappingSet.Count == 0)
		{
			return 0;
		}

		return mappingSet[0];
	}

	/// <summary>
	/// Ensures the given set of item IDs are added to the specified map.
	/// </summary>
	/// <param name="mappingName"></param>
	/// <param name="items"></param>
	public void AddAll(string mappingName, IEnumerable<ulong> items)
	{
		if (items == null)
		{
			return;
		}

		foreach (var item in items)
		{
			Add(mappingName, item);
		}
	}

	/// <summary>
	/// Ensures the given content item's ID is added to the specified map.
	/// </summary>
	/// <typeparam name="ID"></typeparam>
	/// <param name="mappingName"></param>
	/// <param name="item"></param>
	public void Add<ID>(string mappingName, Content<ID> item)
		where ID : struct, IEquatable<ID>, IConvertible, IComparable<ID>
	{
		if (item == null)
		{
			return;
		}

		var id = item.GetId();

		if (typeof(ID) == typeof(uint))
		{
			var uid = id as uint?;
			Add(mappingName, uid.Value);
		}
		else if (typeof(ID) == typeof(ulong))
		{
			var uid = id as ulong?;
			Add(mappingName, uid.Value);
		}
		else
		{
			throw new NotImplementedException("Unknown ID type " + typeof(ID));
		}
	}

	/// <summary>
	/// Ensures the given ID is present in the specified mapping.
	/// </summary>
	/// <param name="mappingName"></param>
	/// <param name="id"></param>
	public void Add(string mappingName, ulong id)
	{
		if (_values == null)
		{
			_values = new();
		}

		mappingName = mappingName.ToLower();

		if (!_values.TryGetValue(mappingName, out List<ulong> set))
		{
			set = new List<ulong>() { id };
			_values[mappingName] = set;
			return;
		}

		// Is it already in there?
		for (var i = 0; i < set.Count; i++)
		{
			if (set[i] == id)
			{
				return;
			}
		}

		set.Add(id);
	}

	/// <summary>
	/// Ensures the given ID is present as the first item in the specified mapping.
	/// </summary>
	/// <param name="mappingName"></param>
	/// <param name="id"></param>
	public void Insert(string mappingName, ulong id)
	{
		if (_values == null)
		{
			_values = new();
		}

		mappingName = mappingName.ToLower();

		if (!_values.TryGetValue(mappingName, out List<ulong> set))
		{
			set = new List<ulong>() { id };
			_values[mappingName] = set;
			return;
		}

		// Is it already in there?
		for (var i = 0; i < set.Count; i++)
		{
			if (i==0 && set[i] == id)
			{
				return;
			}

			if (set[i] == id)
			{
				set.Remove(id);
				break;
			}
		}

		set.Insert(0,id);
	}


	/// <summary>
	/// Makes a deep clone of this mapping data.
	/// </summary>
	/// <returns></returns>
	public MappingData Clone()
	{
		var vals = _values;

		if (vals == null)
		{
			// That was easy
			return new MappingData();
		}

		var newVals = new Dictionary<string, List<ulong>>();

		foreach (var kvp in vals)
		{
			newVals[kvp.Key] = new List<ulong>(kvp.Value);
		}

		return new MappingData() { _values = newVals };
	}

	/// <summary>
	/// Set a specific mapping value.
	/// </summary>
	/// <param name="mappingName"></param>
	/// <param name="values"></param>
	public void Set(string mappingName, List<ulong> values)
	{
		if (values == null || values.Count == 0)
		{
			Remove(mappingName);
			return;
		}
		
		if (_values == null)
		{
			_values = new();
		}


		_values[mappingName.ToLower()] = values;
	}

	/// <summary>
	/// Gets the value for the given mapping name, copying the set such that it can be safely manipulated.
	/// Do this if the type you are getting the raw mapping set from is cached.
	/// </summary>
	/// <param name="mappingName"></param>
	/// <returns></returns>
	public List<ulong> GetCopy(string mappingName)
	{
		if (_values == null)
		{
			return null;
		}

		if (!_values.TryGetValue(mappingName.ToLower(), out List<ulong> result))
		{
			return null;
		}

		if (result == null)
		{
			return null;
		}

		return new List<ulong>(result);
	}

	/// <summary>
	/// Gets the value for the given mapping name.
	/// </summary>
	/// <param name="mappingName"></param>
	/// <returns></returns>
	public List<ulong> Get(string mappingName)
	{
		if (_values == null)
		{
			return null;
		}

		_values.TryGetValue(mappingName.ToLower(), out List<ulong> result);
		return result;
	}

	/// <summary>
	/// Iterate through all the values in this set.
	/// </summary>
	public IReadOnlyDictionary<string, List<ulong>> Values => _values;

	/// <summary>
	/// Creates a new MappingData instance.
	/// </summary>
	public MappingData()
	{
	}

	/// <summary>
	/// Collect a given mapping (if present) in to the given collector, then writes the 
	/// IDs as a JSON array such as [1,2,3,..] to the given writer.
	/// </summary>
	/// <param name="set"></param>
	/// <param name="lcMappingName"></param>
	/// <param name="writer"></param>
	public void WriteAndCollect(LongIDCollector set, string lcMappingName, Writer writer)
	{
		if (_values == null)
		{
			writer.WriteASCII("[]");
			return;
		}

		if (!_values.TryGetValue(lcMappingName, out List<ulong> result))
		{
			writer.WriteASCII("[]");
			return;
		}

		writer.Write((byte)'[');
		var first = true;

		foreach(var val in result)
		{
			if (first)
			{
				first = false;
			}
			else
			{
				writer.Write((byte)',');
			}

			writer.WriteS(val);
			set.Add(val);
		}

		writer.Write((byte)']');
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
				var val = kvp.Value;

				if (val == null || val.Count == 0)
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
				writer.WriteASCII("\":[");

				for (var i = 0; i < val.Count; i++)
				{
					if (i != 0)
					{
						writer.Write((byte)',');
					}

					writer.WriteS(val[i]);
				}

				writer.Write((byte)']');
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
				var val = kvp.Value;

				if (val == null || val.Count == 0)
				{
					continue;
				}

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
				sb.Append("\":[");

				for (var i = 0; i < val.Count; i++)
				{
					if (i != 0)
					{
						sb.Append(',');
					}

					sb.Append(val[i]);
				}

				sb.Append(']');
			}
		}

		sb.Append('}');
		return sb.ToString();
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
	/// True if this mapping data is empty.
	/// </summary>
	public bool IsEmpty => _values == null || _values.Count == 0;
	
	/// <summary>
	/// The number of entries in the set.
	/// </summary>
	public int Count => _values == null ? 0 : _values.Count;

	/// <summary>
	/// Compares the two given MappingData structs. Used by Diff.
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static bool Equals(MappingData a, MappingData b)
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
			if (!b._values.TryGetValue(kvp.Key, out List<ulong> bValue))
				return false;

			var aValue = kvp.Value;

			// Fast path: same object reference
			if (aValue == bValue)
			{
				continue;
			}

			if (aValue == null || bValue == null)
			{
				return false;
			}

			if (aValue.Count != bValue.Count)
			{
				return false;
			}

			// Compare elements in order
			for (int j = 0; j < aValue.Count; j++)
			{
				if (aValue[j] != bValue[j])
				{
					return false;
				}
			}
		}

		return true;
	}
	
	/// <summary>
	/// Similar to equals, compares if 2 mappings are different.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="originalMappings"></param>
	/// <returns></returns>
	public bool Changed(string key, MappingData originalMappings)
	{
		// if both are null, nothing has changed
		if (_values == null && originalMappings._values == null)
		{
			return false;
		}

		List<ulong> current = null;
		List<ulong> original = null;
		
		// It's in there and not null
		var existsInCurrent = _values != null && 
			_values.TryGetValue(key, out current) && 
			current != null;

		// It's in the other one and not null
		var existsInOriginal = originalMappings._values != null && 
				originalMappings._values.TryGetValue(key, out original) && 
				original != null;
	
		if (existsInOriginal != existsInCurrent)
		{
			// In one and not the other
			return true;
		}

		if (!existsInOriginal)
		{
			// It's in neither so can't have changed.
			return false;
		}

		// It's in both and neither is null.

		// they've changed if diff sizes.
		if (current.Count != original.Count)
		{
			return true;
		}

		for (var i = 0; i < current.Count; i++)
		{
			// compare items in order.
			if (current[i] != original[i])
			{
				return true;
			}
		}

		return false;
	}

}