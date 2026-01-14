using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.NavMenus;
using Api.Permissions;
using Api.Startup;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Blogs
{
    /// <summary>
    /// Handles blogs - containers for individual blog posts.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>

    // Ensure that this loads after the search service
    [LoadPriority(200)]

    public partial class BlogService : AutoService<Blog>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public BlogService() : base(Events.Blog)
        {
			AdminNavMenuItemService.RequiredGroups.Add(new()
			{
				Title = "Blogging",
				Key = "blogs",
				IconRef = "fa:fa-blog",
				ParentId = 0
			});

			InstallAdminPages(
				new AdminPageOptions() {
					NavMenuIcon = "fa:fa-blog",
					NavMenuLabel = new Translate.Localized<string>("Blogs"),
					NavMenuParentKey = "blogs",
					ListFields = new string[] { "id", "name" },

					/*
					// Each blog page also has a list of blogpost's on it:
					ChildType = new AdminPageOptions()
					{
						ContentType = "BlogPost",
						ListFields = new string[] { "title" },
						SearchFields = new string[] { "title" }
					}
					*/
				}
			);

			// A site has 1 blog unless configured otherwise.
			var config = GetConfig<BlogServiceConfig>();

			Events.BlogPost.BeforeSettable.AddEventListener((Context ctx, JsonField<BlogPost, uint> field) => {

				if (field == null)
				{
					return new ValueTask<JsonField<BlogPost, uint>>(field);
				}

				if (field.Name == "BlogId" && !config.MultipleBlogs)
				{
					// Not settable if 1 blog.
					field = null;
				}

				return new ValueTask<JsonField<BlogPost, uint>>(field);
			});
		}
	}
    
}
