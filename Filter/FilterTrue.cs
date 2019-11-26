using Api.Contexts;


namespace Api.Permissions
{

	/// <summary>
	/// A filter node which just always returns true.
	/// </summary>
	public partial class FilterTrue : FilterNode
	{
		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override bool IsGranted(Capability cap, Context token, object[] extraArgs)
		{
			// You shall pass!
			return true;
		}

		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterTrue();
		}
	}

}