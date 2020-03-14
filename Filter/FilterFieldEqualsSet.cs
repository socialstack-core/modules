using System;
using System.Collections;
using System.Reflection;
using Api.Contexts;


namespace Api.Permissions
{

	/// <summary>
	/// Checks if a field in an arg matches a set of values.
	/// </summary>
	public partial class FilterFieldEqualsSet : FilterNode
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
		/// The values to match.
		/// </summary>
		public IEnumerable Values;
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
		public FilterFieldEqualsSet(Type type, string field)
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

			// For each value, perform the same checks as a regular single field matching.
			foreach(var value in Values)
			{
				// Firstly is it a direct match?
				if (firstArg == null)
				{
					if (value == null)
					{
						return true;
					}

					continue;
				}

				if (firstArg.Equals(value))
				{
					return true;
				}

				// Nope - try matching it via reading the field next.
				if (value == null)
				{
					continue;
				}

				if (firstArg.GetType() != Type)
				{
					return false;
				}

				if (value.Equals(FieldInfo.GetValue(firstArg)))
				{
					return true;
				}
			}

			// No hits
			return false;
		}

		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterFieldEqualsSet(Type, Field)
			{
				Values = Values
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
		/// Convenience function for granting a capability only if we're provided a number from the given set.
		/// Optionally give a mapping function which maps the provided args through to the number itself.
		/// </summary>
		/// <returns></returns>
		public Filter Id(System.Type type, params object[] ids)
		{
			return Add(new FilterFieldEqualsSet(type, "Id")
			{
				Values = ids
			});
		}

	}

}