using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;


namespace Api.Quizzes
{
	/// <summary>
	/// Handles quizzes.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class QuizService : AutoService<Quiz>, IQuizService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public QuizService() : base(Events.Quiz)
        {
			
			// Create admin pages if they don't already exist:
			InstallAdminPages("Quizzes", "fa:fa-chalkboard", new string[]{"id", "title"});
			
        }
	}
}