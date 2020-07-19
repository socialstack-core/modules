using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.PrivateChats
{
	/// <summary>
	/// Handles privateChatMessages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPrivateChatMessageService
    {
		/// <summary>
		/// Delete a privateChatMessage by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a privateChatMessage by its ID.
		/// </summary>
		Task<PrivateChatMessage> Get(Context context, int id);

		/// <summary>
		/// Create a privateChatMessage.
		/// </summary>
		Task<PrivateChatMessage> Create(Context context, PrivateChatMessage e);

		/// <summary>
		/// Updates the database with the given privateChatMessage data. It must have an ID set.
		/// </summary>
		Task<PrivateChatMessage> Update(Context context, PrivateChatMessage e);

		/// <summary>
		/// List a filtered set of privateChatMessages.
		/// </summary>
		/// <returns></returns>
		Task<List<PrivateChatMessage>> List(Context context, Filter<PrivateChatMessage> filter);

	}
}
