using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles productCategory endpoints.</summary>
    [Route("v1/productCategory")]
    public class ProductCategoryController
        : BaseCategoryController<ProductCategory, ProductCategoryNode, ProductCategoryService>
    {
        /// <summary>
    /// Empty constructor.
        /// </summary>
        /// <param name="service"></param>
        public ProductCategoryController(ProductCategoryService service) : base(service)
        {
        }
    }
}
