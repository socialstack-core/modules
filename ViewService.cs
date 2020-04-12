using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Views
{
	/// <summary>
	/// Handles views
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ViewService : AutoService<View>, IViewService
	{
		
		private readonly Query<View> selectByContentTypeAndIdQuery;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ViewService() : base(Events.View)
        {
			// Because of IHaveViews, View must be nestable:
			MakeNestable();
			
			// View total is automatic on load/ list endpoints
			// The ViewedAt state needs to be figured out however.
			
			selectByContentTypeAndIdQuery = Query.Select<View>();
			selectByContentTypeAndIdQuery.Where().EqualsArg("ContentId", 0).And().EqualsArg("ContentTypeId", 1).And().EqualsArg("UserId", 2);
			
			// First, find all events for types that implement IHaveViews:
			var loadEvents = Events.FindByType(typeof(IHaveViews), "Load", EventPlacement.After);

			foreach (var loadEvent in loadEvents)
			{
				loadEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// The primary object is always the first arg.
					// It's both IHaveViews and a DatabaseRow object:
					if (!(args[0] is IHaveViews viewableObject))
					{
						// Due to the way how event chains work, the primary object can be null.
						// Safely ignore this.
						return null;
					}

					if (!(args[0] is DatabaseRow dbObject))
					{
						throw new System.Exception(
							"Views are only available on DatabaseRow entities. " +
							"This type implements IHaveViews but isn't in the database: " + args[0].GetType().Name
						);
					}

					// Get the content type ID for the primary object:
					var contentTypeId = ContentTypes.GetId(viewableObject.GetType());
					
					// Get the view state for this user:
					var viewEntry = await _database.Select(context, selectByContentTypeAndIdQuery, dbObject.Id, contentTypeId, context.UserId);
					
					if(viewEntry == null){
						// New viewer! Insert and increase view total by 1.
						
						// Use the regular insert so we can add events to the viewed state:
						viewEntry = new View();
						viewEntry.UserId = context.UserId;
						viewEntry.ContentId = dbObject.Id;
						viewEntry.ContentTypeId = contentTypeId;
						
						await Create(context, viewEntry, null);
						
					}else{
						// Update viewed time:
						await Update(context, viewEntry);
					}
					
					// Viewed it just now:
					viewableObject.ViewedAtUtc = DateTime.UtcNow;
					
					return viewableObject;
				});

			}

			// Next, update events. The person who updates the entity must also update their view time.
			var updateEvents = Events.FindByType(typeof(IHaveViews), "Update", EventPlacement.After);

			foreach (var updateEvent in updateEvents)
			{
				updateEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// The primary object is always the first arg.
					// It's both IHaveViews and a DatabaseRow object:
					if (!(args[0] is IHaveViews viewableObject))
					{
						// Due to the way how event chains work, the primary object can be null.
						// Safely ignore this.
						return null;
					}

					if (!(args[0] is DatabaseRow dbObject))
					{
						throw new System.Exception(
							"Views are only available on DatabaseRow entities. " +
							"This type implements IHaveViews but isn't in the database: " + args[0].GetType().Name
						);
					}

					// Get the content type ID for the primary object:
					var contentTypeId = ContentTypes.GetId(viewableObject.GetType());

					// Get the view state for this user:
					var viewEntry = await _database.Select(context, selectByContentTypeAndIdQuery, dbObject.Id, contentTypeId, context.UserId);

					if (viewEntry == null)
					{
						// New viewer! Insert and increase view total by 1.

						// Use the regular insert so we can add events to the viewed state:
						viewEntry = new View();
						viewEntry.UserId = context.UserId;
						viewEntry.ContentId = dbObject.Id;
						viewEntry.ContentTypeId = contentTypeId;

						await Create(context, viewEntry, null);
					}
					else
					{
						// Update viewed time:
						await Update(context, viewEntry);
					}

					// Viewed it just now:
					viewableObject.ViewedAtUtc = DateTime.UtcNow;

					return viewableObject;
				});

			}

			// Next, create events. The person who creates the entity must also update their view time.
			var createEvents = Events.FindByType(typeof(IHaveViews), "Create", EventPlacement.After);

			foreach (var createEvent in createEvents)
			{
				createEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// The primary object is always the first arg.
					// It's both IHaveViews and a DatabaseRow object:
					if (!(args[0] is IHaveViews viewableObject))
					{
						// Due to the way how event chains work, the primary object can be null.
						// Safely ignore this.
						return null;
					}

					if (!(args[0] is DatabaseRow dbObject))
					{
						throw new System.Exception(
							"Views are only available on DatabaseRow entities. " +
							"This type implements IHaveViews but isn't in the database: " + args[0].GetType().Name
						);
					}

					// Get the content type ID for the primary object:
					var contentTypeId = ContentTypes.GetId(viewableObject.GetType());
						
					// Use the regular insert so we can add events to the viewed state:
					var viewEntry = new View();
					viewEntry.UserId = context.UserId;
					viewEntry.ContentId = dbObject.Id;
					viewEntry.ContentTypeId = contentTypeId;

					await Create(context, viewEntry, null);
					
					// Viewed it just now:
					viewableObject.ViewedAtUtc = DateTime.UtcNow;

					return viewableObject;
				});

			}

			// Next the List events. These just need to load the viewed state.
			var listEvents = Events.FindByType(typeof(IHaveViews), "List", EventPlacement.After);

			foreach (var listEvent in listEvents)
			{
				listEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// args[0] is a List of IHaveViews implementors.
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
					var contentLookup = new Dictionary<int, IHaveViews>();
					int contentTypeId = 0;

					for (var i = 0; i < ids.Length; i++)
					{
						// *must* be DatabaseRow entries:
						if (!(list[i] is DatabaseRow entry) || !(list[i] is IHaveViews ihc))
						{
							throw new System.Exception("Views are only available on DatabaseRow entities. " +
							"Failed on this type: " + list[i].GetType().Name);
						}

						// Get the content type ID for the primary object:
						if (contentTypeId == 0)
						{
							contentTypeId = ContentTypes.GetId(entry.GetType());
						}

						// Add to content lookup so we can map the tags to it shortly:
						contentLookup[entry.Id] = ihc;
						
						ids[i] = entry.Id;
					}
					
					if (ids.Length == 0)
					{
						// Nothing to do - just return here:
						return list;
					}
					
					// Create the filter and run the query now:
					var filter = new Filter<View>();
					filter.EqualsArg("ContentTypeId", 0).And().EqualsSet("ContentId", ids).And().EqualsArg("UserId", 1);
					
					// Get all the content views for these entities:
					var allContentViews = await _database.List(context, listQuery, filter, contentTypeId, context.UserId);
					
					// For each content->view relation..
					foreach (var contentView in allContentViews)
					{
						// Lookup content/ category:
						var content = contentLookup[contentView.ContentId];
						
						// Set the viewed date:
						content.ViewedAtUtc = contentView.EditedUtc;
					}
					
					return list;
				});

			}
		}
		
		/// <summary>
		/// Marks a content item of the given type as viewed.
		/// </summary>
		public async Task MarkViewed(Context context, int contentTypeId, int id)
		{
			
			// Get the view state for this user:
			var viewEntry = await _database.Select(context, selectByContentTypeAndIdQuery, id, contentTypeId, context.UserId);
			
			if(viewEntry == null){
				// New viewer! Insert and increase view total by 1.
				
				// Use the regular insert so we can add events to the viewed state:
				viewEntry = new View();
				viewEntry.UserId = context.UserId;
				viewEntry.ContentId = id;
				viewEntry.ContentTypeId = contentTypeId;
				
				await Create(context, viewEntry, null);
				
			}else{
				// Update viewed time:
				await Update(context, viewEntry);
			}
			
		}
	}

}
