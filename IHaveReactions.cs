using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Reactions
{
	/// <summary>
	/// Implement this interface on a type to automatically add reaction support.
	/// If you're extending a built in type, it's best to put the extension in its own module under Api/Reactions/Types/{TypeName}.
	/// </summary>
	public partial interface IHaveReactions
    {
		/// <summary>
		/// The reactions on this content. Implement this as a public property.
		/// </summary>
		List<ReactionCount> Reactions { get; set; }
	}
}
