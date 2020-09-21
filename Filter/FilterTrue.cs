using Api.Contexts;
using System.Threading.Tasks;
using System.Collections.Generic;


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
		public override Task<bool> IsGranted(Capability cap, Context token, object firstArg)
		{
			// You shall pass!
			return Task.FromResult(true);
		}

		/// <summary>
		/// True if this filter node is active on the given object.
		/// </summary>
		public override bool Matches(List<ResolvedValue> values, object obj){
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