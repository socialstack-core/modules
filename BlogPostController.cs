using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Api.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using Api.Permissions;

namespace Api.Blogs
{
    /// <summary>
    /// Handles blog post endpoints.
    /// </summary>
    [Route("v1/blogpost")]
	public partial class BlogPostController : AutoController<BlogPost>
    {
        private readonly BlogPostService _blogPostService;

        /// <summary>
        /// 
        /// </summary>
        public BlogPostController(BlogPostService bp)
        {
            _blogPostService = bp;
        }
	
        /// <summary>
        /// Used to get a unique url provided a title string. We will generate a string and make sure its not in use. If it is, we will add iterations until its good.
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        [HttpPost("getslug")]
        public async Task<string> GetSlug([FromBody]NewTitle title)
        {
            var context = Request.GetContext();
            
            if (context.Role == null || !context.Role.CanViewAdmin)
            {
                // Must be an admin type user
                return null;
            }

            if (title == null || string.IsNullOrWhiteSpace(title.Title))
            {
                return null;
            }

            var slug = await _blogPostService.GetSlug(context, title.Title);

            // Now let's see if the slug is in use.
            var postsWithSlug = await _service.List(context, new Filter<BlogPost>().Equals("Slug", slug), DataOptions.IgnorePermissions);

            var increment = 0;

            // Is the slug in use
            while (postsWithSlug.Count > 0 )
            {
                increment++;
                postsWithSlug = await _service.List(context, new Filter<BlogPost>().Equals("Slug", slug + "-" + increment), DataOptions.IgnorePermissions);
            }

            if (increment > 0)
            {
                slug = slug + "-" + increment;
            }

            return slug;
        }
    }

    /// <summary>
    /// Used when getting a new slug
    /// </summary>
    public class NewTitle
    {
        /// <summary>
        /// The new password.
        /// </summary>
        public string Title;
    }

}