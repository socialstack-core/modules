namespace Api.Permissions
{

	/// <summary>
	/// A filter node which holds a group of others. Used for or/ and operations.
	/// </summary>
	public partial class FilterTwoInput : FilterNode
	{
		/// <summary>
		/// The first input.
		/// </summary>
		public FilterNode Input0 = null;
		/// <summary>
		/// The second input.
		/// </summary>
		public FilterNode Input1 = null;

		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterTwoInput()
			{
				Input0 = Input0.Copy(),
				Input1 = Input1.Copy()
			};
		}

		/// <summary>
		/// Makes sure this grant node is fully constructed.
		/// </summary>
		public override FilterNode Construct(Filter filter)
		{
			Input0 = filter.GetTopOfStack();
			Input1 = filter.PopConstructed();
			return this;
		}
	}
	
}