using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Api.Contexts;
using System.Collections.Generic;


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
		public override ValueTask<bool> IsGranted(Capability capability, Context token, object firstArg)
		{
			// For each value, perform the same checks as a regular single field matching.
			foreach(var value in Values)
			{
				// Firstly is it a direct match?
				if (firstArg == null)
				{
					if (value == null)
					{
						return new ValueTask<bool>(true);
					}

					continue;
				}

				if (firstArg.Equals(value))
				{
					return new ValueTask<bool>(true);
				}

				// Nope - try matching it via reading the field next.
				if (value == null)
				{
					continue;
				}

				if (firstArg.GetType() != Type)
				{
					return new ValueTask<bool>(false);
				}

				if (value.Equals(FieldInfo.GetValue(firstArg)))
				{
					return new ValueTask<bool>(true);
				}
			}

			// No hits
			return new ValueTask<bool>(false);
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
			
			// Matches any value in the set?
			foreach(var compareWith in Values)
			{
				if(val == null)
				{
					if(compareWith == null)
					{
						return true;
					}
					
					continue;
				}
				else if(compareWith == null)
				{
					continue;
				}

				if (compareWith is long)
				{
					// Long is often outputted by JSON deserializer.
					// long.Equals(int) != int.Equals(long).
					var a = (long)compareWith;
					var b = Convert.ToInt64(val);

					if (a == b)
					{
						return true;
					}
				}else if(val.Equals(compareWith))
				{
					return true;
				}
			}
			
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