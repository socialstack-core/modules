using System;
using System.Reflection;
using System.Threading.Tasks;
using Api.Contexts;


namespace Api.Permissions
{
	/// <summary>
	/// Checks if a field matches some other field. Typically used by Join statements only.
	/// </summary>
	public partial class FilterFieldEqualsField : FilterNode
	{
		/// <summary>
		/// The type that we're looking for
		/// </summary>
		public Type TypeA;
		/// <summary>
		/// The field name which should be present in the given type.
		/// </summary>
		public string FieldA;
		/// <summary>
		/// The fieldInfo for the field on the type.
		/// </summary>
		protected FieldInfo FieldInfoA;

		/// <summary>
		/// The 2nd type that we're looking for
		/// </summary>
		public Type TypeB;
		/// <summary>
		/// The 2nd field name which should be present in the given type.
		/// </summary>
		public string FieldB;
		/// <summary>
		/// The 2nd fieldInfo for the field on the type.
		/// </summary>
		protected FieldInfo FieldInfoB;
		/// <summary>
		/// True if this field is localised.
		/// </summary>
		public bool LocaliseA;
		/// <summary>
		/// True if this field is localised.
		/// </summary>
		public bool LocaliseB;

		/// <summary>
		/// Create a new field equals node.
		/// </summary>
		/// <param name="typeA"></param>
		/// <param name="fieldA"></param>
		/// <param name="typeB"></param>
		/// <param name="fieldB"></param>
		public FilterFieldEqualsField(Type typeA, string fieldA, Type typeB, string fieldB)
		{
			TypeA = typeA;
			FieldA = fieldA;
			TypeB = typeB;
			FieldB = fieldB;

			// Get the field info:
			FieldInfoA = typeA.GetField(fieldA);

			if (FieldInfoA != null)
			{
				LocaliseA = FieldInfoA.GetCustomAttribute<Api.Translate.LocalizedAttribute>() != null;
			}

			FieldInfoB = typeA.GetField(fieldB);

			if (FieldInfoB != null)
			{
				LocaliseB = FieldInfoB.GetCustomAttribute<Api.Translate.LocalizedAttribute>() != null;
			}
		}

		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override Task<bool> IsGranted(Capability capability, Context token, object extraArg)
		{
			throw new Exception("Field=Field doesn't support the permissions system at the moment. Got a use case? Do share!");
		}

		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterFieldEqualsField(TypeA, FieldA, TypeB, FieldB);
		}
		
	}

	public partial class Filter
	{
		/// <summary>
		/// Adds a filter node which checks if two fields are equal. 
		/// TypeA is often equal to typeB, but doesn't have to be (e.g. in the case of a Join).
		/// </summary>
		/// <param name="typeA"></param>
		/// <param name="fieldA"></param>
		/// <param name="typeB"></param>
		/// <param name="fieldB"></param>
		/// <returns></returns>
		public Filter Equals(System.Type typeA, string fieldA, System.Type typeB, string fieldB)
		{
			return Add(new FilterFieldEqualsField(typeA, fieldA, typeB, fieldB));
		}

	}

}