using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System;

namespace Api.Notifications
{
	/// <summary>
	/// Handles notifications.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class NotificationService : AutoService<Notification>, INotificationService
    {

		private readonly Query<Notification> selectByContent;
		private readonly Query<Notification> selectByUserNotViewed;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NotificationService() : base(Events.Notification)
        {
			selectByContent = Query.Select<Notification>();
			selectByContent.Where().EqualsArg("ContentTypeId", 0).And().EqualsArg("ContentId", 1).And().EqualsArg("UserId", 2);
			
			selectByUserNotViewed = Query.Select<Notification>();
			selectByUserNotViewed.Where().EqualsArg("UserId", 0).And().Equals("ViewedDateUtc", null);
		}

		/// <summary>
		/// Mark all of a user's notifications as viewed.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="userId">User ID</param>
		/// <returns>The number cleared</returns>
		public async Task<int> MarkAllViewed(Context context, int userId)
		{
			// Attempt to get similar notif:
			var all = await _database.List(context, selectByUserNotViewed, null, userId);
			
			if(all == null)
			{
				return 0;
			}
			
			var now = DateTime.UtcNow;
			
			// We go through them sequentially like this so websockets and caches are updated (amongst anything else that handlers want to do).
			foreach(var notif in all)
			{
				notif.ViewedDateUtc = now;
				await Update(context, notif);
			}
			
			return all.Count;
			
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
