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
	public partial class PollAnswerService : AutoService<PollAnswer, uint>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PollAnswerService() : base(Events.PollAnswer)
        {
			InstallAdminPages(null, null, new string[] { "id", "name" });
			
			Events.PollAnswer.BeforeSettable.AddEventListener((Context context, JsonField<PollAnswer,uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<PollAnswer, uint>>(field);
				}
				
				if(field.Name == "Votes")
				{
					// This field isn't settable
					field = null;
				}
				
				return new ValueTask<JsonField<PollAnswer, uint>>(field);
			});
		}
	}
    
}
