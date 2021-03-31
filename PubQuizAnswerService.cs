using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System.Linq;

namespace Api.PubQuizzes
{
	/// <summary>
	/// Handles pubQuizAnswers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PubQuizAnswerService : AutoService<PubQuizAnswer>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PubQuizAnswerService() : base(Events.PubQuizAnswer)
        {
			InstallAdminPages(null, null, new string[] { "id", "answerJson" });
			
			Events.PubQuizQuestion.AfterLoad.AddEventListener(async (Context context, PubQuizQuestion question) =>
			{
				if (question == null)
				{
					// Due to the way how event chains work, the primary object can be null.
					// Safely ignore this.
					return null;
				}
				
				// Load answers:
				question.Answers = await List(context, new Filter<PubQuizAnswer>().Equals("PubQuizQuestionId", question.Id), DataOptions.IgnorePermissions);
				
				return question;
			});
			
			Events.PubQuizQuestion.AfterList.AddEventListener(async (Context context, List<PubQuizQuestion> questions) =>
			{
				if (questions == null)
				{
					return null;
				}
				
				if(questions.Count == 0)
				{
					return questions;
				}
				
				var allAnswers = await List(context, new Filter<PubQuizAnswer>().EqualsSet("PubQuizQuestionId", questions.Select(q => q.Id)), DataOptions.IgnorePermissions);
				
				// question lookup:
				var lookup = new Dictionary<int, PubQuizQuestion>();
				
				foreach(var question in questions){
					if(question == null){
						continue;
					}
					question.Answers = null;
					lookup[question.Id] = question;
				}
				
				foreach(var answer in allAnswers){
					if(answer == null){
						continue;
					}
					
					if(lookup.TryGetValue(answer.PubQuizQuestionId, out PubQuizQuestion q))
					{
						if(q.Answers == null){
							q.Answers = new List<PubQuizAnswer>();
						}
						
						q.Answers.Add(answer);
					}
					
				}
				
				return questions;
			});
		}
	}
    
}
