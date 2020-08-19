using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Notifications
{
	/// <summary>
	/// Handles notifications.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class NotificationService : AutoService<Notification>, INotificationService
    {

		private readonly Query<Notification> selectByContent;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NotificationService() : base(Events.Notification)
        {
			selectByContent = Query.Select<Notification>();
			selectByContent.Where().EqualsArg("ContentTypeId", 0).And().EqualsArg("ContentId", 1).And().EqualsArg("UserId", 2);
		}


		/// <summary>
		/// Send a notification. This will either create or update a notification if a similar notification exists.
		/// "Similar" means same contentId + type to the same user.
		/// </summary>
		public async Task<Notification> Send(Context context, Notification e)
		{

			// Attempt to get similar notif:
			var existing = await _database.Select(context, selectByContent, e.ContentTypeId, e.ContentId, e.UserId);

			if (existing != null)
			{
				if (existing.ViewedDateUtc == null)
				{
					return existing;
				}

				// Ping it:
				existing.ViewedDateUtc = null;
				existing.EditedUtc = System.DateTime.UtcNow;
				return await Update(context, existing);
			}

			// Otherwise, create it:
			return await Create(context, e);
		}

	}

}
