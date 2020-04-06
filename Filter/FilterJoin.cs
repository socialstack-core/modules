using Api.Contexts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Permissions
{

	/// <summary>
	/// A filter method which is active if 2 inputs are active.
	/// </summary>
	public partial class FilterJoin : FilterNode
	{
		/// <summary>
		/// The type we're joining.
		/// </summary>
		public Type TargetType;

		/// <summary>
		/// The filter for declaring the actual fields we're joining on.
		/// </summary>
		public Filter On;


		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override Task<bool> IsGranted(Capability cap, Context token, object[] extraArgs)
		{
			// TODO: True only if the join succeeds. Joins won't be used in the perm system yet though.
			return Task.FromResult(true);
		}
		
		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterJoin()
			{
				On = On.Copy(),
				TargetType = TargetType
			};
		}
	}

	public partial class Filter
	{
		/// <summary>
		/// Performs a join with some other table. The ON sequence is completely yours to build via using .Equals() etc in the returned filter.
		/// </summary>
		public Filter<T> Join<T>()
		{
			var joinOn = new Filter<T>()
			{
				Role = Role
			};

			if (Joins == null)
			{
				Joins = new List<FilterJoin>();
			}

			Joins.Add(new FilterJoin() {
				On = joinOn,
				TargetType = typeof(T)
			});

			return joinOn;
		}

		/// <summary>
		/// Performs a join with some other table with a fairly common ON fieldA=fieldB.
		/// </summary>
		/// <param name="fieldA">This field is assumed to exist in the default type of the filter.</param>
		/// <param name="fieldB">This field is assumed to exist in the target type.</param>
		public Filter<T> Join<T>(string fieldA, string fieldB)
		{
			var filt = Join<T>();

			filt.Equals(DefaultType, fieldA, typeof(T), fieldB);

			return filt;
		}

	}

}