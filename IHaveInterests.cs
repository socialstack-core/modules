using Api.Permissions;
using Api.Tags;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.TaggedInterests
{
	/// <summary>
	/// Implement this interface on a type to automatically add interests support (just a renamed tags).
	/// If you're extending a built in type, it's best to put the extension in its own module under Api/Tags/Types/{TypeName}.
	/// </summary>
	public partial interface IHaveInterests
    {
		/// <summary>
		/// The interests on this content. Implement this as a public property.
		/// </summary>
		List<Tag> Interests { get; set; }
	}
}
