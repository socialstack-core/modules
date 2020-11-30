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
			HuddleService huddleService = null;
			ActivityInstanceService activityInstanceService = null;
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
				else
				{
					huddle.Activity = null;
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
