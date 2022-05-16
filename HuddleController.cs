using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Contexts;
using Api.Startup;
using Api.PubQuizzes;
using Api.ActivityInstances;

namespace Api.Huddles
{
    /// <summary>
    /// Huddle controller extensions
    /// </summary>
    public partial class HuddleController
    {
        int pubQuizsContentTypeId = ContentTypes.GetId(typeof(PubQuiz));
        PubQuizQuestionService pubQuizQuestionService = null;
        ActivityInstanceService activityInstanceService = null;
        int questionDuration = 15;
 


        /// <summary>
        /// GET /v1/huddle/:id/startactivity/:activityContentTypeName/:activityContentId
        /// E.g. /v1/huddle/14/startactivity/PubQuiz/1
        /// Starts an activity
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id}/startactivity/{activityContentTypeName}/{activityContentId}")]
        public virtual async Task<object> StartActivity([FromRoute] uint id, [FromRoute] string activityContentTypeName, [FromRoute] uint activityContentId)
        {
            // Only possible if you can load a meeting, so we use the load perm here:
            var context = await Request.GetContext();
            var result = await _service.Get(context, id);

            if (result == null)
            {
                return null;
            }
            
            int contentTypeId = ContentTypes.GetId(activityContentTypeName);

            int? durationTicks = null;

                if(pubQuizQuestionService == null)
                {
                    pubQuizQuestionService = Services.Get<PubQuizQuestionService>();
                }

                // it is a pubquiz, let's get its questions.
                var questions = await pubQuizQuestionService.Where("PubQuizId=?", DataOptions.IgnorePermissions).Bind(activityContentId).ListAll(context);
                durationTicks = (1000 * questions.Count * questionDuration);
       

	
                await _service.Update(context, result, async (Context ctx, Huddle hud, Huddle originalHud) => {
                    // Ok - activity starts in 10s:
                    hud.ActivityStartUtc = DateTime.UtcNow.AddSeconds(10);
                    hud.ActivityContentTypeId = contentTypeId;
                    hud.ActivityContentId = activityContentId;

                    if (durationTicks != null)
                    {
                        hud.ActivityDurationTicks = durationTicks;
                    }

                    // Create an instance:
                    if (activityInstanceService == null)
                    {
                        activityInstanceService = Services.Get<ActivityInstanceService>();
                    }

                    var activityInstance = new ActivityInstance();
                    activityInstance = await activityInstanceService.Create(context, activityInstance, DataOptions.IgnorePermissions);
                    hud.ActivityInstanceId = activityInstance.Id;               
                });
            

            return Task.FromResult(result);
        }
		
        [HttpGet("{id}/endactivity")]
		public virtual async ValueTask EndActivity([FromRoute] uint id)
        {
			// Only possible if you can load a meeting, so we use the load perm here:
            var context = await Request.GetContext();
            var result = await _service.Get(context, id);

            if (result == null)
            {
                Response.StatusCode = 400;
                return;
            }
            
			if(result.ActivityContentId != 0)
			{
				result = await _service.Update(context, result, (Context c, Huddle h, Huddle originalHuddle) => {
                    h.ActivityContentId = 0;
                    h.ActivityContentTypeId = 0;
                    h.ActivityStartUtc = null;
                });
			}
			
			await OutputJson(context, result, null);
		}
    }
}