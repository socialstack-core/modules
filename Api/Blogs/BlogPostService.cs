using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using Api.Startup;
using System.Collections.Generic;
using Api.CanvasRenderer;
using Api.Users;

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
		private UserService _users;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		/// <param name="blogs"></param>
		public BlogPostService(BlogService blogs) : base(Events.BlogPost)
        {
			_blogs = blogs;
			
			InstallAdminPages(null, null, new string[] { "id", "slug", "title" });
			
			var config = _blogs.GetConfig<BlogServiceConfig>();

			//Before create to set the author of the blog to the author if the value passed in is not valid, or not set.
			Events.BlogPost.BeforeCreate.AddEventListener(async (Context context, BlogPost blogPost) =>
			{
				if(context == null || blogPost == null)
                {
					return null;
                }

				// Set blog ID to 1 if the site only has 1:
				if (config.MultipleBlogs)
				{

					// Get the blog to obtain the default page ID:
					var blog = await _blogs.Get(context, blogPost.BlogId, DataOptions.IgnorePermissions);

					if (blog == null)
					{
						// Blog doesn't exist!
						throw new PublicException("Blog with ID " + blogPost.BlogId + " doesn't exist or is unusable.", "blog_notfound");
					}
				}
				else
				{
					blogPost.BlogId = 1;
				}

				// Was a slug passed in? if so, just pass the blogPost on.
				if (blogPost.Slug != null && blogPost.Slug != "")
				{
					blogPost.Slug = await GetSlug(context, blogPost.Title, blogPost.Slug);
				}
				else if (config.GenerateSlugs)
				{
					blogPost.Slug = await GetSlug(context, blogPost.Title);
				}
				
				if (config.GenerateSynopsis)
				{
					// Was a synopsis passed in? if so, just pass the blogPost on.
					if (string.IsNullOrEmpty(blogPost.Synopsis))
					{
						var synopsis = "";

						// Was a synopsis passed in? if so, just pass the blogPost on.
						if (string.IsNullOrEmpty(blogPost.Synopsis))
						{
							if (_csr == null)
							{
								_csr = Services.Get<CanvasRendererService>();
							}

							// No synopsis was added, let's get one based on the body json. 
							var renderedPage = await _csr.Render(context, blogPost.BodyJson, null, RenderMode.Text, false);

							synopsis = renderedPage.Text;

							if (synopsis.Length > 500)
							{
								synopsis = synopsis.Substring(0, 497) + "...";
							}

							blogPost.Synopsis = synopsis;
						}
					}
				}
				
				// was an authorid passed in?
				if (blogPost.AuthorId > 0)
                {
					if(_users == null)
                    {
						_users = Services.Get<UserService>();
                    }

					// Yes, is it for a valid user?
					var author = await _users.Get(context, blogPost.AuthorId, DataOptions.IgnorePermissions);

					if(author == null)
                    {
						// The author is not valid, let's set the author to the creatoruser
						blogPost.AuthorId = context.UserId;
                    }
                }
				else
                {
					// An author wasn't passed - set to creator user.
					blogPost.AuthorId = context.UserId;
				}

				return blogPost;
			});

			// Before update to make sure the slug is unique.
			Events.BlogPost.BeforeUpdate.AddEventListener(async (Context context, BlogPost blogPost, BlogPost original) =>
			{
				var slug = "";

				if (context == null || blogPost == null)
				{
					return null;
				}

				if (config.GenerateSynopsis)
				{
					var synopsis = "";

					// Was a synopsis passed in? if so, just pass the blogPost on.
					if (string.IsNullOrEmpty(blogPost.Synopsis))
					{
						if (_csr == null)
						{
							_csr = Services.Get<CanvasRendererService>();
						}

						// No synopsis was added, let's get one based on the body json. 
						var renderedPage = await _csr.Render(context, blogPost.BodyJson, null, RenderMode.Text, false);

						synopsis = renderedPage.Text;

						if (synopsis.Length > 500)
						{
							synopsis = synopsis.Substring(0, 497) + "...";
						}

						blogPost.Synopsis = synopsis;
					}
				}

				// Was an authorId passed in?
				if (blogPost.AuthorId > 0)
				{
					if (_users == null)
					{
						_users = Services.Get<UserService>();
					}

					// Yes, is it for a valid user?
					var author = await _users.Get(context, blogPost.AuthorId, DataOptions.IgnorePermissions);

					if (author == null)
					{
						// The author is not valid, let's set the author to the creatorUser
						blogPost.AuthorId = blogPost.UserId;
					}
				}
				else
				{
					// no authorId, let's return it to being the creatorUser.
					blogPost.AuthorId = blogPost.UserId;
				}

				// Was a slug passed in? if so, just pass the blogPost on.
				if (blogPost.Slug != null && blogPost.Slug != "")
				{
					// Let's make sure the provided slug is unique.
					blogPost.Slug = await GetSlug(context, blogPost.Title, blogPost.Slug, blogPost.Id);
				}
				else if(config.GenerateSlugs)
                {
					// No slug was added, let's get one if we are generating slugs. 
					slug = await GetSlug(context, blogPost.Title, null, blogPost.Id);
					blogPost.Slug = slug;
				}

				return blogPost;
			});

		}

		/// <summary>
		/// Used to get a slug
		/// </summary>
		/// <param name="context"></param>
		/// <param name="title"></param>
		/// <param name="slugCheck"></param>
		/// <param name="exclusionId"></param>
		/// <returns></returns>
		public async ValueTask<string> GetSlug(Context context, string title, string slugCheck = null, uint? exclusionId = null)
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
				List<BlogPost> postsWithSlug;

				// Now let's see if the slug is in use.
				if (exclusionId == null)
				{
					postsWithSlug = await Where("Slug=?", DataOptions.IgnorePermissions).Bind(slug).ListAll(context);
				}
				else
				{
					postsWithSlug = await Where("Slug=? and Id!=?", DataOptions.IgnorePermissions).Bind(slug).Bind(exclusionId.Value).ListAll(context);
				}

				var increment = 0;

				// Is the slug in use
				while (postsWithSlug.Count > 0)
				{
					increment++;
					if (exclusionId == null)
					{
						postsWithSlug = await Where("Slug=?", DataOptions.IgnorePermissions).Bind(slug + "-" + increment).ListAll(context);
					}
					else
					{
						postsWithSlug = await Where("Slug=? and Id!=?", DataOptions.IgnorePermissions).Bind(slug + "-" + increment).Bind(exclusionId.Value).ListAll(context);
					}
				}

				if (increment > 0)
				{
					slug = slug + "-" + increment;
				}
			}
			
			return slug;
		}

	}

}
