using Microsoft.AspNetCore.Mvc;


namespace Api.BlogPosts
{
    /// <summary>
    /// Handles blog post endpoints.
    /// </summary>
    [Route("v1/blog/post")]
	public partial class BlogPostController : AutoController<BlogPost, BlogPostAutoForm>
    {
		
    }

}