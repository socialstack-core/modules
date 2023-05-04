using Microsoft.AspNetCore.Mvc;

namespace Api.Blogs
{
    /// <summary>
    /// Handles blog endpoints.
    /// </summary>

    [Route("v1/blog")]
	public partial class BlogController : AutoController<Blog>
	{
    }

}
