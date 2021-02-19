using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using Api.Startup;
using Api.Permissions;
using System.Collections.Generic;
using Api.Configuration;
using Newtonsoft.Json.Linq;
using Api.CanvasRenderer;
using System;

namespace Api.Blogs
{
	/// <summary>
	/// Handles blogs posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class BlogPostService : AutoService<BlogPost>
    {
        private readonly BlogService _blogs;
		private CanvasRendererService _csr;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		/// <param name="blogs"></param>
		public BlogPostService(BlogService blogs) : base(Events.BlogPost)
        {
			_blogs = blogs;
			
			InstallAdminPages(null, null, new string[] { "id", "slug", "title" });
			
			var config = _blogs.GetConfig<BlogServiceConfig>();

			// Before Create to make sure that the slug is unique.
			Events.BlogPost.BeforeCreate.AddEventListener(async (Context context, BlogPost blogPost) =>
			{
				var slug = "";

				if(context == null || blogPost == null)
                {
					return null;
                }

				// Was a slug passed in? if so, just pass the blogPost on.
				if (blogPost.Slug != null && blogPost.Slug != "")
                {
					blogPost.Slug = await GetSlug(context, blogPost.Title, blogPost.Slug);
					return blogPost;
                }

				// No slug was added, let's get one. 
				if (config.GenerateSlugs)
                {
					slug = await GetSlug(context, blogPost.Title);

					blogPost.Slug = slug;
				}
				
				return blogPost;
			});

			// Before update to make sure the slug is unique.
			Events.BlogPost.BeforeUpdate.AddEventListener(async (Context context, BlogPost blogPost) =>
			{
				var slug = "";

				if (context == null || blogPost == null)
				{
					return null;
				}

				// Was a slug passed in? if so, just pass the blogPost on.
				if (blogPost.Slug != null && blogPost.Slug != "")
				{
					// Let's make sure the provided slug is unique.
					blogPost.Slug = await GetSlug(context, blogPost.Title, blogPost.Slug, blogPost.Id);
					return blogPost;
				}

				if(config.GenerateSlugs)
                {
					// No slug was added, let's get one if we are generating slugs. 
					slug = await GetSlug(context, blogPost.Title, null, blogPost.Id);

					blogPost.Slug = slug;
				}

				return blogPost;
			});

			// We need event listeners for handling synopsis. It should generate when synopsis is blank on creation and update.
			if (config.GenerateSynopsis)
            {
				Events.BlogPost.BeforeCreate.AddEventListener(async (Context context, BlogPost blogPost) =>
				{
					var synopsis = "";

					if (context == null || blogPost == null)
					{
						return null;
					}

					// Was a synopsis passed in? if so, just pass the blogPost on.
					if (blogPost.Synopsis != null && blogPost.Synopsis != "")
					{
						return blogPost;
					}

					// Was a synopsis passed in? if so, just pass the blogPost on.
					if (blogPost.Synopsis != null && blogPost.Synopsis != "")
					{
						return blogPost;
					}

					if (_csr == null)
					{
						_csr = Services.Get<CanvasRendererService>();
					}

					// No synopsis was added, let's get one based on the body json. 
					var renderedPage = await _csr.Render(context, blogPost.BodyJson, null, false, null, RenderMode.Text);

					synopsis = renderedPage.Text;

					if (synopsis.Length > 500)
					{
						synopsis = synopsis.Substring(0, 497) + "...";
					}

					blogPost.Synopsis = synopsis;

					return blogPost;
				});

				Events.BlogPost.BeforeUpdate.AddEventListener(async (Context context, BlogPost blogPost) =>
				{
					var synopsis = "";

					if (context == null || blogPost == null)
					{
						return null;
					}

					// Was a synopsis passed in? if so, just pass the blogPost on.
					if (blogPost.Synopsis != null && blogPost.Synopsis != "")
					{
						return blogPost;
					}

					if(_csr == null)
                    {
						_csr =  Services.Get<CanvasRendererService>();
                    }

					// No synopsis was added, let's get one based on the body json. 
					var renderedPage = await _csr.Render(context, blogPost.BodyJson, null, false, null, RenderMode.Text);

					synopsis = renderedPage.Text;

					if (synopsis.Length > 500)
					{
						synopsis = synopsis.Substring(0, 497) + "...";
					}

					blogPost.Synopsis = synopsis;

					return blogPost;
				});
			}	
		}

		/// <summary>
		/// Used to get a slug
		/// </summary>
		/// <param name="context"></param>
		/// <param name="title"></param>
		/// <param name="slugCheck"></param>
		/// <param name="exclusionId"></param>
		/// <returns></returns>
		public async ValueTask<string> GetSlug(Context context, string title, string slugCheck = null, int? exclusionId = null)
        {
			var config = _blogs.GetConfig<BlogServiceConfig>();
			string slug;

			if (slugCheck == null)
			{
				//First to lower case
				slug = title.ToLowerInvariant();
			}
			else
            {
				slug = slugCheck.ToLowerInvariant();
            }

			//Remove all accents
			var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(slug);
			slug = Encoding.ASCII.GetString(bytes);

			//Replace spaces
			slug = Regex.Replace(slug, @"\s", "-", RegexOptions.Compiled);

			//Remove invalid chars
			slug = Regex.Replace(slug, @"[^a-z0-9\s-_]", "", RegexOptions.Compiled);

			//Trim dashes from end
			slug = slug.Trim('-', '_');

			//Replace double occurences of - or _
			slug = Regex.Replace(slug, @"([-_]){2,}", "$1", RegexOptions.Compiled);		

			// Do we need a unique slug?
			if(config.UniqueSlugs)
            {
				var postsWithSlug = new List<BlogPost>();

				// Now let's see if the slug is in use.
				if (exclusionId == null)
				{
					postsWithSlug = await List(context, new Filter<BlogPost>().Equals("Slug", slug));
				}
				else
				{
					postsWithSlug = await List(context, new Filter<BlogPost>().Equals("Slug", slug).And().Not().Equals("Id", exclusionId));
				}

				var increment = 0;

				// Is the slug in use
				while (postsWithSlug.Count > 0)
				{
					increment++;
					if (exclusionId == null)
					{
						postsWithSlug = await List(context, new Filter<BlogPost>().Equals("Slug", slug + "-" + increment));
					}
					else
					{
						postsWithSlug = await List(context, new Filter<BlogPost>().Equals("Slug", slug + "-" + increment).And().Not().Equals("Id", exclusionId));
					}
				}

				if (increment > 0)
				{
					slug = slug + "-" + increment;
				}
			}
			
			return slug;
		}

		/// <summary>
		/// Creates a new blog post.
		/// </summary>
		public override async ValueTask<BlogPost> Create(Context context, BlogPost post)
		{
			// Get the blog to obtain the default page ID:
			var blog = await _blogs.Get(context, post.BlogId);

			if (blog == null)
			{
				// Blog doesn't exist!
				throw new PublicException("Blog with ID " + post.BlogId + " doesn't exist or is unusable.");
			}
			
			return await base.Create(context, post);
		}
	}

}
