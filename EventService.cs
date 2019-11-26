using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;

namespace Api.CalendarEvents
{
	/// <summary>
	/// Handles real world "calendarable" events.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class EventService : IEventService
    {
        private IDatabaseService _database;
		
		private readonly Query<Event> deleteQuery;
		private readonly Query<Event> createQuery;
		private readonly Query<Event> selectQuery;
		private readonly Query<Event> listQuery;
		private readonly Query<Event> updateQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public EventService(IDatabaseService database)
        {
            _database = database;
			
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Event>();
			
			createQuery = Query.Insert<Event>();
			updateQuery = Query.Update<Event>();
			selectQuery = Query.Select<Event>();
			listQuery = Query.List<Event>();
		}

		/// <summary>
		/// List a filtered set of events.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Event>> List(Context context, Filter<Event> filter)
		{
			filter = await Api.Eventing.Events.EventBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Api.Eventing.Events.EventAfterList.Dispatch(context, list);
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
		public async Task<Event> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Creates a new event.
		/// </summary>
		public async Task<Event> Create(Context context, Event evt)
		{
			evt = await Api.Eventing.Events.EventBeforeCreate.Dispatch(context, evt);

			// Note: The Id field is automatically updated by Run here.
			if (evt == null || !await _database.Run(createQuery, evt))
			{
				return null;
			}

			evt = await Api.Eventing.Events.EventAfterCreate.Dispatch(context, evt);
			return evt;
		}

		/// <summary>
		/// Updates the given event.
		/// </summary>
		public async Task<Event> Update(Context context, Event evt)
		{
			evt = await Api.Eventing.Events.EventBeforeUpdate.Dispatch(context, evt);

			if (evt == null || !await _database.Run(updateQuery, evt, evt.Id))
			{
				return null;
			}

			evt = await Api.Eventing.Events.EventAfterUpdate.Dispatch(context, evt);
			return evt;
		}
	}
    
}
