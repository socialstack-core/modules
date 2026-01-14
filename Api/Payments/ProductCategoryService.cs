using Api.CanvasRenderer;
using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using System.Threading.Tasks;

namespace Api.Payments
{
    /// <summary>
    /// The product category service, extends the Base Category Service with data types tailored to product categories.
    /// </summary>
    public partial class ProductCategoryService : BaseCategoryService<ProductCategory, ProductCategoryNode>
    {
        /// <summary>
        /// The category label
        /// </summary>
        public override string CategoryLabel => "Product Category";
        
        /// <summary>
        /// The category field name
        /// </summary>
        public override string CategoryFieldName => "ProductCategories";
        
        /// <summary>
        /// The URL prefix
        /// </summary>
        public override string CategoryUrlPrefix => "category";

		/// <summary>
		/// The constructor (instanced automatically).
		/// </summary>
		/// <param name="products"></param>
		/// <param name="pages"></param>
		/// <param name="permalinks"></param>
		/// <param name="productTemplates"></param>
		public ProductCategoryService(
            ProductService products,
            PageService pages,
            PermalinkService permalinks,
            ProductTemplateService productTemplates
        ) : base(Events.ProductCategory, products, pages, permalinks, productTemplates)
        {
            // No admin menu entry - categories are covered by the main products page
            InstallAdminPages(null, null, ["id", "name"]);

            pages.Install(
                // Install a default primary product category page.
                // Note that this does not define a URL, because we want nice readable slug based URLs.
                // Because slugs can change, the URL is therefore not necessarily constant and thus
                // must be handled at the permalink level, which the event handler further down does.
                new PageBuilder()
                {
                    Key = "primary:productcategory",
                    Title = "${productcategory.name}",
                    PrimaryContentIncludes = "calculatedPrice,breadcrumb,primaryUrl",
                    BuildBody = (PageBuilder builder) =>
                    {
						return builder.AddTemplate(
							new CanvasNode("UI/ProductCategory/Banner").WithPrimaryLink("category"),
							// A prop called 'productCategory' will be the category referenced by the URL.
							// If it does not exist, the page 404s, so you can expect it to be not-null always.
							new CanvasNode("UI/Product/Search").WithPrimaryLink("productCategory").With("showPromotions", true)
						);
                    }
                }
            );

			Events.ProductTemplate.AfterCreate.AddEventListener((Context context, ProductTemplate template) =>
			{
				// clear the cache
				_categoryTree = null;

				return new ValueTask<ProductTemplate>(template);
			});

			Events.ProductTemplate.AfterUpdate.AddEventListener((Context ctx, ProductTemplate template) =>
			{
				// clear the cache
				_categoryTree = null;

				return new ValueTask<ProductTemplate>(template);
			});

			Events.ProductTemplate.AfterDelete.AddEventListener((Context ctx, ProductTemplate template) =>
			{
				// clear the cache
				_categoryTree = null;

				return new ValueTask<ProductTemplate>(template);
			});
		}
    }
}
