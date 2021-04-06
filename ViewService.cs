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
	public partial class ViewService : AutoService<View>
	{

		private readonly Query<View> selectByContentTypeAndIdQuery = null;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ViewService() : base(Events.View)
        {
			// Because of IHaveViews, View must be nestable:
			MakeNestable();
			
			// Note: This module is incompatible with cached entities.

			// View total is automatic on load/ list endpoints
			// The ViewedAt state needs to be figured out however.
			
			selectByContentTypeAndIdQuery = Query.Select<View>();
			selectByContentTypeAndIdQuery.Where().EqualsArg("ContentId", 0).And().EqualsArg("ContentTypeId", 1).And().EqualsArg("UserId", 2);
			
			

			/*
			// First, find all events for types that implement IHaveViews:
			var loadEvents = Events.FindByType(typeof(IHaveViews), "Load", EventPlacement.After);

			var methodInfo = GetType().GetMethod("SetupForViews");

			foreach (var loadEvent in loadEvents)
			{
				// Get the actual type. We use this to avoid Revisions etc as we're not interested in those here:
				var contentType = ContentTypes.GetType(loadEvent.EntityName);

				if (contentType == null)
				{
					continue;
				}

				// Invoke setup for type:
				var setupType = methodInfo.MakeGenericMethod(new Type[] {
					contentType
				});

				setupType.Invoke(this, new object[] {
				});

			}
			*/

		}

		/// <summary>
		/// Sets a particular type with views handlers. Used via reflection.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void SetupForViews<T>() where T : Content<uint>, IHaveViews, new()
		{
			// Invoked by reflection
			var evtGroup = Events.GetGroup<T>();

			// Get the content type ID for the primary object:
			var contentTypeId = ContentTypes.GetId(typeof(T));

			evtGroup.AfterLoad.AddEventListener(async (Context context, T entity) =>
			{
				if (entity == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return entity;
				}

				// Get the view state for this user:
				var viewEntry = await _database.Select(context, selectByContentTypeAndIdQuery, entity.Id, contentTypeId, context.UserId);

				if (viewEntry == null)
				{
					// New viewer! Insert and increase view total by 1.

					// Use the regular insert so we can add events to the viewed state:
					viewEntry = new View();
					viewEntry.UserId = context.UserId;
					viewEntry.ContentId = entity.Id;
					viewEntry.ContentTypeId = contentTypeId;

					await Create(context, viewEntry);
				}
				else
				{
					// Update viewed time:
					await Update(context, viewEntry);
				}

				// Viewed it just now:
				entity.ViewedAtUtc = DateTime.UtcNow;

				return entity;
			});

			evtGroup.AfterUpdate.AddEventListener(async (Context context, T entity) =>
			{
				if (entity == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return entity;
				}

				// Get the view state for this user:
				var viewEntry = await _database.Select(context, selectByContentTypeAndIdQuery, entity.Id, contentTypeId, context.UserId);

				if (viewEntry == null)
				{
					// New viewer! Insert and increase view total by 1.

					// Use the regular insert so we can add events to the viewed state:
					viewEntry = new View();
					viewEntry.UserId = context.UserId;
					viewEntry.ContentId = entity.Id;
					viewEntry.ContentTypeId = contentTypeId;

					await Create(context, viewEntry);
				}
				else
				{
					// Update viewed time:
					await Update(context, viewEntry);
				}

				// Viewed it just now:
				entity.ViewedAtUtc = DateTime.UtcNow;

				return entity;
			});

			evtGroup.AfterCreate.AddEventListener(async (Context context, T entity) =>
			{
				if (entity == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				// Use the regular insert so we can add events to the viewed state:
				var viewEntry = new View();
				viewEntry.UserId = context.UserId;
				viewEntry.ContentId = entity.Id;
				viewEntry.ContentTypeId = contentTypeId;

				await Create(context, viewEntry);

				// Viewed it just now:
				entity.ViewedAtUtc = DateTime.UtcNow;

				return entity;
			});

			evtGroup.AfterList.AddEventListener(async (Context context, List<T> list) =>
			{
				if (list == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return list;
				}

				// First we'll collect all their IDs so we can do a single bulk lookup.
				// ASSUMPTION: The list is not excessively long!
				// FUTURE IMPROVEMENT: Do this in chunks of ~50k entries.
				// (applies to at least categories/ tags).

				var ids = new object[list.Count];
				var contentLookup = new Dictionary<uint, T>();
				
				for (var i = 0; i < ids.Length; i++)
				{
					var entry = list[i];

					// Add to content lookup so we can map the tags to it shortly:
					contentLookup[entry.Id] = entry;

					ids[i] = entry.Id;
				}

				if (ids.Length == 0)
				{
					// Nothing to do - just return here:
					return list;
				}

				// Create the filter and run the query now:
				var filter = new Filter<View>();
				filter.Equals("ContentTypeId", contentTypeId).And().EqualsSet("ContentId", ids).And().Equals("UserId", context.UserId);

				// Get all the content views for these entities:
				var allContentViews = await _database.List(context, listQuery, filter);

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

		/// <summary>
		/// Marks a content item of the given type as viewed.
		/// </summary>
		public async Task MarkViewed(Context context, int contentTypeId, uint id)
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
				
				await Create(context, viewEntry);
				
			}else{
				// Update viewed time:
				await Update(context, viewEntry);
			}
			
		}
	}

}
