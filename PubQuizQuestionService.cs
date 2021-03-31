using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;

namespace Api.PubQuizzes
{
	/// <summary>
	/// Handles pubQuizQuestions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PubQuizQuestionService : AutoService<PubQuizQuestion>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PubQuizQuestionService() : base(Events.PubQuizQuestion)
		{
			InstallAdminPages(null, null, new string[] { "id", "questionJson" });

			Events.PubQuizAnswer.BeforeCreate.AddEventListener(async (Context context, PubQuizAnswer answer) => {

				if (answer.PubQuizQuestionId == 0)
				{
					// Question required.
					return null;
				}

				var question = await Get(context, answer.PubQuizQuestionId, DataOptions.IgnorePermissions);

				if (question == null)
				{
					return null;
				}

				answer.PubQuizId = question.PubQuizId;

				return answer;
			});

			Events.PubQuizAnswer.BeforeSettable.AddEventListener((Context context, JsonField<PubQuizAnswer> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<PubQuizAnswer>>(field);
				}

				if (field.Name == "PubQuizId")
				{
					// This field isn't settable (it appears as a textarea in the admin panel otherwise).
					field = null;
				}

				return new ValueTask<JsonField<PubQuizAnswer>>(field);
			}
			);

			Events.PubQuizQuestion.BeforeSettable.AddEventListener((Context context, JsonField<PubQuizQuestion> field) =>
				{
					if (field == null)
					{
						return new ValueTask<JsonField<PubQuizQuestion>>(field);
					}

					if (field.Name == "Answers")
					{
						// This field isn't settable (it appears as a textarea in the admin panel otherwise).
						field = null;
					}

					return new ValueTask<JsonField<PubQuizQuestion>>(field);
				}
			);
		}
	}
    
}
