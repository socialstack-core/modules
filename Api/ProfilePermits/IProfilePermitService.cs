using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.ProfilePermits
{
	/// <summary>
	/// Handles users setting who are permitted to view their private profile.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IProfilePermitService
	{
		/// <summary>
		/// Deletes a profile permit by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Gets a single profile permit by its ID.
		/// </summary>
		Task<ProfilePermit> Get(Context context, int id);

		/// <summary>
		/// Creates a new profile permit.
		/// </summary>
		Task<ProfilePermit> Create(Context context, ProfilePermit follower);

		/// <summary>
		/// Updates the given profile permit.
		/// </summary>
		Task<ProfilePermit> Update(Context context, ProfilePermit follower);

		/// <summary>
		/// List a filtered set of profile permits.
		/// </summary>
		/// <returns></returns>
		Task<List<ProfilePermit>> List(Context context, Filter<ProfilePermit> filter);
	}
}
