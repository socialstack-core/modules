using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Rewards
{
	/// <summary>
	/// Implement this interface on a type to automatically add reward support.
	/// If you're extending a built in type, it's best to put the extension in its own module under Api/Rewards/Types/{TypeName}.
	/// </summary>
	public partial interface IHaveRewards
    {
		/// <summary>
		/// The rewards on this content. Implement this as a public property.
		/// </summary>
		List<Reward> Rewards { get; set; }
	}
}
