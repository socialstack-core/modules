using Api.Contexts;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace Api.Permissions
{

	/// <summary>
	/// A grant chain method which just inverts the first child added to it.
	/// </summary>
	public partial class FilterNot : FilterNode
	{
		/// <summary>
		/// The child that will be inverted.
		/// </summary>
		public FilterNode Input0;

		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override async Task<bool> IsGranted(Capability capability, Context token, object[] extraObjectsToCheck)
		{
			var a = await Input0.IsGranted(capability, token, extraObjectsToCheck);
			return !a;
		}

		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterNot()
			{
				Input0 = Input0.Copy()
			};
		}

		/// <summary>
		/// Makes sure this grant node is fully constructed.
		/// </summary>
		public override FilterNode Construct(Filter filter)
		{
			Input0 = filter.PopConstructed();
			return this;
		}
		
		/// <summary>
		/// True if this filter node is active on the given object.
		/// </summary>
		public override bool Matches(List<ResolvedValue> values, object obj){
			return !Input0.Matches(values, obj);
		}
		
	}

	public partial class Filter
	{

		/// <summary>
		/// Adds a NOT which will invert the result following call.
		/// </summary>
		/// <returns></returns>
		public Filter Not()
		{
			return Add(new FilterNot());
		}

	}

}