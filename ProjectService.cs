using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Projects
{
	/// <summary>
	/// Handles projects.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ProjectService : IProjectService
    {
        private IDatabaseService _database;
		
		private readonly Query<Project> deleteQuery;
		private readonly Query<Project> createQuery;
		private readonly Query<Project> selectQuery;
		private readonly Query<Project> listQuery;
		private readonly Query<Project> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProjectService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Project>();
			
			createQuery = Query.Insert<Project>();
			updateQuery = Query.Update<Project>();
			selectQuery = Query.Select<Project>();
			listQuery = Query.List<Project>();
		}

		/// <summary>
		/// List a filtered set of projects.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Project>> List(Context context, Filter<Project> filter)
		{
			filter = await Events.ProjectBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.ProjectAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes a Event by its ID.
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
		/// Gets a single Event by its ID.
		/// </summary>
		public async Task<Project> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new project.
		/// </summary>
		public async Task<Project> Create(Context context, Project prj)
		{
			prj = await Events.ProjectBeforeCreate.Dispatch(context, prj);

			// Note: The Id field is automatically updated by Run here.
			if (prj == null || !await _database.Run(createQuery, prj))
			{
				return null;
			}

			prj = await Events.ProjectAfterCreate.Dispatch(context, prj);
			return prj;
		}

		/// <summary>
		/// Updates the given project.
		/// </summary>
		public async Task<Project> Update(Context context, Project prj)
		{
			prj = await Events.ProjectBeforeUpdate.Dispatch(context, prj);

			if (prj == null || !await _database.Run(updateQuery, prj, prj.Id))
			{
				return null;
			}

			prj = await Events.ProjectAfterUpdate.Dispatch(context, prj);
			return prj;
		}
	}
    
}
