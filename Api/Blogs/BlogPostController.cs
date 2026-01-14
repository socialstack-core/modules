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
    }
}