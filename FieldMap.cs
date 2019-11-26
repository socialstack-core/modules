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
	/// These are stored in the Query objects and essentially map 
	/// </summary>
	public class FieldMap
	{
		/// <summary>
		/// The field map for the given type. It should inherit DatabaseRow.
		/// </summary>
		public FieldMap(Type type)
		{
			// For now we just use *all* fields:
			var fields = type.GetFields();
			
			var fieldSet = new List<Field>();
			
			for(var i=0;i<fields.Length;i++){
				var field = fields[i];
				
				// (filter here if needed).
				
				fieldSet.Add(new Field(){
					OwningType = type,
					Type = field.FieldType,
					Name = field.Name,
					TargetField = field,
					FullName = "`" + type.Name + "`.`" + field.Name + "`"
				});
			}
			
			// Set:
			Fields = fieldSet;
		}
		
		/// <summary>
		/// All the fields in this map.
		/// </summary>
		public List<Field> Fields;

		/// <summary>
		/// Remove all fields except the named ones.
		/// </summary>
		/// <param name="fieldNames"></param>
		public void RemoveAllBut(string[] fieldNames)
		{
			Fields = Fields.Where(fld => fieldNames.Contains(fld.Name)).ToList();
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
