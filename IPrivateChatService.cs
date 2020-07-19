using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.PrivateChats
{
	/// <summary>
	/// Handles privateChats.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPrivateChatService
    {
		/// <summary>
		/// Delete a privateChat by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a privateChat by its ID.
		/// </summary>
		Task<PrivateChat> Get(Context context, int id);

		/// <summary>
		/// Create a privateChat.
		/// </summary>
		Task<PrivateChat> Create(Context context, PrivateChat e);

		/// <summary>
		/// Updates the database with the given privateChat data. It must have an ID set.
		/// </summary>
		Task<PrivateChat> Update(Context context, PrivateChat e);

		/// <summary>
		/// List a filtered set of privateChats.
		/// </summary>
		/// <returns></returns>
		Task<List<PrivateChat>> List(Context context, Filter<PrivateChat> filter);

	}
}
