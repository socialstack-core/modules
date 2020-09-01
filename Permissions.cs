using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Huddles
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
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("huddle_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("huddle_load", "huddle_list");
				Roles.Public.Revoke("huddle_load", "huddle_list");
				Roles.Member.Revoke("huddle_list");
				
				// Can only list public huddles:
				Roles.Member.If((Filter f) => f.EqualsField("HuddleType", 0)).ThenGrant("huddle_list");
				
				/*
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("huddleLoadMetric_create");
				Roles.Public.Grant("huddleLoadMetric_create");
				Roles.Guest.Grant("huddleLoadMetric_create");
				*/
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("huddleLoadMetric_load", "huddleLoadMetric_list", "huddleserver_load", "huddleserver_list");
				Roles.Public.Revoke("huddleLoadMetric_load", "huddleLoadMetric_list", "huddleserver_load", "huddleserver_list");
				Roles.Member.Revoke("huddleLoadMetric_load", "huddleLoadMetric_list", "huddleserver_load", "huddleserver_list");
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("huddlePermittedUser_create");
				
				/*
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("huddlePermittedUser_load", "huddlePermittedUser_list");
				Roles.Public.Revoke("huddlePermittedUser_load", "huddlePermittedUser_list");
				Roles.Member.Revoke("huddlePermittedUser_load", "huddlePermittedUser_list");
				*/
				
				return Task.FromResult(source);
			}, 20);
		}
	}
}