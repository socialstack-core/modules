using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

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
