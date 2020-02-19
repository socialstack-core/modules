using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace Api.Categories
{
	/// <summary>
	/// Handles categories - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CategoryService : AutoService<Category>, ICategoryService
    {
		private readonly Query<CategoryContent> listByObjectQuery;
		private readonly Query<CategoryContent> listContentQuery;
		private readonly Query<Category> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CategoryService() : base(Events.Category)
		{
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			listContentQuery = Query.List<CategoryContent>();
			listByObjectQuery = Query.List<CategoryContent>();
			listByObjectQuery.Where().EqualsArg("ContentTypeId", 0).And().EqualsArg("ContentId", 1);

			// Load categories on Load/List events next. First, find all events for types that implement IHaveCategories:
			var loadEvents = Events.FindByType(typeof(IHaveCategories), "Load", EventPlacement.After);

			foreach (var loadEvent in loadEvents)
			{
				loadEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// The primary object is always the first arg.
					// It's both IHaveCategories and a DatabaseRow object:
					if (!(args[0] is IHaveCategories categorisableObject))
					{
						// Due to the way how event chains work, the primary object can be null.
						// Safely ignore this.
						return null;
					}

					if (!(args[0] is DatabaseRow dbObject))
					{
						throw new System.Exception(
							"Categories are only available on DatabaseRow entities. " +
							"This type implements IHaveCategories but isn't in the database: " + args[0].GetType().Name
						);
					}

					// Get the content type ID for the primary object:
					var contentTypeId = ContentTypes.GetId(categorisableObject.GetType());

					// List the categories now:
					var contentCategories = await _database.List(listByObjectQuery, null, contentTypeId, dbObject.Id);

					if (contentCategories == null || contentCategories.Count == 0)
					{
						// None found - just set an empty category array.
						categorisableObject.Categories = new List<Category>();
					}
					else if (contentCategories.Count == 1)
					{
						// Use a higher speed prepared statement:
						categorisableObject.Categories = new List<Category>(1)
						{
							await Get(context, contentCategories[0].CategoryId)
						};
					}
					else
					{
						// It's in multiple categories. Use a filtered list here.
						var filter = new Filter<Category>();

						var ids = new object[contentCategories.Count];
						for (var i = 0; i < ids.Length; i++)
						{
							ids[i] = contentCategories[i].CategoryId;
						}

						filter.EqualsSet("Id", ids);
						categorisableObject.Categories = await List(context, filter);
					}

					return categorisableObject;
				});

			}

			// Next the AfterList events to add categories to stuff:
			var listAfterEvents = Events.FindByType(typeof(IHaveCategories), "List", EventPlacement.After);

			foreach (var listEvent in listAfterEvents)
			{
				listEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// args[0] is a List of IHaveCategories implementors.
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
					var contentLookup = new Dictionary<int, IHaveCategories>();
					int contentTypeId = 0;

					for (var i = 0; i < ids.Length; i++)
					{
						// *must* be DatabaseRow entries:
						if (!(list[i] is DatabaseRow entry) || !(list[i] is IHaveCategories ihc))
						{
							throw new System.Exception("Categories are only available on DatabaseRow entities. " +
							"Failed on this type: " + list[i].GetType().Name);
						}

						// Get the content type ID for the primary object:
						if (contentTypeId == 0)
						{
							contentTypeId = ContentTypes.GetId(entry.GetType());
						}

						// Add to content lookup so we can map the categories to it shortly:
						contentLookup[entry.Id] = ihc;

						// Setup empty category array:
						ihc.Categories = new List<Category>();

						ids[i] = entry.Id;
					}

					if (ids.Length == 0)
					{
						// Nothing to do - just return here:
						return list;
					}

					// Create the filter and run the query now:
					var filter = new Filter<CategoryContent>();
					filter.EqualsArg("ContentTypeId", 0).And().EqualsSet("ContentId", ids);

					// Get all the content categories for these entities:
					var allContentCategories = await _database.List(listContentQuery, filter, contentTypeId);

					// Build fast category lookup:
					var categoryLookup = new Dictionary<int, Category>();

					// Get the unique set of category IDs so we can collect those categories. Shortly will reuse the same lookup dict.
					foreach (var contentCategory in allContentCategories)
					{
						categoryLookup[contentCategory.CategoryId] = null;
					}

					if (categoryLookup.Count == 0)
					{
						// Nothing to do - just return here:
						return list;
					}

					// Array of unique category IDs:
					var categoryIds = new object[categoryLookup.Count];
					var index = 0;

					foreach (var kvp in categoryLookup) {
						categoryIds[index++] = kvp.Key;
					}

					var catFilter = new Filter<Category>();
					catFilter.EqualsSet("Id", categoryIds);
					var categories = await List(context, catFilter);

					foreach (var category in categories)
					{
						categoryLookup[category.Id] = category;
					}

					// For each content->category relation..
					foreach (var contentCategory in allContentCategories)
					{
						// Lookup content/ category:
						var content = contentLookup[contentCategory.ContentId];
						var category = categoryLookup[contentCategory.CategoryId];

						// Add the category to the content:
						content.Categories.Add(category);
					}

					return list;
				});

			}

			// We'll handle where field "Categories" ourselves:
			Filter.DeclareCustomWhereField("Categories");

			// Next we'll add a handler for "Categories" filters on List endpoints.
			// These are the *List events (but unlike above, we're using the "NotSpecified" placement):
			var listOnEvents = Events.FindByType(typeof(IHaveCategories), "List", EventPlacement.NotSpecified);

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

					// If the filter contained a "Categories" key then we'll add a join restriction:
					var categorySet = where["Categories"] as JArray;

					if (categorySet != null)
					{
						// We've got categories that we need to filter by. For now this set must be integer category IDs.
						// We want to join the category content table 
						// on WhateverTypeTheFilterIsUsing.Id = CategoryContent.Id AND ContentTypeId = TheIDOfThatFilterType
						// AND CategoryId IN(categorySet)

						// Need an object[] first:
						var idArray = new object[categorySet.Count];

						for (var i = 0; i < idArray.Length; i++)
						{
							var categoryId = categorySet[i].Value<int>();
							idArray[i] = categoryId;
						}

						filter.Join<CategoryContent>("Id", "ContentId")
							.And().Equals("ContentTypeId", ContentTypes.GetId(filter.DefaultType))
							.And().EqualsSet("CategoryId", idArray);

					}

					return args[0];
				});
			}

		}

	}
    
}
