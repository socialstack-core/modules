using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace Api.AutoForms
{
	/// <summary>
	/// Handles JSON input for autoform supporting endpoints.
	/// </summary>
	public class AutoFormFieldMap
	{
		/// <summary>
		/// The already generated field maps.
		/// </summary>
		private static Dictionary<Type, AutoFormFieldMap> BuiltMaps = new Dictionary<Type, AutoFormFieldMap>();

		/// <summary>
		/// Gets or generates the field map for the given AutoForm type.
		/// </summary>
		/// <param name="type"></param>
		public static AutoFormFieldMap Get(Type type)
		{
			if (BuiltMaps.TryGetValue(type, out AutoFormFieldMap map))
			{
				// Already built it:
				return map;
			}

			// Build it now
			map = new AutoFormFieldMap();
			
			// Get the fields from the model:
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

			// Get the base target types (AutoForm<>) generic args so we know the thing we're targeting:
			Type[] typeArguments = type.BaseType.GetGenericArguments();
			var targetFields = typeArguments[0].GetFields(BindingFlags.Public | BindingFlags.Instance);

			// Build dictionary of fieldInfo for faster lookups:
			var fieldLookup = new Dictionary<string, FieldInfo>();

			for (var i = 0; i < targetFields.Length; i++)
			{
				var target = targetFields[i];
				fieldLookup[target.Name] = target;
			}

			var fieldPairs = new List<AutoFormFieldPair>();

			// For each field, do we have a match in the target type?
			for (var i = 0; i < fields.Length; i++)
			{
				var src = fields[i];

				if (!fieldLookup.TryGetValue(src.Name, out FieldInfo target))
				{
					continue;
				}
				
				// Types must match:
				if (src.FieldType != target.FieldType)
				{
					continue;
				}

				// If either is tagged with DontCopy then ignore this one:
				if (
					src.GetCustomAttribute<DontCopyAttribute>() != null || 
					target.GetCustomAttribute<DontCopyAttribute>() != null
				) {
					continue;
				}

				// Valid pair.
				fieldPairs.Add(new AutoFormFieldPair() {
					Source = src,
					Target = target,
					Name = src.Name
				});

			}
			
			map.FieldPairs = fieldPairs.ToArray();
			BuiltMaps[type] = map;
			return map;
		}

		/// <summary>
		/// All the field pairs in this map. This is never null.
		/// </summary>
		public AutoFormFieldPair[] FieldPairs;

	}

	/// <summary>
	/// A pairing of source/ target fields.
	/// </summary>
	public class AutoFormFieldPair
	{
		/// <summary>
		/// The field name.
		/// </summary>
		public string Name;
		/// <summary>
		/// The field in the input model.
		/// </summary>
		public FieldInfo Source;
		/// <summary>
		/// The field in the object being built up.
		/// </summary>
		public FieldInfo Target;
	}

}
