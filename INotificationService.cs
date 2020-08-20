using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Notifications
{
	/// <summary>
	/// Handles notifications.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface INotificationService
    {
		/// <summary>
		/// Delete a notification by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Mark all of a user's notifications as viewed.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="userId">User ID</param>
		/// <returns>The number cleared</returns>
		Task<int> MarkAllViewed(Context context, int userId);

		/// <summary>
		/// Get a notification by its ID.
		/// </summary>
		Task<Notification> Get(Context context, int id);

		/// <summary>
		/// Send a notification. This will either create or update a notification if a similar notification exists.
		/// "Similar" means same contentId + type to the same user.
		/// </summary>
		Task<Notification> Send(Context context, Notification e);

		/// <summary>
		/// Create a notification.
		/// </summary>
		Task<Notification> Create(Context context, Notification e);

		/// <summary>
		/// Updates the database with the given notification data. It must have an ID set.
		/// </summary>
		Task<Notification> Update(Context context, Notification e);

		/// <summary>
		/// List a filtered set of notifications.
		/// </summary>
		/// <returns></returns>
		Task<List<Notification>> List(Context context, Filter<Notification> filter);

	}
}
