using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Blogs
{
    /// <summary>
    /// Handles blog endpoints.
    /// </summary>

    [Route("v1/blog")]
	public partial class BlogController : AutoController<Blog, BlogAutoForm>
	{
    }

}
