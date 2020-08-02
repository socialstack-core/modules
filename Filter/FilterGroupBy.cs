using Api.Contexts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Permissions
{

	/// <summary>
	/// A filter node which defines a group by.
	/// </summary>
	public partial class FilterGroupBy : FilterNode
	{
		/// <summary>
		/// The type the field belongs to.
		/// </summary>
		public Type Type;
		
		/// <summary>
		/// The field.
		/// </summary>
		public string Field;
		
		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override Task<bool> IsGranted(Capability cap, Context token, object[] extraArgs)
		{
			// Always granted.
			return Task.FromResult(true);
		}
		
		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterGroupBy()
			{
				Type = Type,
				Field = Field
			};
		}
	}

	public partial class Filter
	{
		/// <summary>
		/// Performs a join with some other table. The ON sequence is completely yours to build via using .Equals() etc in the returned filter.
		/// </summary>
		public Filter GroupBy(string field)
		{
			if (Groupings == null)
			{
				Groupings = new List<FilterGroupBy>();
			}
			
			Groupings.Add(new FilterGroupBy() {
				Field = field,
				Type = DefaultType
			});

			return this;
		}
		
	}

}