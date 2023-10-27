using Microsoft.AspNetCore.Mvc;

namespace Api.Categories
{
    /// <summary>
    /// Handles category endpoints.
    /// </summary>
    [Route("v1/category")]
	public partial class CategoryController : AutoController<Category>
	{
    }
}