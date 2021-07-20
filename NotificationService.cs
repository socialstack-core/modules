using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System;
using Api.Startup;

namespace Api.Notifications
{
	/// <summary>
	/// Handles notifications.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class NotificationService : AutoService<Notification>
    {
		private ComposableChangeField viewedDateChanged;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public NotificationService() : base(Events.Notification)
        {
			viewedDateChanged = GetChangeField("ViewedDateUtc");
		}
		
		/// <summary>
		/// Mark all of a user's notifications as viewed.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="userId">User ID</param>
		/// <returns>The number cleared</returns>
		public async Task<int> MarkAllViewed(Context context, uint userId)
		{
			var all = await Where("UserId=? and ViewedDateUtc=?", DataOptions.IgnorePermissions).Bind(userId).Bind((DateTime?)null).ListAll(context);
			return await MarkChanged(all, context);
		}

		/// <summary>
		/// Mark the given set of notif id's as viewed.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="userId"></param>
		/// <param name="ids"></param>
		/// <returns></returns>
		public async Task<int> MarkSetViewed(Context context, uint userId, uint[] ids)
        {
			// With our ids, let's list the notifications based on the ids and the user id of the context.
			// We aren't ignoring permissions so we can verify the user is authed to handle the target notifications.
			var notifs = await Where("UserId=? and ViewedDateUtc=? and Id=[?]").Bind(userId).Bind((DateTime?)null).Bind(ids).ListAll(context);
			return await MarkChanged(notifs, context);
		}

		/// <summary>
		/// Used to mark a set of notifications as Viewed. 
		/// </summary>
		/// <param name="notifs"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public async Task<int> MarkChanged(List<Notification> notifs, Context context)
        {
			foreach (var notif in notifs)
			{
				await Update(context, notif, (Context c, Notification n) => {

					n.ViewedDateUtc = DateTime.UtcNow;
					n.MarkChanged(viewedDateChanged);
				});
			}

			return notifs.Count;
		}

		/// <summary>
		/// Send a notification.
		/// </summary>
		public async Task<Notification> Send(Context context, Notification e, DataOptions opts = DataOptions.Default)
		{
			// Create it:
			return await Create(context, e, opts);
		}

	}

}
