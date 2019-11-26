using Api.Contexts;


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
		public override bool IsGranted(Capability cap, Context token, object[] extraArgs)
		{
			return Input0.IsGranted(cap, token, extraArgs) || Input1.IsGranted(cap, token, extraArgs);
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