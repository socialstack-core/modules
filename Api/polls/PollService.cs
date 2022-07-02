using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;

namespace Api.Polls
{
	/// <summary>
	/// Handles polls.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PollService : AutoService<Poll, uint>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PollService() : base(Events.Poll)
        {
			InstallAdminPages(
				"Polls", "fa:fa-poll", new string[] { "id", "title" },
				new ChildAdminPageOptions()
				{
					ChildType = "PollAnswer",
					Fields = new string[] { "title" }
				}
			);

			Events.Poll.BeforeSettable.AddEventListener((Context context, JsonField<Poll, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<Poll, uint>>(field);
				}
				
				if(field.Name == "Answers")
				{
					// This field isn't settable
					field = null;
				}
				
				return new ValueTask<JsonField<Poll, uint>>(field);
			});
			
		}
	}
    
}
