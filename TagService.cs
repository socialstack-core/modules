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
		public TagService(ITagContentService _tagContents) : base(Events.Tag)
        {
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			listContentQuery = Query.List<TagContent>();
			listByObjectQuery = Query.List<TagContent>();
			listByObjectQuery.Where().EqualsArg("ContentTypeId", 0).And().EqualsArg("ContentId", 1).And().EqualsArg("RevisionId", 2);

			// Because of IHaveTags, Tag must be nestable:
			MakeNestable();

			InstallAdminPages("Tags", "fa:fa-tags", new string[] { "id", "name" });
			
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
					
					int revisionId = 0;

					if (dbObject is RevisionRow)
					{
						var revId = ((RevisionRow)dbObject).RevisionId;

						if (revId.HasValue)
						{
							revisionId = revId.Value;
						}
					}

					// List the tags now:
					var contentTags = await _database.List(context, listByObjectQuery, null, contentTypeId, dbObject.Id, revisionId);

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

			// Hook up a MultiSelect on the underlying fields:
			var listBeforeSettable = Events.FindByType(typeof(IHaveTags), "Settable", EventPlacement.Before);

			foreach (var listEvent in listBeforeSettable)
			{
				listEvent.AddEventListener((Context context, object[] args) =>
				{
					var field = args[0] as JsonField;

					if (field != null && field.Name == "Tags")
					{
						field.Module = "Admin/MultiSelect";
						field.Data["contentType"] = "tag";
						
						// Defer the set after the ID is available:
						field.AfterId = true;

						// On set, convert provided IDs into tag objects.
						field.OnSetValueUnTyped.AddEventListener(async (Context ctx, object[] valueArgs) =>
						{
							if (valueArgs == null || valueArgs.Length < 2)
							{
								return null;
							}

							// The value should be an array of ints.
							var value = valueArgs[0];

							// The object we're setting to will have an ID now because of the above defer:
							if (!(valueArgs[1] is DatabaseRow targetObject))
							{
								return null;
							}

							if (!(value is JArray idArray))
							{
								return null;
							}

							var ids = new List<int>();

							foreach (var token in idArray)
							{
								// id is..
								var id = token.Value<int?>();

								if (id.HasValue && id > 0)
								{
									ids.Add(id.Value);
								}
							}

							if (ids.Count == 0)
							{
								// Do nothing
								return null;
							}

							int revisionId = 0;

							if (targetObject is RevisionRow)
							{
								var revId = ((RevisionRow)targetObject).RevisionId;

								if (revId.HasValue)
								{
									revisionId = revId.Value;
								}
							}

							var contentTypeId = ContentTypes.GetId(targetObject.GetType());
							
							// Get all tag content entries for this host object:
							var existingEntries = await _tagContents.List(
								ctx,
								new Filter<TagContent>().Equals("ContentId", targetObject.Id).And().Equals("ContentTypeId", contentTypeId).And().Equals("RevisionId", revisionId)
							);

							// Identify ones being deleted, and ones being added, then update tag contents.
							Dictionary<int, TagContent> existingLookup = new Dictionary<int, TagContent>();

							foreach (var existingEntry in existingEntries)
							{
								existingLookup[existingEntry.TagId] = existingEntry;
							}

							var now = DateTime.UtcNow;

							Dictionary<int, bool> newSet = new Dictionary<int, bool>();

							foreach (var id in ids)
							{
								newSet[id] = true;

								if (!existingLookup.ContainsKey(id))
								{
									// Add it:
									await _tagContents.Create(ctx, new TagContent()
									{
										ContentId = targetObject.Id,
										ContentTypeId = contentTypeId,
										TagId = id,
										RevisionId = revisionId,
										CreatedUtc = now
									});
								}
							}

							// Delete any being removed:
							foreach (var existingEntry in existingEntries)
							{
								if (!newSet.ContainsKey(existingEntry.TagId))
								{
									// Delete this row:
									await _tagContents.Delete(ctx, existingEntry.Id);
								}
							}

							// Get the tags:
							return await List(ctx, new Filter<Tag>().EqualsSet("Id", ids));
						});

						
					}

					return args[0];
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
					filter.EqualsArg("ContentTypeId", 0).And().EqualsSet("ContentId", ids).And().Equals("RevisionId", 0);
					
					// Todo: The above blocks tags from loading on lists of revisions
					// However, such a list of revisions requires a special case of per-row querying.
					
					// Get all the content tags for these entities:
					var allContentTags = await _database.List(context, listContentQuery, filter, contentTypeId);

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
					var tags = await List(context, tagFilter);

					foreach (var tag in tags)
					{
						tagLookup[tag.Id] = tag;
					}

					// For each content->tag relation..
					foreach (var contentTag in allContentTags)
					{
						// Lookup content/ tag:
						var content = contentLookup[contentTag.ContentId];
						var tag = tagLookup[contentTag.TagId];

						// Add the tag to the content:
						content.Tags.Add(tag);
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
							.And().Equals("RevisionId", 0)
							.And().Equals("ContentTypeId", ContentTypes.GetId(filter.DefaultType))
							.And().EqualsSet("TagId", idArray);

					}

					return args[0];
				});
			}

		}
		
	}
    
}
