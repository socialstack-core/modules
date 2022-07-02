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


namespace Api.ChannelUsers
{
	/// <summary>
	/// Handles channel users.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ChannelUserService : IChannelUserService
    {
        private IDatabaseService _database;
		
		private readonly Query<ChannelUser> deleteQuery;
		private readonly Query<ChannelUser> createQuery;
		private readonly Query<ChannelUser> selectQuery;
		private readonly Query<ChannelUser> updateQuery;
		private readonly Query<ChannelUser> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ChannelUserService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<ChannelUser>();
			createQuery = Query.Insert<ChannelUser>();
			updateQuery = Query.Update<ChannelUser>();
			selectQuery = Query.Select<ChannelUser>();
			listQuery = Query.List<ChannelUser>();
		}

		/// <summary>
		/// List a filtered set of channel users.
		/// </summary>
		/// <returns></returns>
		public async Task<List<ChannelUser>> List(Context context, Filter<ChannelUser> filter)
		{
			filter = await Events.ChannelUserBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.ChannelUserAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a channel user by its ID.
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
		/// Gets a single channel user by its ID.
		/// </summary>
		public async Task<ChannelUser> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}
		
		/// <summary>
		/// Creates a new channel user.
		/// </summary>
		public async Task<ChannelUser> Create(Context context, ChannelUser channelUser)
		{
			channelUser = await Events.ChannelUserBeforeCreate.Dispatch(context, channelUser);

			// Note: The Id field is automatically updated by Run here.
			if (channelUser == null || !await _database.Run(createQuery, channelUser)) {
				return null;
			}

			channelUser = await Events.ChannelUserAfterCreate.Dispatch(context, channelUser);
			return channelUser;
		}
		
		/// <summary>
		/// Updates the given channel user.
		/// </summary>
		public async Task<ChannelUser> Update(Context context, ChannelUser channelUser)
		{
			channelUser = await Events.ChannelUserBeforeUpdate.Dispatch(context, channelUser);

			if (channelUser == null || !await _database.Run(updateQuery, channelUser, channelUser.Id))
			{
				return null;
			}

			channelUser = await Events.ChannelUserAfterUpdate.Dispatch(context, channelUser);
			return channelUser;
		}
		
    }
    
}
