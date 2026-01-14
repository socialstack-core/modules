using Api.Startup;
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
	/// Parse the given JSON string and returns a boxed Localized for a specific field. 
	/// Note that the target method is a concrete type (Localized string etc).
	/// </summary>
	/// <param name="src"></param>
	/// <returns></returns>
	public delegate object LocalizeParseBoxed(string src);

	/// <summary>
	/// Stores details about a DatabaseRow child type field.
	/// </summary>
	public class Field
	{
		/// <summary>
		/// The type that this field is part of.
		/// This isn't the same as TargetField.DeclaringType (specifically because Id is declared elsewhere, but is "owned" by the row type).
		/// </summary>
		public readonly Type OwningType;
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
		/// The full name of this field. OwningTypeName.`Name`
		/// </summary>
		public string FullName;
		/// <summary>
		/// The owning type name to use.
		/// </summary>
		public string OwningTypeName;
		/// <summary>
		/// Does this field have a price attribute? If so, do not use default locale value if null
		/// </summary>
		public bool IsPrice;
		/// <summary>
		/// Attributes on the field/ property (if any). Can be null.
		/// </summary>
		public List<Attribute> TargetFieldCustomAttributes;
		/// <summary>
		/// True if this field is Localized.
		/// </summary>
		public bool IsLocalized;

		/// <summary>
		/// Converts a JSON string to a Localized, boxed.
		/// </summary>
		public LocalizeParseBoxed ParseLocalized;

		/// <summary>
		/// Creates a new empty field
		/// </summary>
		public Field(Type owningType, string typeName) {
			OwningType = owningType;
			OwningTypeName = typeName;
		}

		/// <summary>
		/// Creates a new field for the given owning type and field info, with optional field name.
		/// </summary>
		public Field(Type type, FieldInfo field, string typeName) {

			OwningType = type;
			Type = field.FieldType;
			TargetField = field;
			Name = field.Name;
			OwningTypeName = typeName;
			TargetFieldCustomAttributes = ContentField.BuildAttributes(field.CustomAttributes);
			if (
				TargetFieldCustomAttributes != null 
				&& TargetFieldCustomAttributes.Count > 0 
				&& TargetFieldCustomAttributes.FirstOrDefault(tfca => tfca.GetType().Name == "PriceAttribute") != null)
            {
				IsPrice = true;
			}
			SetIsLocalized();
			SetFullName(null);
		}

		private bool? _isNullable = null;

		/// <summary>
		/// True if this fields value is a nullable type. Either it is a reference type, or Nullable.
		/// </summary>
		/// <returns></returns>
		public bool IsNullable()
		{
			if (_isNullable.HasValue)
			{
				return _isNullable.Value;
			}

			if (Nullable.GetUnderlyingType(Type) != null)
			{
				_isNullable = true;
			}
			else
			{
				// It's nullable if it is not a value type.
				_isNullable = !Type.IsValueType;
			}

			return _isNullable.Value;
		}

		private void SetIsLocalized()
		{
			if (TargetField == null)
			{
				return;
			}

			var ft = TargetField.FieldType;

			if (ft.IsGenericType)
			{
				var typeDef = ft.GetGenericTypeDefinition();

				if (typeDef == typeof(Localized<>))
				{
					ParseLocalized = ft.GetMethod("ParseBoxed", BindingFlags.Static | BindingFlags.Public).CreateDelegate<LocalizeParseBoxed>();

					IsLocalized = true;
				}
			}
		}

		/// <summary>
		/// Updates the FullName and LocalisedName fields.
		/// </summary>
		public void SetFullName(string extension = null)
		{
			FullName = "`" + OwningTypeName + ((extension == null) ? "" : "_" + extension) + "`.`" + Name;
			FullName += "`";
		}
		
		/// <summary>
		/// Creates a clone of this field.
		/// </summary>
		/// <returns></returns>
		public Field Clone()
		{
			return new Field(OwningType, OwningTypeName)
			{
				Type = Type,
				TargetField = TargetField,
				Name = Name,
				FullName = FullName,
				IsLocalized = IsLocalized
			};
		}

	}
	
}
