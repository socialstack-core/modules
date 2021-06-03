using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.FrequentlyAskedQuestions
{
	/// <summary>
	/// Handles frequentlyAskedQuestions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class FrequentlyAskedQuestionService : AutoService<FrequentlyAskedQuestion, uint>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public FrequentlyAskedQuestionService() : base(Events.FrequentlyAskedQuestion)
        {
			InstallAdminPages("FAQs", "fa:fa-question-circle", new string[] { "id", "question" });
		}
	}
    
}
