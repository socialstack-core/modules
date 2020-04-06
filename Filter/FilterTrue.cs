using Api.Contexts;
using System.Threading.Tasks;

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
		public override Task<bool> IsGranted(Capability cap, Context token, object[] extraArgs)
		{
			// You shall pass!
			return Task.FromResult(true);
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