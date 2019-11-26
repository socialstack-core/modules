using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Categories
{
	/// <summary>
	/// Implement this interface on a type to automatically add category support.
	/// If you're extending a built in type, it's best to put the extension in its own module under Api/Categories/Types/{TypeName}.
	/// </summary>
	public partial interface IHaveCategories
    {
		/// <summary>
		/// The categories of this content. Implement this as a public property.
		/// </summary>
		List<Category> Categories { get; set; }
	}
}
