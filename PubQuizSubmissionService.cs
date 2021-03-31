using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.PubQuizzes
{
	/// <summary>
	/// Handles pubQuizSubmissions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PubQuizSubmissionService : AutoService<PubQuizSubmission>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PubQuizSubmissionService(PubQuizAnswerService answers) : base(Events.PubQuizSubmission)
        {
			var pubQuizAnswerContentTypeId = ContentTypes.GetId(typeof(PubQuizAnswer));
			// Example admin page install:
			// InstallAdminPages("PubQuizSubmissions", "fa:fa-rocket", new string[] { "id", "name" });
			Events.PubQuizSubmission.AfterList.AddEventListener(async (Context context, List<PubQuizSubmission> submissions) =>
			{
				if (submissions == null)
				{
					return null;
				}

				await Content.ApplyMixed(
					context,
					submissions,
					src => {
						var submission = src as PubQuizSubmission;
						return new ContentTypeAndId(pubQuizAnswerContentTypeId, submission.PubQuizAnswerId);
					},
					(object src, object content) => {
						var submission = src as PubQuizSubmission;
						submission.PubQuizAnswer = content;
					}
				);

				return submissions;
			});

			Events.PubQuizSubmission.BeforeCreate.AddEventListener(async (Context context, PubQuizSubmission submission) =>
			{
				if (submission == null)
                {
					return null;
                }

				var answer = await answers.Get(context, submission.PubQuizAnswerId, DataOptions.IgnorePermissions);

				if (answer == null)
				{
					return null;
				}

				submission.IsCorrect = answer.IsCorrect;
				return submission;
			});

		}
	}
    
}
