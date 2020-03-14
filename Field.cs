using Api.Translate;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace Api.Database
{
	/// <summary>
	/// Stores details about a DatabaseRow child type field.
	/// </summary>
	public class Field
	{
		/// <summary>
		/// The type that this field is part of.
		/// This isn't the same as TargetField.DeclaringType (specifically because Id is declared elsewhere, but is "owned" by the row type).
		/// </summary>
		public Type OwningType;
		/// <summary>
		/// The type of this fields value (int, string etc).
		/// </summary>
		public Type Type;
		/// <summary>
		/// The field where the value is set to/get from.
		/// </summary>
		public FieldInfo TargetField;
		/// <summary>
		/// The name of this field.
		/// </summary>
		public string Name;
		/// <summary>
		/// The full name of this field. OwningType.Name.`Name`
		/// </summary>
		public string FullName;
		/// <summary>
		/// The full name of this field, except it ends with an underscore. OwningType.Name.`Name_
		/// </summary>
		public string LocalisedName;


		/// <summary>
		/// Creates a new empty field
		/// </summary>
		public Field() { }

		/// <summary>
		/// Creates a new field for the given owning type and field info, with optional field name.
		/// </summary>
		public Field(Type type, FieldInfo field, string name = null) {

			OwningType = type;
			Type = field.FieldType;
			TargetField = field;
			Name = name == null ? field.Name : name;
			SetFullName(null);
		}

		/// <summary>
		/// Updates the FullName and LocalisedName fields.
		/// </summary>
		public void SetFullName(string extension = null)
		{
			FullName = "`" + OwningType.Name + ((extension == null) ? "" : "_" + extension) + "`.`" + Name;

			if (TargetField != null && TargetField.GetCustomAttribute<LocalizedAttribute>() != null)
			{
				LocalisedName = FullName + "_";
			}
			else
			{
				LocalisedName = null;
			}

			FullName += "`";
		}
		
		/// <summary>
		/// Creates a clone of this field.
		/// </summary>
		/// <returns></returns>
		public Field Clone()
		{
			return new Field()
			{
				OwningType = OwningType,
				Type = Type,
				TargetField = TargetField,
				Name = Name,
				FullName = FullName,
				LocalisedName = LocalisedName
			};
		}

	}
	
}
