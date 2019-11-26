using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Tags
{
	/// <summary>
	/// Implement this interface on a type to automatically add tag support.
	/// If you're extending a built in type, it's best to put the extension in its own module under Api/Tags/Types/{TypeName}.
	/// </summary>
	public partial interface IHaveTags
    {
		/// <summary>
		/// The tags on this content. Implement this as a public property.
		/// </summary>
		List<Tag> Tags { get; set; }
	}
}
