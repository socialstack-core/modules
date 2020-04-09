using System;
using System.Reflection;
using System.Threading.Tasks;
using Api.Contexts;


namespace Api.Permissions
{

	/// <summary>
	/// Checks if a field in an arg is greater than a given value.
	/// </summary>
	public partial class FilterFieldGreaterThan: FilterNode
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
		/// Create a new node.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="field"></param>
		public FilterFieldGreaterThan(Type type, string field)
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
		public override Task<bool> IsGranted(Capability capability, Context token, object[] extraObjectsToCheck)
		{
			// Get first extra arg
			if (extraObjectsToCheck == null || extraObjectsToCheck.Length < ArgIndex)
			{
				// Arg not provided. Hard fail scenario.
				return Task.FromResult(Fail(capability));
			}

			var firstArg = extraObjectsToCheck[ArgIndex];

			// Firstly is it a direct match?
			if (firstArg == null)
			{
				return Task.FromResult(false);
			}

            if (Value is long)
            {
                // Might just be a direct number given to us:
                var firstArgAsNum = firstArg as long?;

                if (firstArgAsNum != null && firstArgAsNum > (long)Value)
                {
                    return Task.FromResult(true);
                }

                // Nope - try matching it via reading the field next.
                if (firstArg.GetType() != Type)
                {
                    return Task.FromResult(false);
                }

                // Try reading it:
                var fieldValue = FieldInfo.GetValue(firstArg) as long?;

                return Task.FromResult(fieldValue.HasValue && fieldValue > (long)Value);
            }

            if (Value is DateTime)
            {
                // Might just be a direct datetime given to us:
                var firstArgAsNum = firstArg as DateTime?;

                if (firstArgAsNum != null && firstArgAsNum > (DateTime)Value)
                {
                    return Task.FromResult(true);
                }

                // Nope - try matching it via reading the field next.
                if (firstArg.GetType() != Type)
                {
                    return Task.FromResult(false);
                }

                // Try reading it:
                var fieldValue = FieldInfo.GetValue(firstArg) as DateTime?;

                return Task.FromResult(fieldValue.HasValue && fieldValue > (DateTime)Value);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Copies this filter node.
        /// </summary>
        /// <returns>A deep copy of the node.</returns>
        public override FilterNode Copy()
		{
			return new FilterFieldGreaterThan(Type, Field)
			{
				Value = Value,
				ArgIndex = ArgIndex,
				AlwaysArgMatch = AlwaysArgMatch
			};
		}

		/// <summary>
		/// Used when the basic setup checks fail.
		/// </summary>
		/// <param name="capability"></param>
		protected bool Fail(Capability capability)
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
		/// Adds a filter node which checks if the value at the given argIndex is greater than the given value.
		/// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fieldName"></param>
		/// <param name="argIndex"></param>
		/// <returns></returns>
		public Filter GreaterThanArg(System.Type type, string fieldName, int argIndex = 0)
		{
			return Add(new FilterFieldGreaterThan(type, fieldName)
			{
				AlwaysArgMatch = true,
				ArgIndex = argIndex
			});
		}

		/// <summary>
		/// Adds a filter node which checks if the value at the given argIndex is greater than the given value.
		/// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fieldName"></param>
		/// <param name="value"></param>
		/// <param name="argIndex"></param>
		/// <returns></returns>
		public Filter GreaterThan(System.Type type, string fieldName, long value, int argIndex = 0)
		{
			return Add(new FilterFieldGreaterThan(type, fieldName)
			{
				Value = value,
				ArgIndex = argIndex
			});
		}
		
	}

}