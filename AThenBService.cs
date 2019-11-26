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


namespace Api.IfAThenB
{
	/// <summary>
	/// Handles a then b rules.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class AThenBService : IAThenBService
    {
        private IDatabaseService _database;
		
		private readonly Query<AThenB> deleteQuery;
		private readonly Query<AThenB> createQuery;
		private readonly Query<AThenB> selectQuery;
		private readonly Query<AThenB> updateQuery;
		private readonly Query<AThenB> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AThenBService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<AThenB>();
			createQuery = Query.Insert<AThenB>();
			updateQuery = Query.Update<AThenB>();
			selectQuery = Query.Select<AThenB>();
			listQuery = Query.List<AThenB>();
		}

		/// <summary>
		/// List a filtered set of a then b rules.
		/// </summary>
		/// <returns></returns>
		public async Task<List<AThenB>> List(Context context, Filter<AThenB> filter)
		{
			filter = await Events.AThenBBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.AThenBAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes an a then b rule by its ID.
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
		/// Gets a single a then b rule by its ID.
		/// </summary>
		public async Task<AThenB> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}
		
		/// <summary>
		/// Creates a new a then b rule.
		/// </summary>
		public async Task<AThenB> Create(Context context, AThenB athenb)
		{
			athenb = await Events.AThenBBeforeCreate.Dispatch(context, athenb);

			// Note: The Id field is automatically updated by Run here.
			if (athenb == null || !await _database.Run(createQuery, athenb)) {
				return null;
			}

			athenb = await Events.AThenBAfterCreate.Dispatch(context, athenb);
			return athenb;
		}
		
		/// <summary>
		/// Updates the given a then b rule.
		/// </summary>
		public async Task<AThenB> Update(Context context, AThenB athenb)
		{
			athenb = await Events.AThenBBeforeUpdate.Dispatch(context, athenb);

			if (athenb == null || !await _database.Run(updateQuery, athenb, athenb.Id))
			{
				return null;
			}

			athenb = await Events.AThenBAfterUpdate.Dispatch(context, athenb);
			return athenb;
		}
		
    }
    
}
