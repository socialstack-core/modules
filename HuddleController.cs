using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Contexts;
using Api.Eventing;
using Api.Huddles;
using Api.UserAgendaEntries;
using Api.Users;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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
        public virtual async Task<object> StartActivity([FromRoute] int id, [FromRoute] string activityContentTypeName, [FromRoute] int activityContentId)
        {
            // Only possible if you can load a meeting, so we use the load perm here:
            var context = Request.GetContext();
            var result = await _service.Get(context, id);

            if (result == null)
            {
                return null;
            }
            
            int contentTypeId = ContentTypes.GetId(activityContentTypeName);

            // Can only set content that the user is permitted to actually use - so try getting it first:
            var sourceContent = await Api.Database.Content.Get(context, contentTypeId, activityContentId, true);

            int? durationTicks = null;
            // If the source isn't null, let's see if its a quiz. If yes, lets get all questions with that quiz id to get total number of questions.
            if (sourceContent != null && contentTypeId == pubQuizsContentTypeId)
            {
                if(pubQuizQuestionService == null)
                {
                    pubQuizQuestionService = Services.Get<PubQuizQuestionService>();
                }

                // it is a pubquiz, let's get its questions.
                var questions = await pubQuizQuestionService.List(context, new Filter<PubQuizQuestion>().Equals("PubQuizId", activityContentId));
                durationTicks = (1000 * questions.Count * questionDuration);
            }

            if (sourceContent != null)
            {
                // Ok - activity starts in 10s:
                result.ActivityStartUtc = DateTime.UtcNow.AddSeconds(10);
                result.ActivityContentTypeId = contentTypeId;
                result.ActivityContentId = activityContentId;

                if(durationTicks != null)
                {
                    result.ActivityDurationTicks = durationTicks;
                }
				
				// Create an instance:
				if (activityInstanceService == null)
				{
					activityInstanceService = Services.Get<ActivityInstanceService>();
				}
				
				var activityInstance = new ActivityInstance();
				activityInstance = await activityInstanceService.Create(context, activityInstance);
				result.ActivityInstanceId = activityInstance.Id;
				
                await _service.Update(context, result);
            }

            return Task.FromResult(result);
        }
		
        [HttpGet("{id}/endactivity")]
		public virtual async Task<object> EndActivity([FromRoute] int id)
        {
			// Only possible if you can load a meeting, so we use the load perm here:
            var context = Request.GetContext();
            var result = await _service.Get(context, id);

            if (result == null)
            {
                return null;
            }
            
			if(result.ActivityContentId != 0)
			{
				result.ActivityContentId = 0;
				result.ActivityContentTypeId = 0;
				result.ActivityStartUtc = null;
				result = await _service.Update(context, result);
			}
			
			return Task.FromResult(result);
		}
    }
}