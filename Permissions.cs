using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.PubQuizzes
{
	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class Permissions
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Permissions()
		{
			// Hook the default role setup. It's done like this so it can be removed by a plugin if wanted.
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) =>
			{
				/*
				Example permission rules.
				
				Member role: A verified user account. Not an admin.
				Guest role: A user account. The transition from guest to member is up to you.
				Public role: Not logged in at all.
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("pubQuiz_create");
				Roles.Public.Grant("pubQuiz_create");
				Roles.Guest.Grant("pubQuiz_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("pubQuiz_load", "pubQuiz_list");
				Roles.Public.Revoke("pubQuiz_load", "pubQuiz_list");
				Roles.Member.Revoke("pubQuiz_load", "pubQuiz_list");
				*/
				
				/*
				Example permission rules.
				
				Member role: A verified user account. Not an admin.
				Guest role: A user account. The transition from guest to member is up to you.
				Public role: Not logged in at all.
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("pubQuizAnswer_create");
				Roles.Public.Grant("pubQuizAnswer_create");
				Roles.Guest.Grant("pubQuizAnswer_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("pubQuizAnswer_load", "pubQuizAnswer_list");
				Roles.Public.Revoke("pubQuizAnswer_load", "pubQuizAnswer_list");
				Roles.Member.Revoke("pubQuizAnswer_load", "pubQuizAnswer_list");
				*/
				
				/*
				Example permission rules.
				
				Member role: A verified user account. Not an admin.
				Guest role: A user account. The transition from guest to member is up to you.
				Public role: Not logged in at all.
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("pubQuizQuestion_create");
				Roles.Public.Grant("pubQuizQuestion_create");
				Roles.Guest.Grant("pubQuizQuestion_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("pubQuizQuestion_load", "pubQuizQuestion_list");
				Roles.Public.Revoke("pubQuizQuestion_load", "pubQuizQuestion_list");
				Roles.Member.Revoke("pubQuizQuestion_load", "pubQuizQuestion_list");
				*/
				
				Roles.Guest.Grant("pubQuizSubmission_create");
				Roles.Member.Grant("pubQuizSubmission_create");
				return new ValueTask<object>(source);
			}, 20);
		}
	}
}