using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.PasswordResetRequests
{
	/// <summary>
	/// Handles passwordResetRequests.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPasswordResetRequestService
    {
		/// <summary>
		/// Delete a passwordResetRequest by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a passwordResetRequest by its ID.
		/// </summary>
		Task<PasswordResetRequest> Get(Context context, int id);

		/// <summary>
		/// Create a passwordResetRequest.
		/// </summary>
		Task<PasswordResetRequest> Create(Context context, PasswordResetRequest e);

		/// <summary>
		/// Updates the database with the given passwordResetRequest data. It must have an ID set.
		/// </summary>
		Task<PasswordResetRequest> Update(Context context, PasswordResetRequest e);

		/// <summary>
		/// List a filtered set of passwordResetRequests.
		/// </summary>
		/// <returns></returns>
		Task<List<PasswordResetRequest>> List(Context context, Filter<PasswordResetRequest> filter);
		
		/// <summary>
		/// Gets a reset request by the given token.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		Task<PasswordResetRequest> Get(Context context, string token);
		
		/// <summary>
		/// True if given req has expired (or is already used).
		/// </summary>
		bool HasExpired(PasswordResetRequest req);
	}
}
