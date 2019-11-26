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


namespace Api.Connections
{
	/// <summary>
	/// Handles connections (subscribers).
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ConnectionService : IConnectionService
    {
        private IDatabaseService _database;
		
		private readonly Query<Connection> deleteQuery;
		private readonly Query<Connection> createQuery;
		private readonly Query<Connection> selectQuery;
		private readonly Query<Connection> updateQuery;
		private readonly Query<Connection> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ConnectionService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Connection>();
			createQuery = Query.Insert<Connection>();
			updateQuery = Query.Update<Connection>();
			selectQuery = Query.Select<Connection>();
			listQuery = Query.List<Connection>();
		}

		/// <summary>
		/// List a filtered set of connections.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Connection>> List(Context context, Filter<Connection> filter)
		{
			filter = await Events.ConnectionBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.ConnectionAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a connection by its ID.
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
		/// Gets a single connection by its ID.
		/// </summary>
		public async Task<Connection> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}
		
		/// <summary>
		/// Creates a new connection.
		/// </summary>
		public async Task<Connection> Create(Context context, Connection connection)
		{
			connection = await Events.ConnectionBeforeCreate.Dispatch(context, connection);

			// Note: The Id field is automatically updated by Run here.
			if (connection == null || !await _database.Run(createQuery, connection)) {
				return null;
			}

			connection = await Events.ConnectionAfterCreate.Dispatch(context, connection);
			return connection;
		}
		
		/// <summary>
		/// Updates the given connection.
		/// </summary>
		public async Task<Connection> Update(Context context, Connection connection)
		{
			connection = await Events.ConnectionBeforeUpdate.Dispatch(context, connection);

			if (connection == null || !await _database.Run(updateQuery, connection))
			{
				return null;
			}

			connection = await Events.ConnectionAfterUpdate.Dispatch(context, connection);
			return connection;
		}
		
    }
    
}
