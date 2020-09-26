using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Tags;
using Api.Users;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;

namespace Api.TaggedInterests
{
	/// <summary>
	/// Handles interests.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class TaggedInterestService
    {	
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TaggedInterestService(DatabaseService _database)
        {
			// Interests which are simply thinly renamed Tags.

			// Define the IHaveInterests handler:
			var mapper = new IHaveArrayHandler<IHaveInterests, Tag, TagContent>()
			{
				WhereFieldName = "Interests",
				MapperFieldName = "TagId",
				OnSetResult = (IHaveInterests content, List<Tag> results) =>
				{
					content.Interests = results;
				},
				Database = _database
			};

			mapper.Map();
		}
	}
    
}
