using System;
using System.Reflection;
using System.Threading.Tasks;
using Api.Contexts;
using System.Collections.Generic;
using System.Collections;

namespace Api.Permissions
{

	/// <summary>
	/// Checks if a field in an arg matches a given value.
	/// </summary>
	public partial class FilterFieldEquals : FilterNode
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
		/// The value to match.
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
		/// Create a new equals node.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="field"></param>
		public FilterFieldEquals(Type type, string field)
		{
			Type = type;

			// Get the field info:
			if (field == "IsDraft")
			{
				// Special case
				Field = "RevisionIsDraft";
				FieldInfo = typeof(Users.RevisionRow).GetField("_IsDraft", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			else if (field == "RevisionOriginalContentId")
			{
				// Special case
				Field = "RevisionOriginalContentId";
				FieldInfo = typeof(Users.RevisionRow).GetField("_RevisionId", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			else
			{
				Field = field;
				FieldInfo = type.GetField(field);
			}

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
			if (firstArg == null)
			{
				return Task.FromResult(Value == null);
			}

			if (firstArg.Equals(Value))
			{
				return Task.FromResult(true);
			}

			// Nope - try matching it via reading the field next.
			if (Value == null)
			{
				return Task.FromResult(false);
			}

			if (firstArg.GetType() != Type)
			{
				return Task.FromResult(false);
			}

			return Task.FromResult(Value.Equals(FieldInfo.GetValue(firstArg)));
		}

		/// <summary>
		/// True if this filter node is active on the given object.
		/// </summary>
		public override bool Matches(List<ResolvedValue> values, object obj){
			
			if(obj == null)
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
			
			if(val == null)
			{
				return (compareWith == null);
			}
			else if(compareWith == null)
			{
				return false;
			}

			var typeA = val.GetType();
			var typeB = compareWith.GetType();

			if (typeA == typeB)
			{
				// Both the same type - equals can handle it:
				return val.Equals(compareWith);
			}

			// They're different types.
			// If one is a string, we do a ToString on the other:
			if (typeA == typeof(string))
			{
				return (string)val == compareWith.ToString();
			}
			
			if (typeB == typeof(string))
			{
				return val.ToString() == (string)compareWith;
			}

			// Number vs. bool or other numeric type
			// Treat all as a long:
			var isNumA = IsNumericishType(typeA);
			var isNumB = IsNumericishType(typeB);

			if (isNumA && isNumB)
			{
				return Convert.ToInt64(val) == Convert.ToInt64(compareWith);
			}

			// Unknown type combination
			return false;
		}

		private bool IsNumericishType(Type t)
		{
			switch (Type.GetTypeCode(t))
			{
				case TypeCode.Boolean:
				case TypeCode.Char:
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterFieldEquals(Type, Field)
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
		/// Adds a filter node which checks if the value at the given argIndex equals the given value.
		/// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fieldName"></param>
		/// <param name="argIndex"></param>
		/// <returns></returns>
		public Filter EqualsArg(System.Type type, string fieldName, int argIndex = 0)
		{
			return Add(new FilterFieldEquals(type, fieldName)
			{
				AlwaysArgMatch = true,
				ArgIndex = argIndex
			});
		}

		/// <summary>
		/// Adds a filter node which checks if the value at the given argIndex equals the given value.
		/// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fieldName"></param>
		/// <param name="value"></param>
		/// <param name="argIndex"></param>
		/// <returns></returns>
		public Filter Equals(System.Type type, string fieldName, object value)
		{
			return Add(new FilterFieldEquals(type, fieldName)
			{
				Value = value
			});
		}

		/// <summary>
		/// Convenience function for granting a capability only if we're provided an entry from the given set.
		/// </summary>
		/// <returns></returns>
		public Filter Equals(string fieldName, IEnumerable values)
		{
			return Add(new FilterFieldEqualsSet(DefaultType, fieldName)
			{
				Values = values
			});
		}

		/// <summary>
		/// Adds a filter node which checks if the value at the given argIndex equals the given value.
		/// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
		/// </summary>
		/// <param name="fieldName"></param>
		/// <param name="value"></param>
		/// <param name="argIndex"></param>
		/// <returns></returns>
		public Filter EqualsField(string fieldName, object value)
		{
			return Add(new FilterFieldEquals(DefaultType, fieldName)
			{
				Value = value
			});
		}

	}

}