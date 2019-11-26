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


namespace Api.Channels
{
	/// <summary>
	/// Handles channels.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ChannelService : IChannelService
    {
        private IDatabaseService _database;
		
		private readonly Query<Channel> deleteQuery;
		private readonly Query<Channel> createQuery;
		private readonly Query<Channel> selectQuery;
		private readonly Query<Channel> updateQuery;
		private readonly Query<Channel> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ChannelService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Channel>();
			createQuery = Query.Insert<Channel>();
			updateQuery = Query.Update<Channel>();
			selectQuery = Query.Select<Channel>();
			listQuery = Query.List<Channel>();
		}

		/// <summary>
		/// List a filtered set of channels.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Channel>> List(Context context, Filter<Channel> filter)
		{
			filter = await Events.ChannelBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.ChannelAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a channel by its ID.
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
		/// Gets a single channel by its ID.
		/// </summary>
		public async Task<Channel> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}
		
		/// <summary>
		/// Creates a new channel.
		/// </summary>
		public async Task<Channel> Create(Context context, Channel channel)
		{
			channel = await Events.ChannelBeforeCreate.Dispatch(context, channel);

			// Note: The Id field is automatically updated by Run here.
			if (channel == null || !await _database.Run(createQuery, channel)) {
				return null;
			}

			channel = await Events.ChannelAfterCreate.Dispatch(context, channel);
			return channel;
		}
		
		/// <summary>
		/// Updates the given channel.
		/// </summary>
		public async Task<Channel> Update(Context context, Channel channel)
		{
			channel = await Events.ChannelBeforeUpdate.Dispatch(context, channel);

			if (channel == null || !await _database.Run(updateQuery, channel, channel.Id))
			{
				return null;
			}

			channel = await Events.ChannelAfterUpdate.Dispatch(context, channel);
			return channel;
		}
		
    }
    
}
