using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Pages
{
	/// <summary>
	/// Handles pages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PageService : IPageService
    {
        private IDatabaseService _database;
		
		private readonly Query<Page> deleteQuery;
		private readonly Query<Page> createQuery;
		private readonly Query<Page> selectQuery;
		private readonly Query<Page> listQuery;
		private readonly Query<Page> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PageService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Page>();
			createQuery = Query.Insert<Page>();
			updateQuery = Query.Update<Page>();
			selectQuery = Query.Select<Page>();
			listQuery = Query.List<Page>();

			Task.Run(async () => {

				var filter = new Filter();
				filter.PageSize = 1;

				var list = await _database.List(listQuery, filter);

				if (list.Count == 0)
				{
					// No pages in the database - let's install the hello world page now:
					await Create(new Context(), new Page() {

						Url = "/",
						BodyJson = "{\r\n\"content\": \"Welcome to your new SocialStack instance. This text comes from the pages table in your database in a format called canvas JSON - you can read more about this format in the documentation. \"\r\n}"

					});
				}

			});
			
		}
		
        /// <summary>
        /// Deletes a page by its ID.
		/// Optionally includes uploaded content refs in there too.
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
		/// List a filtered set of pages.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Page>> List(Context context, Filter<Page> filter)
		{
			filter = await Events.PageBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.PageAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Gets a single page by its ID.
		/// </summary>
		public async Task<Page> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new page.
		/// </summary>
		public async Task<Page> Create(Context context, Page page)
		{
			page = await Events.PageBeforeCreate.Dispatch(context, page);

			// Note: The Id field is automatically updated by Run here.
			if (page == null || !await _database.Run(createQuery, page))
			{
				return null;
			}

			page = await Events.PageAfterCreate.Dispatch(context, page);
			return page;
		}

		/// <summary>
		/// Updates the given nav menu.
		/// </summary>
		public async Task<Page> Update(Context context, Page page)
		{
			page = await Events.PageBeforeUpdate.Dispatch(context, page);

			if (page == null || !await _database.Run(updateQuery, page, page.Id))
			{
				return null;
			}

			page = await Events.PageAfterUpdate.Dispatch(context, page);
			return page;
		}

	}
    
}
