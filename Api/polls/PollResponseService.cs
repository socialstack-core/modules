using System;
using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Polls
{
	/// <summary>
	/// Handles polls.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PollResponseService : AutoService<PollResponse, uint>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PollResponseService(PollAnswerService answers) : base(Events.PollResponse)
        {
			
			Events.PollResponse.BeforeCreate.AddEventListener(async (Context context, PollResponse response) => {
				
				// Note that there's a unique index on the table which blocks users
				// from answering twice.
				
				if(response == null)
				{
					return response;
				}
				
				// The answer:
				var answer = await answers.Get(context, response.AnswerId);
				
				if(answer == null)
				{
					return null;
				}
				
				// Set the response info:
				response.PollId = answer.PollId;
				response.UserId = context.UserId;
				response.CreatedUtc = DateTime.UtcNow;
				
				await answers.Update(context, answer, (Context c, PollAnswer ans) => {
					answer.Votes++;
				});
				
				return response;
			});
			
		}
	}
    
}
