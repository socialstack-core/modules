using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Galleries
{
	/// <summary>
	/// Handles creations of galleries - containers for image uploads.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class GalleryService : IGalleryService
    {
        private IDatabaseService _database;
		
		private readonly Query<Gallery> deleteQuery;
		private readonly Query<Gallery> createQuery;
		private readonly Query<Gallery> selectQuery;
		private readonly Query<Gallery> listQuery;
		private readonly Query<Gallery> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public GalleryService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Gallery>();
			createQuery = Query.Insert<Gallery>();
			updateQuery = Query.Update<Gallery>();
			selectQuery = Query.Select<Gallery>();
			listQuery = Query.List<Gallery>();
		}
		
        /// <summary>
        /// Deletes a Gallery by its ID.
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
		/// List a filtered set of galleries.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Gallery>> List(Context context, Filter<Gallery> filter)
		{
			filter = await Events.GalleryBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.GalleryAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Gets a single Gallery by its ID.
		/// </summary>
		public async Task<Gallery> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new gallery.
		/// </summary>
		public async Task<Gallery> Create(Context context, Gallery gallery)
		{
			gallery = await Events.GalleryBeforeCreate.Dispatch(context, gallery);

			// Note: The Id field is automatically updated by Run here.
			if (gallery == null || !await _database.Run(createQuery, gallery))
			{
				return null;
			}

			gallery = await Events.GalleryAfterCreate.Dispatch(context, gallery);
			return gallery;
		}

		/// <summary>
		/// Updates the given gallery.
		/// </summary>
		public async Task<Gallery> Update(Context context, Gallery gallery)
		{
			gallery = await Events.GalleryBeforeUpdate.Dispatch(context, gallery);

			if (gallery == null || !await _database.Run(updateQuery, gallery, gallery.Id))
			{
				return null;
			}

			gallery = await Events.GalleryAfterUpdate.Dispatch(context, gallery);
			return gallery;
		}
	}
    
}
