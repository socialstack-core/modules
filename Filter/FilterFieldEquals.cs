using System;
using System.Reflection;
using Api.Contexts;


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
		public override bool IsGranted(Capability capability, Context token, object[] extraObjectsToCheck)
		{
			// Get first extra arg
			if (extraObjectsToCheck == null || extraObjectsToCheck.Length < ArgIndex)
			{
				// Arg not provided. Hard fail scenario.
				return EqualsFail(capability);
			}

			var firstArg = extraObjectsToCheck[ArgIndex];

			// Firstly is it a direct match?
			if (firstArg == null)
			{
				return Value == null;
			}

			if (firstArg.Equals(Value))
			{
				return true;
			}

			// Nope - try matching it via reading the field next.
			if (Value == null)
			{
				return false;
			}

			if (firstArg.GetType() != Type)
			{
				return false;
			}

			return Value.Equals(FieldInfo.GetValue(firstArg));
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

		/// <summary>
		/// Used by equals when the basic setup checks fail.
		/// </summary>
		/// <param name="capability"></param>
		protected bool EqualsFail(Capability capability)
		{
			// Separating this helps make the grant methods potentially go inline.
			if (capability == null)
			{
				throw new Exception("Capability wasn't found. This probably means you used a capability name which doesn't exist.");
			}

			throw new Exception(
				"Use of '" + capability.Name +
				"' capability requires giving it a " + Type.Name + " as argument " + ArgIndex + ". " +
				"Capability.IsGranted(request, \"cap_name\", .., *" + Type.Name + "*);"
			);
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
		public Filter Equals(System.Type type, string fieldName, object value, int argIndex = 0)
		{
			return Add(new FilterFieldEquals(type, fieldName)
			{
				Value = value,
				ArgIndex = argIndex
			});
		}
		
	}

}