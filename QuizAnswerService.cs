using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;


namespace Api.Quizzes
{
	/// <summary>
	/// Handles quiz answers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class QuizAnswerService : AutoService<QuizAnswer>, IQuizAnswerService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public QuizAnswerService() : base(Events.QuizAnswer)
        {
			// Create admin pages if they don't already exist:
			InstallAdminPages(new string[]{"id", "title"});
        }
	}
}