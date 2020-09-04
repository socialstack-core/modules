using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System.Linq;
using Api.Users;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using Api.ActivityInstances;

namespace Api.Huddles
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[EventListener]
	public partial class HuddleActivityEventHandler
    {
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddleActivityEventHandler()
        {
			IHuddleService huddleService = null;
			IActivityInstanceService activityInstanceService = null;
			Events.Huddle.AfterLoad.AddEventListener(async (Context context, Huddle huddle) =>
			{
				if (huddle == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				if (huddle.ActivityContentId != 0)
				{
					huddle.Activity = await Content.Get(context, huddle.ActivityContentTypeId, huddle.ActivityContentId);
				}
				
				return huddle;
			});
			
			Events.Huddle.AfterList.AddEventListener(async (Context context, List<Huddle> huddles) =>
			{
				if (huddles == null)
				{
					return null;
				}

				await Content.ApplyMixed(
					context,
					huddles,
					src => {
						var huddle = src as Huddle;
						return new ContentTypeAndId(huddle.ActivityContentTypeId, huddle.ActivityContentId);
					},
					(object src, object content) => {
						var huddle = src as Huddle;
						huddle.Activity = content;
					}
				);
				
				return huddles;
			});

			Events.Huddle.BeforeUpdate.AddEventListener(async (Context context, Huddle huddle) =>
			{
				// The purpose of this event listener is to see if the activity or the activity time (indicating an activity has been restarted)
				// has been been changed. If they have been changed, we need to create a new activity instance
				if(huddle == null)
                {
					return null;
                }

				if(huddleService == null)
                {
					huddleService = Services.Get<IHuddleService>();
                }

				var unupdatedHuddle = await huddleService.Get(context, huddle.Id);

				// Is there an activity contentId change occuring or activity start time change occuring?
				if(huddle.ActivityStartUtc == unupdatedHuddle.ActivityStartUtc && huddle.ActivityContentId == unupdatedHuddle.ActivityContentId)
                {
					// The activity is unchanged, just return the huddle here.
					return huddle;
                }

				// It could be the case that the values were nullified. If so, we should nullify the activityInstance as well.
				if(huddle.ActivityStartUtc == null && huddle.ActivityContentId == 0)
                {
					huddle.ActivityInstanceId = 0;
				}
                else
                {
					// The activity has been updated. Let's create a new activityInstance
					if (activityInstanceService == null)
					{
						activityInstanceService = Services.Get<IActivityInstanceService>();
					}

					var activityInstance = new ActivityInstance();
					activityInstance = await activityInstanceService.Create(context, activityInstance);

					huddle.ActivityInstanceId = activityInstance.Id;
				}

				return huddle;
			});

			Events.Huddle.AfterUpdate.AddEventListener(async (Context context, Huddle huddle) =>
			{
				if (huddle == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				if (huddle.ActivityContentId != 0)
				{
					huddle.Activity = await Content.Get(context, huddle.ActivityContentTypeId, huddle.ActivityContentId);
				}
				else
                {
					huddle.Activity = null;
                }

				return huddle;
			});

		}

	}
    
}
