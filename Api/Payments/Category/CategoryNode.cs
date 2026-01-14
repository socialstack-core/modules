using Newtonsoft.Json;
using System.Collections.Generic;

namespace Api.Payments
{
    /// <summary>
    /// A generic node in a category tree.
    /// </summary>
    /// <typeparam name="TCategory">The type of the category (e.g., ProductCategory).</typeparam>
    /// <typeparam name="TCategoryNode">The type of the category node (e.g., ProductCategoryNode).</typeparam>
    public class CategoryNode<TCategory, TCategoryNode>
        where TCategoryNode : CategoryNode<TCategory, TCategoryNode>
    {
        /// <summary>
        /// The category this node represents, e.g.ProductCategory
        /// </summary>
        public TCategory Category;

        /// <summary>
        /// The direct children of this category node.
        /// </summary>
        public List<TCategoryNode> Children { get; set; } = new();

        /// <summary>
        /// The parent of this category node.
        /// </summary>
        [JsonIgnore]
        public TCategoryNode Parent { get; set; }

        /// <summary>
        /// Breadcrumb-style ancestry path including the category itself.
        /// </summary>
        public List<TCategory> BreadcrumbCategories;

        /// <summary>
        /// Products associated with this category node.
        /// </summary>
        public List<ProductNode> Products { get; set; } = new();

        /// <summary>
        /// Product templates associated with this category node.
        /// </summary>
        public List<TemplateNode> Templates { get; set; } = new();
    }

    /// <summary>
    /// The category tree storage structure
    /// </summary>
    /// <typeparam name="TCategoryNode">The type of the category node (e.g., ProductCategoryNode).</typeparam>
    public struct CategoryTree<TCategoryNode>
    {
	    /// <summary>
	    /// The category node roots
	    /// </summary>
        public List<TCategoryNode> Roots;
	    
	    /// <summary>
	    /// The ID lookup dictionary
	    /// </summary>
        public Dictionary<uint, TCategoryNode> IdLookup;
	    
	    /// <summary>
	    /// The slug lookup dictionary
	    /// </summary>
        public Dictionary<string, TCategoryNode> SlugLookup;
    }    

	/// <summary>
	/// A location in the category tree.
	/// </summary>
	public struct CategoryTreeLocation
	{
		/// <summary>
		/// The path to resolve relative to. Empty string (not /) indicates root set.
		/// </summary>
		public string Path;
	}

}
