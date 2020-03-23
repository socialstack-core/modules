using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Api.FeedStories
{
	/// <summary>
	/// Handles feed stories (entries within the news feed).
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class FeedStoryService : IFeedStoryService
    {
        private IDatabaseService _database;
		
		private readonly Query<FeedStory> deleteQuery;
		private readonly Query<FeedStory> createQuery;
		private readonly Query<FeedStory> selectQuery;
		private readonly Query<FeedStory> updateQuery;
		private readonly Query<FeedStory> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public FeedStoryService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<FeedStory>();
			createQuery = Query.Insert<FeedStory>();
			updateQuery = Query.Update<FeedStory>();
			selectQuery = Query.Select<FeedStory>();
			listQuery = Query.List<FeedStory>();
		}

		/// <summary>
		/// List a filtered set of feed stories.
		/// </summary>
		/// <returns></returns>
		public async Task<List<FeedStory>> List(Context context, Filter<FeedStory> filter)
		{
			filter = await Events.FeedStoryBeforeList.Dispatch(context, filter);
			var list = await _database.List(context, listQuery, filter);
			list = await Events.FeedStoryAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a feed story by its ID.
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
		/// Gets a single feed story by its ID.
		/// </summary>
		public async Task<FeedStory> Get(Context context, int id)
		{
			return await _database.Select(context, selectQuery, id);
		}
		
		/// <summary>
		/// Creates a new feed story.
		/// </summary>
		public async Task<FeedStory> Create(Context context, FeedStory feedStory)
		{
			feedStory = await Events.FeedStoryBeforeCreate.Dispatch(context, feedStory);

			// Note: The Id field is automatically updated by Run here.
			if (feedStory == null || !await _database.Run(context, createQuery, feedStory)) {
				return null;
			}

			feedStory = await Events.FeedStoryAfterCreate.Dispatch(context, feedStory);
			return feedStory;
		}
		
		/// <summary>
		/// Updates the given feed story.
		/// </summary>
		public async Task<FeedStory> Update(Context context, FeedStory feedStory)
		{
			feedStory = await Events.FeedStoryBeforeUpdate.Dispatch(context, feedStory);

			if (feedStory == null || !await _database.Run(context, updateQuery, feedStory, feedStory.Id))
			{
				return null;
			}

			feedStory = await Events.FeedStoryAfterUpdate.Dispatch(context, feedStory);
			return feedStory;
		}
		
    }
    
}
