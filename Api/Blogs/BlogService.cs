﻿using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using Api.Startup;

namespace Api.Blogs
{
	/// <summary>
	/// Handles blogs - containers for individual blog posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class BlogService : AutoService<Blog>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public BlogService() : base(Events.Blog)
        {
			InstallAdminPages(
				"Blogs", "fa:fa-blog", new string[] { "id", "name" },

				// Each blog page also has a list of blogpost's on it:
				new ChildAdminPageOptions(){
					ChildType = "BlogPost",
					Fields = new string[] { "title" },
					SearchFields = new string[]{ "title" }
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

			InstallRoles(new Role() {
				Key = "blogger",
				Name = "Blogger",
				CanViewAdmin = true
			});
		}
	}
    
}
