using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Reactions
{
	/// <summary>
	/// Handles reaction types - i.e. define a new type of reaction that can be used on content.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ReactionTypeService : IReactionTypeService
	{
        private IDatabaseService _database;
		
		private readonly Query<ReactionType> deleteQuery;
		private readonly Query<ReactionType> createQuery;
		private readonly Query<ReactionType> selectQuery;
		private readonly Query<ReactionType> updateQuery;
		private readonly Query<ReactionType> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ReactionTypeService(IDatabaseService database)
        {
            _database = database;

			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<ReactionType>();
			createQuery = Query.Insert<ReactionType>();
			updateQuery = Query.Update<ReactionType>();
			selectQuery = Query.Select<ReactionType>();
			listQuery = Query.List<ReactionType>();
        }
		
        /// <summary>
        /// Deletes a reaction type by its ID.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Delete(Context context, int id)
        {
            // Delete the entry:
			await _database.Run(deleteQuery, id);
			
			// Ok!
			return true;
        }

		/// <summary>
		/// List a filtered set of reaction type.
		/// </summary>
		/// <returns></returns>
		public async Task<List<ReactionType>> List(Context context, Filter<ReactionType> filter)
		{
			filter = await Events.ReactionTypeBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.ReactionTypeAfterList.Dispatch(context, list);
			return list;
		}
		
		/// <summary>
		/// Gets a single reaction type by its ID.
		/// </summary>
		public async Task<ReactionType> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new reaction.
		/// </summary>
		public async Task<ReactionType> Create(Context context, ReactionType reaction)
		{
			reaction = await Events.ReactionTypeBeforeCreate.Dispatch(context, reaction);

			// Note: The Id field is automatically updated by Run here.
			if (reaction == null || !await _database.Run(createQuery, reaction))
			{
				return null;
			}

			reaction = await Events.ReactionTypeAfterCreate.Dispatch(context, reaction);
			return reaction;
		}

		/// <summary>
		/// Updates the given reaction.
		/// </summary>
		public async Task<ReactionType> Update(Context context, ReactionType reaction)
		{
			reaction = await Events.ReactionTypeBeforeUpdate.Dispatch(context, reaction);

			if (reaction == null || !await _database.Run(updateQuery, reaction, reaction.Id))
			{
				return null;
			}

			reaction = await Events.ReactionTypeAfterUpdate.Dispatch(context, reaction);
			return reaction;
		}

	}

}
