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
			var all = await Where("UserId=? and ViewedDateUtc=null", DataOptions.IgnorePermissions).Bind(userId).ListAll(context);
			
			// We go through them sequentially like this so websockets and caches are updated (amongst anything else that handlers want to do).
			foreach(var notif in all)
			{
				await Update(context, notif, (Context c, Notification n) => {
					
					n.ViewedDateUtc = DateTime.UtcNow;
					n.MarkChanged(viewedDateChanged);
				});
			}
			
			return all.Count;
			
		}

		/// <summary>
		/// Send a notification.
		/// </summary>
		public async Task<Notification> Send(Context context, Notification e)
		{
			// Create it:
			return await Create(context, e);
		}

	}

}
