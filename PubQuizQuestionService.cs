using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.PubQuizzes
{
	/// <summary>
	/// Handles pubQuizQuestions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PubQuizQuestionService : AutoService<PubQuizQuestion>, IPubQuizQuestionService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PubQuizQuestionService() : base(Events.PubQuizQuestion)
        {
		}
	}
    
}
