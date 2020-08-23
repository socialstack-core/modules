using Api.Contexts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Permissions
{

	/// <summary>
	/// A node in a filter.
	/// </summary>
	public partial class FilterNode
	{
		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		/// <param name="cap"></param>
		/// <param name="token"></param>
		/// <param name="extraArgs"></param>
		/// <returns></returns>
		public virtual Task<bool> IsGranted(Capability cap, Context token, object[] extraArgs)
		{
			// You shall not pass!
			return Task.FromResult(false);
		}
		
		/// <summary>
		/// True if this filter node is active on the given object.
		/// </summary>
		public virtual bool Matches(List<ResolvedValue> values, object obj){
			return false;
		}
		
		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public virtual FilterNode Copy()
		{
			return new FilterNode();
		}

		/// <summary>
		/// Makes sure this filter node is fully constructed.
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		public virtual FilterNode Construct(Filter filter)
		{
			// Most nodes are just as-is. 
			// It's only really operators that do something here.
			return this;
		}
	}

}