using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.StaffMembers
{
	/// <summary>
	/// Handles staff members who aren't users - used by e.g. staff lists.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IStaffMemberService
    {
		/// <summary>
		/// Delete a person by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a StaffMember by its ID.
		/// </summary>
		Task<StaffMember> Get(Context context, int id);

		/// <summary>
		/// Create a new StaffMember.
		/// </summary>
		Task<StaffMember> Create(Context context, StaffMember prj);

		/// <summary>
		/// Updates the database with the given StaffMember data. It must have an ID set.
		/// </summary>
		Task<StaffMember> Update(Context context, StaffMember prj);

		/// <summary>
		/// List a filtered set of StaffMembers.
		/// </summary>
		/// <returns></returns>
		Task<List<StaffMember>> List(Context context, Filter<StaffMember> filter);

	}
}
