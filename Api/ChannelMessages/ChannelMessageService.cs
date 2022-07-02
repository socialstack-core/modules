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


namespace Api.ChannelMessages
{
	/// <summary>
	/// Handles messages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ChannelMessageService : IChannelMessageService
    {
        private IDatabaseService _database;
		
		private readonly Query<ChannelMessage> deleteQuery;
		private readonly Query<ChannelMessage> createQuery;
		private readonly Query<ChannelMessage> selectQuery;
		private readonly Query<ChannelMessage> updateQuery;
		private readonly Query<ChannelMessage> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ChannelMessageService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<ChannelMessage>();
			createQuery = Query.Insert<ChannelMessage>();
			updateQuery = Query.Update<ChannelMessage>();
			selectQuery = Query.Select<ChannelMessage>();
			listQuery = Query.List<ChannelMessage>();
		}

		/// <summary>
		/// List a filtered set of messages.
		/// </summary>
		/// <returns></returns>
		public async Task<List<ChannelMessage>> List(Context context, Filter<ChannelMessage> filter)
		{
			filter = await Events.ChannelMessageBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.ChannelMessageAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes an message by its ID.
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
		/// Gets a single message by its ID.
		/// </summary>
		public async Task<ChannelMessage> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}
		
		/// <summary>
		/// Creates a new message.
		/// </summary>
		public async Task<ChannelMessage> Create(Context context, ChannelMessage message)
		{
			message = await Events.ChannelMessageBeforeCreate.Dispatch(context, message);

			// Note: The Id field is automatically updated by Run here.
			if (message == null || !await _database.Run(createQuery, message)) {
				return null;
			}

			message = await Events.ChannelMessageAfterCreate.Dispatch(context, message);
			return message;
		}
		
		/// <summary>
		/// Updates the given message.
		/// </summary>
		public async Task<ChannelMessage> Update(Context context, ChannelMessage message)
		{
			message = await Events.ChannelMessageBeforeUpdate.Dispatch(context, message);

			if (message == null || !await _database.Run(updateQuery, message, message.Id))
			{
				return null;
			}

			message = await Events.ChannelMessageAfterUpdate.Dispatch(context, message);
			return message;
		}
		
    }
    
}
