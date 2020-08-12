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
		/// Get a notification by its ID.
		/// </summary>
		Task<Notification> Get(Context context, int id);

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
