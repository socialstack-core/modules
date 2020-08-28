using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System.Linq;

namespace Api.Polls
{
	/// <summary>
	/// Handles polls.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PollAnswerService : AutoService<PollAnswer>, IPollAnswerService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PollAnswerService() : base(Events.PollAnswer)
        {
			InstallAdminPages(null, null, new string[] { "id", "name" });
			
			Events.PollAnswer.BeforeSettable.AddEventListener((Context context, JsonField<PollAnswer> field) =>
			{
				if (field == null)
				{
					return Task.FromResult(field);
				}
				
				if(field.Name == "Votes")
				{
					// This field isn't settable
					field = null;
				}
				
				return Task.FromResult(field);
			});
			
			Events.Poll.AfterLoad.AddEventListener(async (Context context, Poll poll) =>
			{
				if (poll == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}
				
				// Get the answers:
				poll.Answers = await List(context, new Filter<PollAnswer>().Equals("PollId", poll.Id));
				return poll;
			});
			
			Events.Poll.AfterUpdate.AddEventListener(async (Context context, Poll poll) =>
			{
				if (poll == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}
				
				// Get the answers:
				poll.Answers = await List(context, new Filter<PollAnswer>().Equals("PollId", poll.Id));
				
				return poll;
			});
			
			Events.Poll.AfterCreate.AddEventListener(async (Context context, Poll poll) =>
			{
				if (poll == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}
				
				// Get the answers:
				poll.Answers = await List(context, new Filter<PollAnswer>().Equals("PollId", poll.Id));
				
				return poll;
			});
			
			Events.Poll.AfterList.AddEventListener(async (Context context, List<Poll> polls) =>
			{
				if (polls == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}

				if (polls.Count == 0)
				{
					return polls;
				}

				// Get the answers:
				var allAnswers = await List(context, new Filter<PollAnswer>().EqualsSet("PollId", polls.Select(poll => poll.Id)));
				
				// Filter them through to the actual polls they're for:
				if(allAnswers != null)
				{
					var pollMap = new Dictionary<int, Poll>();
					
					foreach(var poll in polls)
					{
						if(poll == null)
						{
							continue;
						}
						pollMap[poll.Id] = poll;
					}
					
					foreach(var answer in allAnswers)
					{
						if(answer == null)
						{
							continue;
						}
						
						if(pollMap.TryGetValue(answer.PollId, out Poll poll))
						{
							
							if(poll.Answers == null)
							{
								poll.Answers = new List<PollAnswer>();
							}
							
							poll.Answers.Add(answer);
						}
					}
				}
				
				return polls;
			});

			
		}
	}
    
}
