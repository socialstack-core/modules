using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Api.StoryAttachments
{
	/// <summary>
	/// Handles story attachments. These attachments are e.g. images attached to a feed story or a message in a chat channel.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class StoryAttachmentService : IStoryAttachmentService
    {
        private IDatabaseService _database;
		
		private readonly Query<StoryAttachment> deleteQuery;
		private readonly Query<StoryAttachment> createQuery;
		private readonly Query<StoryAttachment> selectQuery;
		private readonly Query<StoryAttachment> updateQuery;
		private readonly Query<StoryAttachment> listQuery;
		private readonly Query<StoryAttachment> listByObjectQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public StoryAttachmentService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<StoryAttachment>();
			createQuery = Query.Insert<StoryAttachment>();
			updateQuery = Query.Update<StoryAttachment>();
			selectQuery = Query.Select<StoryAttachment>();
			listQuery = Query.List<StoryAttachment>();
			listByObjectQuery = Query.List<StoryAttachment>();
			listByObjectQuery.Where().EqualsArg("ContentTypeId", 0).And().EqualsArg("ContentId", 1);


			// Load story attachments on Load/List events next. First, find all events for types that implement IHaveStoryAttachments:
			var loadEvents = Events.FindByType(typeof(IHaveStoryAttachments), "Load", EventPlacement.After);

			foreach (var loadEvent in loadEvents)
			{
				loadEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// The primary object is always the first arg.
					// It's both IHaveStoryAttachments and a DatabaseRow object:
					if (!(args[0] is IHaveStoryAttachments attachableObject))
					{
						// Due to the way how event chains work, the primary object can be null.
						// Safely ignore this.
						return null;
					}

					if (!(args[0] is DatabaseRow dbObject))
					{
						throw new System.Exception(
							"Story attachments are only available on DatabaseRow entities. " +
							"This type implements IHaveStoryAttachments but isn't in the database: " + args[0].GetType().Name
						);
					}

					// Get the content type ID for the primary object:
					var contentTypeId = ContentTypes.GetId(attachableObject.GetType());

					// List the stories now:
					var stories = await _database.List(context, listByObjectQuery, null, contentTypeId, dbObject.Id);
					attachableObject.Attachments = stories;
					return attachableObject;
				});

			}

			// Next the List events:
			var listEvents = Events.FindByType(typeof(IHaveStoryAttachments), "List", EventPlacement.After);

			foreach (var listEvent in listEvents)
			{
				listEvent.AddEventListener(async (Context context, object[] args) =>
				{
					// args[0] is a List of IHaveStoryAttachments implementors.
					if (!(args[0] is IList list))
					{
						// Can't handle this (or it was null anyway):
						return args[0];
					}

					// First we'll collect all their IDs so we can do a single bulk lookup.
					// ASSUMPTION: The list is not excessively long!
					// FUTURE IMPROVEMENT: Do this in chunks of ~50k entries.
					// (applies to at least categories/ tags/ attachments).

					var ids = new object[list.Count];
					var contentLookup = new Dictionary<int, IHaveStoryAttachments>();
					int contentTypeId = 0;

					for (var i = 0; i < ids.Length; i++)
					{
						// *must* be DatabaseRow entries:
						if (!(list[i] is DatabaseRow entry) || !(list[i] is IHaveStoryAttachments ihc))
						{
							throw new System.Exception("Story attachments are only available on DatabaseRow entities. " +
							"Failed on this type: " + list[i].GetType().Name);
						}

						// Get the content type ID for the primary object:
						if (contentTypeId == 0)
						{
							contentTypeId = ContentTypes.GetId(entry.GetType());
						}

						// Add to content lookup so we can map the tags to it shortly:
						contentLookup[entry.Id] = ihc;

						// Setup empty attachment array:
						ihc.Attachments = new List<StoryAttachment>();

						ids[i] = entry.Id;
					}

					if (ids.Length == 0)
					{
						// Nothing to do - just return here:
						return list;
					}

					// Create the filter and run the query now:
					var filter = new Filter<StoryAttachment>();
					filter.EqualsArg("ContentTypeId", 0).And().EqualsSet("ContentId", ids);

					// Get all the attachments for these entities:
					var allStoryAttachments = await _database.List(context, listQuery, filter, contentTypeId);

					// Add each one to the content:
					foreach (var attachment in allStoryAttachments)
					{
						if (!attachment.ContentId.HasValue)
						{
							continue;
						}

						if (contentLookup.TryGetValue(attachment.ContentId.Value, out IHaveStoryAttachments content)) {
							content.Attachments.Add(attachment);
						}
					}
					
					return list;
				});

			}
			
		}

		/// <summary>
		/// List a filtered set of story attachments.
		/// </summary>
		/// <returns></returns>
		public async Task<List<StoryAttachment>> List(Context context, Filter<StoryAttachment> filter)
		{
			filter = await Events.StoryAttachmentBeforeList.Dispatch(context, filter);
			var list = await _database.List(context, listQuery, filter);
			list = await Events.StoryAttachmentAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a story attachment by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> Delete(Context context, int id, bool deleteUploads = true)
        {
            // Delete the entry:
			await _database.Run(context, deleteQuery, id);
			
			if(deleteUploads){
			}
			
			// Ok!
			return true;
        }
        
		/// <summary>
		/// Gets a single story attachment by its ID.
		/// </summary>
		public async Task<StoryAttachment> Get(Context context, int id)
		{
			return await _database.Select(context, selectQuery, id);
		}
		
		/// <summary>
		/// Creates a new story attachment.
		/// </summary>
		public async Task<StoryAttachment> Create(Context context, StoryAttachment storyAttachment)
		{
			storyAttachment = await Events.StoryAttachmentBeforeCreate.Dispatch(context, storyAttachment);

			// Note: The Id field is automatically updated by Run here.
			if (storyAttachment == null || !await _database.Run(context, createQuery, storyAttachment)) {
				return null;
			}

			storyAttachment = await Events.StoryAttachmentAfterCreate.Dispatch(context, storyAttachment);
			return storyAttachment;
		}
		
		/// <summary>
		/// Updates the given story attachment.
		/// </summary>
		public async Task<StoryAttachment> Update(Context context, StoryAttachment storyAttachment)
		{
			storyAttachment = await Events.StoryAttachmentBeforeUpdate.Dispatch(context, storyAttachment);

			if (storyAttachment == null || !await _database.Run(context, updateQuery, storyAttachment, storyAttachment.Id))
			{
				return null;
			}

			storyAttachment = await Events.StoryAttachmentAfterUpdate.Dispatch(context, storyAttachment);
			return storyAttachment;
		}
		
    }
    
}
