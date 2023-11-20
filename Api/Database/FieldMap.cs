using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace Api.Database
{
	/// <summary>
	/// Retains a fast lookup for fields of particular types.
	/// These are stored in the Query objects and essentially map name to the raw field.
	/// </summary>
	public class FieldMap
	{
		/// <summary>
		/// The type that this is a map for.
		/// </summary>
		public Type Type;

		/// <summary>
		/// The field map for the given type. It should inherit DatabaseRow.
		/// </summary>
		public FieldMap(Type type, string typeName)
		{
			Type = type;

			if (typeName == null)
			{
				typeName = type.Name;
			}

			var fields = type.GetFields();
			
			var fieldSet = new List<Field>();
			Lookup = new Dictionary<string, Field>();

			for (var i=0;i<fields.Length;i++){
				var field = fields[i];

				// (filter here if needed).
				var dbField = field.GetCustomAttribute<DatabaseFieldAttribute>();

				if (dbField != null && dbField.Ignore) {
					// Ignore this field.
					continue;
				}

				var fld = new Field(type, field, typeName);
				fieldSet.Add(fld);
				Lookup[fld.Name] = fld;
			}
			
			// Set:
			Fields = fieldSet;
		}
		
		/// <summary>
		/// All the fields in this map.
		/// </summary>
		public List<Field> Fields;
		/// <summary>
		/// Name lookup.
		/// </summary>
		private Dictionary<string, Field> Lookup;

		/// <summary>
		/// Remove all fields except the named ones.
		/// </summary>
		/// <param name="fieldNames"></param>
		public void RemoveAllBut(string[] fieldNames)
		{
			Fields = Fields.Where(fld => fieldNames.Contains(fld.Name)).ToList();
			Lookup.Clear();
			foreach (var field in Fields)
			{
				Lookup[field.Name] = field;
			}
		}

		/// <summary>
		/// Finds a field with the given name. Null if not found.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Field Find(string name)
		{
			Lookup.TryGetValue(name, out Field f);
			return f;
		}

		/// <summary>
		/// Adds a field to this map.
		/// </summary>
		/// <param name="field"></param>
		public void Add(Field field)
		{
			Fields.Add(field);
			Lookup[field.Name] = field;
		}

		/// <summary>
		/// Remove a field from the map.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Field Remove(string name)
		{
			for (var i = 0; i < Fields.Count; i++)
			{
				var fld = Fields[i];

				if (fld.Name == name)
				{
					// Pop it now:
					Fields.RemoveAt(i);
					return fld;
				}
			}

			return null;
		}

		/// <summary>
		/// The number of fields in this map.
		/// </summary>
		public int Count
		{
			get
			{
				return Fields.Count;
			}
		}

		/// <summary>
		/// Gets a field by its index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Field this[int index]
		{
			get
			{
				return Fields[index];
			}
		}
	}
}
