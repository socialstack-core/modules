using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace Api.Tags
{
	/// <summary>
	/// Handles tags - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class TagService : AutoService<Tag>, ITagService
    {
		private readonly Query<TagContent> listByObjectQuery;
		private readonly Query<TagContent> listContentQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TagService() : base(Events.Tag)
        {
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			listContentQuery = Query.List<TagContent>();
			listByObjectQuery = Query.List<TagContent>();
			listByObjectQuery.Where().EqualsArg("ContentTypeId", 0).And().EqualsArg("ContentId", 1);

			// Because of IHaveTags, Tag must be nestable:
			MakeNestable();

			InstallAdminPages("Tags", "fa:fa-tags", new string[] { "id", "name" });

			#warning todo - handle create and update events. Applies to tags, categories, reactions etc.
			// -> I.e. permit changing tags during entity create/ update.

			// Load tags on Load/List events next. First, find all events for types that implement IHaveTags:
			var loadEvents = Events.FindByType(typeof(IHaveTags), "Load", EventPlacement.After);

			foreach (var loadEvent in loadEvents)
			{
				loadEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// The primary object is always the first arg.
					// It's both IHaveTags and a DatabaseRow object:
					if (!(args[0] is IHaveTags taggableObject))
					{
						// Due to the way how event chains work, the primary object can be null.
						// Safely ignore this.
						return null;
					}

					if (!(args[0] is DatabaseRow dbObject))
					{
						throw new System.Exception(
							"Tags are only available on DatabaseRow entities. " +
							"This type implements IHaveTags but isn't in the database: " + args[0].GetType().Name
						);
					}

					// Get the content type ID for the primary object:
					var contentTypeId = ContentTypes.GetId(taggableObject.GetType());

					// List the tags now:
					var contentTags = await _database.List(listByObjectQuery, null, contentTypeId, dbObject.Id);

					if (contentTags == null || contentTags.Count == 0)
					{
						// None found - just set an empty tag array.
						taggableObject.Tags = new List<Tag>();
					}
					else if (contentTags.Count == 1)
					{
						// Use a higher speed prepared statement:
						taggableObject.Tags = new List<Tag>(1)
						{
							await Get(context, contentTags[0].TagId)
						};
					}
					else
					{
						// It has multiple tags. Use a filtered list here.
						var filter = new Filter<Tag>();

						var ids = new object[contentTags.Count];
						for (var i = 0; i < ids.Length; i++)
						{
							ids[i] = contentTags[i].TagId;
						}

						filter.EqualsSet("Id", ids);
						taggableObject.Tags = await List(context, filter);
					}

					return taggableObject;
				});

			}

			// Next the List events:
			var listEvents = Events.FindByType(typeof(IHaveTags), "List", EventPlacement.After);

			foreach (var listEvent in listEvents)
			{
				listEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// args[0] is a List of IHaveTags implementors.
					if (!(args[0] is IList list))
					{
						// Can't handle this (or it was null anyway):
						return args[0];
					}

					// First we'll collect all their IDs so we can do a single bulk lookup.
					// ASSUMPTION: The list is not excessively long!
					// FUTURE IMPROVEMENT: Do this in chunks of ~50k entries.
					// (applies to at least categories/ tags).

					var ids = new object[list.Count];
					var contentLookup = new Dictionary<int, IHaveTags>();
					int contentTypeId = 0;

					for (var i = 0; i < ids.Length; i++)
					{
						// *must* be DatabaseRow entries:
						if (!(list[i] is DatabaseRow entry) || !(list[i] is IHaveTags ihc))
						{
							throw new System.Exception("Tags are only available on DatabaseRow entities. " +
							"Failed on this type: " + list[i].GetType().Name);
						}

						// Get the content type ID for the primary object:
						if (contentTypeId == 0)
						{
							contentTypeId = ContentTypes.GetId(entry.GetType());
						}

						// Add to content lookup so we can map the tags to it shortly:
						contentLookup[entry.Id] = ihc;

						// Setup empty tag array:
						ihc.Tags = new List<Tag>();

						ids[i] = entry.Id;
					}

					if (ids.Length == 0)
					{
						// Nothing to do - just return here:
						return list;
					}

					// Create the filter and run the query now:
					var filter = new Filter<TagContent>();
					filter.EqualsArg("ContentTypeId", 0).And().EqualsSet("ContentId", ids);

					// Get all the content tags for these entities:
					var allContentTags = await _database.List(listContentQuery, filter, contentTypeId);

					// Build fast tag lookup:
					var tagLookup = new Dictionary<int, Tag>();

					// Get the unique set of tag IDs so we can collect those categories. Shortly will reuse the same lookup dict.
					foreach (var contentTag in allContentTags)
					{
						tagLookup[contentTag.TagId] = null;
					}

					if (tagLookup.Count == 0)
					{
						// Nothing to do - just return here:
						return list;
					}

					// Array of unique tag IDs:
					var tagIds = new object[tagLookup.Count];
					var index = 0;

					foreach (var kvp in tagLookup)
					{
						tagIds[index++] = kvp.Key;
					}

					var tagFilter = new Filter<Tag>();
					tagFilter.EqualsSet("Id", tagIds);
					var categories = await List(context, tagFilter);

					foreach (var category in categories)
					{
						tagLookup[category.Id] = category;
					}

					// For each content->tag relation..
					foreach (var contentCategory in allContentTags)
					{
						// Lookup content/ category:
						var content = contentLookup[contentCategory.ContentId];
						var category = tagLookup[contentCategory.TagId];

						// Add the tag to the content:
						content.Tags.Add(category);
					}

					return list;
				});

			}

			// We'll handle where field "Tags" ourselves:
			Filter.DeclareCustomWhereField("Tags");

			// Next we'll add a handler for "Tags" filters on List endpoints.
			// These are the *List events (but unlike above, we're using the "NotSpecified" placement):
			var listOnEvents = Events.FindByType(typeof(IHaveTags), "List", EventPlacement.NotSpecified);

			foreach (var listEvent in listOnEvents)
			{
				listEvent.AddEventListener((Context context, object[] args) =>
				{
					// These always have:
					// args[0] is a filter object
					// args[1] is for the Response

					var filter = args[0] as Filter;

					if (filter == null || filter.FromRequest == null)
					{
						// We can't handle this - return the first arg just in case it was something else:
						return args[0];
					}

					var where = filter.FromRequest["where"] as JObject;

					if (where == null)
					{
						// No where clause
						return args[0];
					}

					// If the filter contained a "Tags" key then we'll add a join restriction:
					var tagSet = where["Tags"] as JArray;

					if (tagSet != null)
					{
						// We've got tags that we need to filter by. For now this set must be integer tag IDs.
						// We want to join the tag content table 
						// on WhateverTypeTheFilterIsUsing.Id = TagContent.Id AND ContentTypeId = TheIDOfThatFilterType
						// AND TagId IN(tagSet)

						// Need an object[] first:
						var idArray = new object[tagSet.Count];

						for (var i = 0; i < idArray.Length; i++)
						{
							var tagId = tagSet[i].Value<int>();
							idArray[i] = tagId;
						}

						filter.Join<TagContent>("Id", "ContentId")
							.And().Equals("ContentTypeId", ContentTypes.GetId(filter.DefaultType))
							.And().EqualsSet("TagId", idArray);

					}

					return args[0];
				});
			}

		}
		
	}
    
}
