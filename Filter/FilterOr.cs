using Api.Contexts;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace Api.Permissions
{

	/// <summary>
	/// A filter method which is active if either of 2 inputs are active.
	/// </summary>
	public partial class FilterOr : FilterTwoInput
	{
		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override async ValueTask<bool> IsGranted(Capability cap, Context token, object firstArg)
		{
			var a = await Input0.IsGranted(cap, token, firstArg);
			var b = await Input1.IsGranted(cap, token, firstArg);
			return a || b;
		}

		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterOr()
			{
				Input0 = Input0.Copy(),
				Input1 = Input1.Copy()
			};
		}
		
		/// <summary>
		/// True if this filter node is active on the given object.
		/// </summary>
		public override bool Matches(List<ResolvedValue> values, object obj){
			return Input0.Matches(values, obj) || Input1.Matches(values, obj);
		}
		
	}


	public partial class Filter
	{
		/// <summary>
		/// Usage: .Thing().Or().OtherThing() - this will be true if either are true.
		/// </summary>
		/// <returns></returns>
		public Filter Or()
		{
			return Add(new FilterOr());
		}
	}

}