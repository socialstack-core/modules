using System.Collections.Generic;
using Api.Contexts;
using System.Threading.Tasks;

namespace Api.Permissions
{

	/// <summary>
	/// A filter method which is active if 2 inputs are active.
	/// </summary>
	public partial class FilterAnd : FilterTwoInput
	{
		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override async Task<bool> IsGranted(Capability cap, Context token, object[] extraArgs)
		{
			var a = await Input0.IsGranted(cap, token, extraArgs);
			var b = await Input1.IsGranted(cap, token, extraArgs);
			return a && b;
		}
		
		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterAnd()
			{
				Input0 = Input0.Copy(),
				Input1 = Input1.Copy()
			};
		}
		
		/// <summary>
		/// True if this filter node is active on the given object.
		/// </summary>
		public override bool Matches(List<ResolvedValue> values, object obj){
			return Input0.Matches(values, obj) && Input1.Matches(values, obj);
		}
		
	}

	public partial class Filter
	{
		/// <summary>
		/// Usage: .Thing().And().OtherThing() - this will be true if both are true.
		/// </summary>
		/// <returns></returns>
		public Filter And()
		{
			return Add(new FilterAnd());
		}
	}

}