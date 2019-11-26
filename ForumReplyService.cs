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


namespace Api.ForumReplies
{
	/// <summary>
	/// Handles forum replies.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ForumReplyService : IForumReplyService
    {
        private IDatabaseService _database;
		
		private readonly Query<ForumReply> deleteQuery;
		private readonly Query<ForumReply> createQuery;
		private readonly Query<ForumReply> selectQuery;
		private readonly Query<ForumReply> updateQuery;
		private readonly Query<ForumReply> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumReplyService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<ForumReply>();
			createQuery = Query.Insert<ForumReply>();
			updateQuery = Query.Update<ForumReply>();
			selectQuery = Query.Select<ForumReply>();
			listQuery = Query.List<ForumReply>();
        }
		
        /// <summary>
        /// Deletes a forum reply by its ID.
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
		/// Gets a single reply by its ID.
		/// </summary>
		public async Task<ForumReply> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}
		
		/// <summary>
		/// List a filtered set of replies.
		/// </summary>
		/// <returns></returns>
		public async Task<List<ForumReply>> List(Context context, Filter<ForumReply> filter)
		{
			filter = await Events.ForumReplyBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.ForumReplyAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Creates a new forum reply.
		/// </summary>
		public async Task<ForumReply> Create(Context context, ForumReply reply)
		{
			reply = await Events.ForumReplyBeforeCreate.Dispatch(context, reply);

			// Note: The Id field is automatically updated by Run here.
			if (reply == null || !await _database.Run(createQuery, reply))
			{
				return null;
			}

			reply = await Events.ForumReplyAfterCreate.Dispatch(context, reply);
			return reply;
		}

		/// <summary>
		/// Updates the given forum reply.
		/// </summary>
		public async Task<ForumReply> Update(Context context, ForumReply reply)
		{
			reply = await Events.ForumReplyBeforeUpdate.Dispatch(context, reply);

			if (reply == null || !await _database.Run(updateQuery, reply, reply.Id))
			{
				return null;
			}

			reply = await Events.ForumReplyAfterUpdate.Dispatch(context, reply);
			return reply;
		}
	}

}
