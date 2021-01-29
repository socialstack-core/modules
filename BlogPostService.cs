using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using Api.Startup;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Blogs
{
	/// <summary>
	/// Handles blogs posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class BlogPostService : AutoService<BlogPost>
    {
        private BlogService _blogs;
		private BlogPostService _blogPosts;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		/// <param name="blogs"></param>
		public BlogPostService(BlogService blogs) : base(Events.BlogPost)
        {
			_blogs = blogs;
			
			// Before Create to make sure that the slug is unique.
			Events.BlogPost.BeforeCreate.AddEventListener(async (Context context, BlogPost blogPost) =>
			{
				var slug = "";

				if(context == null || blogPost == null)
                {
					return null;
                }

				if (_blogPosts == null)
				{
					_blogPosts = Services.Get<BlogPostService>();
				}

				// Was a slug passed in? if so, just pass the blogPost on.
				if (blogPost.Slug != null && blogPost.Slug != "")
                {
					blogPost.Slug = await _blogPosts.GetSlug(context, blogPost.Title, blogPost.Slug);
					return blogPost;
                }

				// No slug was added, let's get one. 
				slug = await _blogPosts.GetSlug(context, blogPost.Title);

				blogPost.Slug = slug;

				return blogPost;
			});

			Events.BlogPost.BeforeUpdate.AddEventListener(async (Context context, BlogPost blogPost) =>
			{
				var slug = "";

				if (context == null || blogPost == null)
				{
					return null;
				}

				if (_blogPosts == null)
				{
					_blogPosts = Services.Get<BlogPostService>();
				}

				// Was a slug passed in? if so, just pass the blogPost on.
				if (blogPost.Slug != null && blogPost.Slug != "")
				{
					blogPost.Slug = await _blogPosts.GetSlug(context, blogPost.Title, blogPost.Slug, blogPost.Id);
					return blogPost;
				}

				// No slug was added, let's get one. 
				slug = await _blogPosts.GetSlug(context, blogPost.Title, null, blogPost.Id);

				blogPost.Slug = slug;

				return blogPost;
			});
		}


		public async ValueTask<string> GetSlug(Context context, string title, string slugCheck = null, int? exclusionId = null)
        {
			string slug;

			if (slugCheck == null)
			{
				//First to lower case
				slug = title.ToLowerInvariant();

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
			}
			else
			{
				slug = slugCheck;
			}

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
				return null;
			}
			
			if (post.PageId == 0)
			{
				// Default page ID applied now:
				post.PageId = blog.PostPageId;
			}
			
			return await base.Create(context, post);
		}
	}

}
