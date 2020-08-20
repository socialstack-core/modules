using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Presence
{
	/// <summary>
	/// Handles presenceRecords.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPresenceRecordService
    {
		/// <summary>
		/// Delete a presenceRecord by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a presenceRecord by its ID.
		/// </summary>
		Task<PresenceRecord> Get(Context context, int id);

		/// <summary>
		/// Create a presenceRecord.
		/// </summary>
		Task<PresenceRecord> Create(Context context, PresenceRecord e);

		/// <summary>
		/// Updates the database with the given presenceRecord data. It must have an ID set.
		/// </summary>
		Task<PresenceRecord> Update(Context context, PresenceRecord e);

		/// <summary>
		/// List a filtered set of presenceRecords.
		/// </summary>
		/// <returns></returns>
		Task<List<PresenceRecord>> List(Context context, Filter<PresenceRecord> filter);

	}
}
