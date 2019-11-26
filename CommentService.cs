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


namespace Api.Comments
{

	/// <summary>
	/// Handles comments on other pieces of content.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CommentService : ICommentService
	{
        private IDatabaseService _database;
		
		private readonly Query<Comment> deleteQuery;
		private readonly Query<Comment> createQuery;
		private readonly Query<Comment> selectQuery;
		private readonly Query<Comment> updateQuery;
		private readonly Query<Comment> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CommentService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Comment>();
			createQuery = Query.Insert<Comment>();
			updateQuery = Query.Update<Comment>();
			selectQuery = Query.Select<Comment>();
			listQuery = Query.List<Comment>();
		}

		/// <summary>
		/// List a filtered set of blog posts.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Comment>> List(Context context, Filter<Comment> filter)
		{
			filter = await Events.CommentBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.CommentAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a Comment by its ID.
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
		/// Gets a single comment by its ID.
		/// </summary>
		public async Task<Comment> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new comment.
		/// </summary>
		public async Task<Comment> Create(Context context, Comment comment)
		{
			comment = await Events.CommentBeforeCreate.Dispatch(context, comment);

			// Note: The Id field is automatically updated by Run here.
			if (comment == null || !await _database.Run(createQuery, comment))
			{
				return null;
			}

			comment = await Events.CommentAfterCreate.Dispatch(context, comment);
			return comment;
		}

		/// <summary>
		/// Updates the given comment.
		/// </summary>
		public async Task<Comment> Update(Context context, Comment comment)
		{
			comment = await Events.CommentBeforeUpdate.Dispatch(context, comment);

			if (comment == null || !await _database.Run(updateQuery, comment, comment.Id))
			{
				return null;
			}

			comment = await Events.CommentAfterUpdate.Dispatch(context, comment);
			return comment;
		}
	}
    
}
