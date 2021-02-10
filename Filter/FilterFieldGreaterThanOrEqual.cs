using System;
using System.Reflection;
using System.Threading.Tasks;
using Api.Contexts;
using System.Collections.Generic;


namespace Api.Permissions
{

	/// <summary>
	/// Checks if a field in an arg is greater than or equal to a given value.
	/// </summary>
	public partial class FilterFieldGreaterThanOrEqual: FilterNode
	{
		/// <summary>
		/// The type that we're looking for
		/// </summary>
		public Type Type;
		/// <summary>
		/// The field name which should be present in the given type.
		/// </summary>
		public string Field;
		/// <summary>
		/// The index of the argument we'll compare with.
		/// </summary>
		public int ArgIndex;
		/// <summary>
		/// The value to match. Number or datetime.
		/// </summary>
		public object Value;
		/// <summary>
		/// Matches arg instead of against the given constant Value.
		/// </summary>
		public bool AlwaysArgMatch;
		/// <summary>
		/// The fieldInfo for the field on the type.
		/// </summary>
		protected FieldInfo FieldInfo;
		/// <summary>
		/// True if this field is localised.
		/// </summary>
		public bool Localise;

		/// <summary>
		/// Create a new node.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="field"></param>
		public FilterFieldGreaterThanOrEqual(Type type, string field)
		{
			Type = type;
			Field = field;

			// Get the field info:
			FieldInfo = type.GetField(field, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

			if (FieldInfo == null)
			{
				throw new Exception(field + " doesn't exist on type '" + type.Name + "'");
			}

			Localise = FieldInfo.GetCustomAttribute<Api.Translate.LocalizedAttribute>() != null;
		}

		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override Task<bool> IsGranted(Capability capability, Context token, object firstArg)
		{
			// Firstly is it a direct match?
			if (firstArg == null || firstArg.GetType() != Type)
			{
				return Task.FromResult(false);
			}

			if (Value is long)
			{
				// Try reading it:
				var fieldValue = FieldInfo.GetValue(firstArg) as long?;

				return Task.FromResult(fieldValue.HasValue && fieldValue >= (long)Value);
			}

			if (Value is DateTime)
			{
				// Try reading it:
				var fieldValue = FieldInfo.GetValue(firstArg) as DateTime?;

				return Task.FromResult(fieldValue.HasValue && fieldValue >= (DateTime)Value);
			}

			return Task.FromResult(false);
		}

		/// <summary>
		/// True if this filter node is active on the given object.
		/// </summary>
		public override bool Matches(List<ResolvedValue> values, object obj)
		{

			if (obj == null)
			{
				return false;
			}

			// Read the value:
			var val = FieldInfo.GetValue(obj);
			
			if (AlwaysArgMatch)
			{
				// No args in this mode
				return false;
			}
			
			object compareWith = Value;
			
			if (val == null || compareWith == null)
			{
				// Matches DB behaviour too. I.e. NULL<10, NULL>10, 10>NULL, 10<NULL are all false.
				return false;
			}

			if (val is DateTime)
			{
				return (DateTime)val >= (DateTime)compareWith;
			}

			var us = Convert.ToInt64(val);
			var them = Convert.ToInt64(compareWith);
			return us >= them;
		}

		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterFieldGreaterThanOrEqual(Type, Field)
			{
				Value = Value,
				ArgIndex = ArgIndex,
				AlwaysArgMatch = AlwaysArgMatch
			};
		}
	}
	
	public partial class Filter
	{

		/// <summary>
		/// Adds a filter node which checks if the value at the given argIndex is greater than or equal the given value.
		/// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fieldName"></param>
		/// <param name="argIndex"></param>
		/// <returns></returns>
		public Filter GreaterThanOrEqualArg(System.Type type, string fieldName, int argIndex = 0)
		{
			return Add(new FilterFieldGreaterThanOrEqual(type, fieldName)
			{
				AlwaysArgMatch = true,
				ArgIndex = argIndex
			});
		}

		/// <summary>
		/// Adds a filter node which checks if the value at the given argIndex is greater than or equal the given value.
		/// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fieldName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public Filter GreaterThanOrEqual(System.Type type, string fieldName, object value)
		{
			return Add(new FilterFieldGreaterThanOrEqual(type, fieldName)
			{
				Value = value
			});
		}
		
	}

}