using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;

namespace Api.Payments
{
	
	/// <summary>
	/// A ProductCategory
	/// </summary>
	 
	// A product category can have a single parent
	[HasVirtualField("ProductCategory", typeof(ProductCategory), "ParentId")]

    // products can be linked to one or more categories (normally the lowest child)
    [ListAs("ProductCategories", Tab = "tags_categories")]
    [ImplicitFor("ProductCategories", typeof(Product))]
    [ImplicitFor("ProductCategories", typeof(ProductTemplate))]

    public partial class ProductCategory : BaseCategory
	{
	}

}