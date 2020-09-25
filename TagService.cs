using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using System.Collections;
using Newtonsoft.Json.Linq;
using Api.Startup;
using System;
using Api.Users;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Api.Tags
{
	/// <summary>
	/// Handles tags - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class TagService : AutoService<Tag>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TagService() : base(Events.Tag)
		{
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.

			// Because of IHaveTags, Tag must be nestable:
			MakeNestable();

			InstallAdminPages("Tags", "fa:fa-tags", new string[] { "id", "name" });

			// Define the IHaveTags handler:
			DefineIHaveArrayHandler<IHaveTags, Tag, TagContent>(
				"Tags",
				"TagId",
				(IHaveTags content, List<Tag> results) =>
				{
					content.Tags = results;
				}
			);
		}

	}
    
}
