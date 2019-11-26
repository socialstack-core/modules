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


namespace Api.Followers
{
	/// <summary>
	/// Handles followers (subscribers).
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class FollowerService : IFollowerService
    {
        private IDatabaseService _database;
		
		private readonly Query<Follower> deleteQuery;
		private readonly Query<Follower> createQuery;
		private readonly Query<Follower> selectQuery;
		private readonly Query<Follower> updateQuery;
		private readonly Query<Follower> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public FollowerService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Follower>();
			createQuery = Query.Insert<Follower>();
			updateQuery = Query.Update<Follower>();
			selectQuery = Query.Select<Follower>();
			listQuery = Query.List<Follower>();
		}

		/// <summary>
		/// List a filtered set of followers.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Follower>> List(Context context, Filter<Follower> filter)
		{
			filter = await Events.FollowerBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.FollowerAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a follower by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> Delete(Context context, int id, bool deleteUploads = true)
        {
            // Delete the entry:
			await _database.Run(deleteQuery, id);
			
			if(deleteUploads){
			}
			
			// Ok!
			return true;
        }
        
		/// <summary>
		/// Gets a single follower by its ID.
		/// </summary>
		public async Task<Follower> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}
		
		/// <summary>
		/// Creates a new follower.
		/// </summary>
		public async Task<Follower> Create(Context context, Follower follower)
		{
			follower = await Events.FollowerBeforeCreate.Dispatch(context, follower);

			// Note: The Id field is automatically updated by Run here.
			if (follower == null || !await _database.Run(createQuery, follower)) {
				return null;
			}

			follower = await Events.FollowerAfterCreate.Dispatch(context, follower);
			return follower;
		}
		
		/// <summary>
		/// Updates the given follower.
		/// </summary>
		public async Task<Follower> Update(Context context, Follower follower)
		{
			follower = await Events.FollowerBeforeUpdate.Dispatch(context, follower);

			if (follower == null || !await _database.Run(updateQuery, follower, follower.Id))
			{
				return null;
			}

			follower = await Events.FollowerAfterUpdate.Dispatch(context, follower);
			return follower;
		}
		
    }
    
}
