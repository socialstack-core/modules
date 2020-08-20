using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.PubQuizzes
{
	/// <summary>
	/// Handles pubQuizzes.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PubQuizService : AutoService<PubQuiz>, IPubQuizService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PubQuizService() : base(Events.PubQuiz)
        {
			InstallAdminPages("Quizzes", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
