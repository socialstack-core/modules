using Api.Contexts;
using Api.Permissions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Huddles
{
	/// <summary>
	/// Handles huddleServers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IHuddleServerService
    {
		/// <summary>
		/// Delete a huddleServer by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a huddleServer by its ID.
		/// </summary>
		Task<HuddleServer> Get(Context context, int id);

		/// <summary>
		/// Create a huddleServer.
		/// </summary>
		Task<HuddleServer> Create(Context context, HuddleServer e);

		/// <summary>
		/// Updates the database with the given huddleServer data. It must have an ID set.
		/// </summary>
		Task<HuddleServer> Update(Context context, HuddleServer e);

		/// <summary>
		/// List a filtered set of huddleServers.
		/// </summary>
		/// <returns></returns>
		Task<List<HuddleServer>> List(Context context, Filter<HuddleServer> filter);

		/// <summary>
		/// Allocates a huddle server for the given time range and load factor.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="startTimeUtc"></param>
		/// <param name="projectedEndTimeUtc"></param>
		/// <param name="loadFactor"></param>
		/// <returns></returns>
		Task<HuddleServer> Allocate(Context context, DateTime startTimeUtc, DateTime projectedEndTimeUtc, int loadFactor);

	}
}
