using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;


namespace Api.Quizzes
{
	/// <summary>
	/// Handles creations of questions - containers for answers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class QuizQuestionService : AutoService<QuizQuestion>, IQuizQuestionService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public QuizQuestionService() : base(Events.QuizQuestion)
        {
			// Create admin pages if they don't already exist:
			InstallAdminPages(new string[]{"id", "title"});
        }
	}
}