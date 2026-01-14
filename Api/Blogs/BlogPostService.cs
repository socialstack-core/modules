using Api.CanvasRenderer;
using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using Api.Startup;
using Api.Translate;
using Api.Users;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Api.Blogs
{
    /// <summary>
    /// Handles blogs posts.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>

    // Ensure that this loads after the search service
    [LoadPriority(200)]
    public partial class BlogPostService : AutoService<BlogPost>
    {
        private readonly BlogService _blogs;
		private PermalinkService _permalinks;
		// private BlogServiceConfig _blogConfig;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		/// <param name="permalinks"></param>
		/// <param name="blogs"></param>
		/// <param name="pages"></param>
		public BlogPostService(PermalinkService permalinks, BlogService blogs, PageService pages) : base(Events.BlogPost)
        {
			_blogs = blogs;
			_permalinks = permalinks;

			InstallAdminPages(
				new AdminPageOptions()
				{
					NavMenuLabel = new Localized<string>("Blog posts"),
					NavMenuIcon = "fa:fa-blog",
					NavMenuParentKey = "blogs",
					ListFields = ["id", "slug", "title"],
					Tabs = [
						new AdminTab("Content", "content"),
						new AdminTab("Details", "details")
					]
				}
			);

			pages.Install(
				// Install a default primary blog page.
				new PageBuilder()
				{
					Key = "primary:blogpost",
					PrimaryContentIncludes = "creatorUser",
					Title = "${blogpost.title}",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/BlogPost/View").WithPrimaryLink("blogpost")
						);
					}
				}
			);

			var config = _blogs.GetConfig<BlogServiceConfig>();
			// _blogConfig = config;

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

				await UpdateSlug(context, blogPost);
				
				if (config.GenerateSynopsis)
				{
					// Was a synopsis passed in? if so, just pass the blogPost on.
					UpdateSynopsis(context, blogPost);
				}
				
				return blogPost;
			});

			// Before update to make sure the slug is unique.
			Events.BlogPost.BeforeUpdate.AddEventListener(async (Context context, BlogPost blogPost, BlogPost original) =>
			{
				if (context == null || blogPost == null)
				{
					return null;
				}

				if (config.GenerateSynopsis)
				{
					UpdateSynopsis(context, blogPost);
				}

				await UpdateSlug(context, blogPost);

				if (blogPost.Slug != original.Slug && !string.IsNullOrEmpty(blogPost.Slug))
				{
					// Create new permalink.
					await CreatePermalink(context, blogPost);
				}

				return blogPost;
			});

			Events.BlogPost.AfterCreate.AddEventListener(async (Context context, BlogPost blogPost) =>
			{
				if (blogPost == null)
				{
					return blogPost;
				}

				await CreatePermalink(context, blogPost);

				return blogPost;
			});
		}

		private async ValueTask CreatePermalink(Context context, BlogPost blogPost)
		{
			if (blogPost == null || string.IsNullOrEmpty(blogPost.Slug))
			{
				return;
			}

			// Permalink target which will be for whichever page wants to handle a blogpost as its primary content.
			// If a specific page for this blogpost exists, it will ultimately pick that.
			var linkTarget = _permalinks.CreatePrimaryTargetLocator(this, blogPost);

			await _permalinks.Create(
				context,
				new Permalink()
				{
					Url = "/news/" + blogPost.Slug,
					Target = linkTarget
				},
				DataOptions.IgnorePermissions
			);
		}

		private async ValueTask UpdateSlug(Context context, BlogPost blogPost)
		{
			// Was a slug passed in? if so, just pass the blogPost on.
			if (!string.IsNullOrEmpty(blogPost.Slug))
			{
				// Let's make sure the provided slug is unique.
				blogPost.Slug = await EnsureUniqueSlug(context, blogPost.Slug, blogPost.Id);
			}
			else
			{
				// No slug present, generate one. 
				var slug = GenerateNormalizedSlug(blogPost.Title);
				blogPost.Slug = await EnsureUniqueSlug(context, slug, blogPost.Id);
			}
		}

		private void UpdateSynopsis(Context context, BlogPost blogPost)
		{
			// Was a synopsis passed in? if so, just pass the blogPost on.
			if (blogPost == null || !string.IsNullOrEmpty(blogPost.Synopsis))
			{
				return;
			}
			
			// No synopsis was added, let's get one based on the body html. 
			var synopsis = GenerateSynopsis(blogPost.BodyHtml);
			blogPost.Synopsis = synopsis;
		}

		/// <summary>
		/// Generates a basic synopsis from a HTML string.
		/// </summary>
		/// <param name="html"></param>
		/// <param name="maxLength"></param>
		/// <returns></returns>
		private static string GenerateSynopsis(string html, int maxLength = 500)
		{
			if (string.IsNullOrWhiteSpace(html))
				return string.Empty;

			// 1. Load HTML into HtmlAgilityPack
			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			// 2. Extract inner text (strips tags safely)
			string text = doc.DocumentNode.InnerText;

			// 3. Decode HTML entities (&amp;, &nbsp;, etc.)
			text = HttpUtility.HtmlDecode(text);

			// 4. Normalize whitespace
			text = string.Join(" ", text.Split(new[] { ' ', '\r', '\n', '\t' },
											   StringSplitOptions.RemoveEmptyEntries));

			// 5. Cut to max length
			if (text.Length <= maxLength)
				return text;

			// 6. Try to cut at the last space before limit
			int lastSpace = text.LastIndexOf(' ', maxLength);
			if (lastSpace > 0)
				return text.Substring(0, lastSpace) + "...";

			return text.Substring(0, maxLength) + "...";
		}

		private static string GenerateNormalizedSlug(string phrase)
		{
			if (string.IsNullOrWhiteSpace(phrase))
				return string.Empty;

			// remove accents
			string str = RemoveDiacritics(phrase);

			// custom replacements (ß, ø, æ, etc.)
			str = str.Replace("ß", "ss")
					 .Replace("ø", "o")
					 .Replace("Ø", "O")
					 .Replace("æ", "ae")
					 .Replace("Æ", "Ae");

			// lowercase
			str = str.ToLowerInvariant();

			// replace anything not alphanumeric with hyphens
			str = Regex.Replace(str, @"[^a-z0-9]+", "-");

			// trim extra hyphens
			str = str.Trim('-');

			// Replace double occurences of - or _
			str = Regex.Replace(str, @"([-_]){2,}", "$1", RegexOptions.Compiled);

			return str;
		}

		private static string RemoveDiacritics(string text)
		{
			var normalizedString = text.Normalize(NormalizationForm.FormD);
			var stringBuilder = new StringBuilder();

			foreach (var c in normalizedString)
			{
				var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
				if (unicodeCategory != UnicodeCategory.NonSpacingMark)
				{
					stringBuilder.Append(c);
				}
			}

			return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
		}

		/// <summary>
		/// Ensures the given slug string is unique.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="slug"></param>
		/// <param name="exclusionId"></param>
		/// <returns></returns>
		public async ValueTask<string> EnsureUniqueSlug(Context context, string slug, uint exclusionId)
		{
			// Now let's see if the slug is in use.
			var postWithSlug = await Where("Slug=? and Id!=?", DataOptions.IgnorePermissions)
				.Bind(slug)
				.Bind(exclusionId)
				.First(context);
			
			var increment = 0;

			// Is the slug in use
			while (postWithSlug != null)
			{
				increment++;
				postWithSlug = await Where("Slug=?", DataOptions.IgnorePermissions).Bind(slug + "-" + increment).First(context);
			}

			if (increment > 0)
			{
				slug = slug + "-" + increment;
			}

			return slug;
		}

	}

}
