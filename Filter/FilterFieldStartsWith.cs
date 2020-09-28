using System;
using System.Reflection;
using System.Threading.Tasks;
using Api.Contexts;
using System.Collections.Generic;


namespace Api.Permissions
{

	/// <summary>
	/// Checks if a field in an arg starts with a given value.
	/// </summary>
	public partial class FilterFieldStartsWith: FilterNode
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
		public string Value;
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
		public FilterFieldStartsWith(Type type, string field)
		{
			Type = type;
			Field = field;

			// Get the field info:
			FieldInfo = type.GetField(field);

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
			
			// Try matching it via reading the field next.
			if (Value == null)
			{
				return Task.FromResult(false);
			}

			if (firstArg.GetType() != Type)
			{
				return Task.FromResult(false);
			}

			return Task.FromResult(Value.Equals(FieldInfo.GetValue(firstArg) as string));
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
			
			if(val == null || compareWith == null)
			{
				return false;
			}
			
			// Does val start with compareWith?
			return val.ToString().StartsWith(compareWith.ToString(), StringComparison.OrdinalIgnoreCase);
		}
		
		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterFieldStartsWith(Type, Field)
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
		/// Adds a filter node which checks if the value at the given argIndex starts with the given value.
		/// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fieldName"></param>
		/// <param name="argIndex"></param>
		/// <returns></returns>
		public Filter StartsWithArg(System.Type type, string fieldName, int argIndex = 0)
		{
			return Add(new FilterFieldStartsWith(type, fieldName)
			{
				AlwaysArgMatch = true,
				ArgIndex = argIndex
			});
		}

		/// <summary>
		/// Adds a filter node which checks if the value at the given argIndex starts with the given value.
		/// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fieldName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public Filter StartsWith(System.Type type, string fieldName, string value)
		{
			return Add(new FilterFieldStartsWith(type, fieldName)
			{
				Value = value
			});
		}
		
	}

}