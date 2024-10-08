﻿using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.ActiveLogins
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
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("activeLogin_load", "activeLogin_list");
				Roles.Public.Revoke("activeLogin_load", "activeLogin_list");
				Roles.Member.Revoke("activeLogin_load", "activeLogin_list");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("activeLoginHistory_load", "activeLoginHistory_list");
				Roles.Public.Revoke("activeLoginHistory_load", "activeLoginHistory_list");
				Roles.Member.Revoke("activeLoginHistory_load", "activeLoginHistory_list");

				Roles.Guest.Grant("activeLoginHistory_create", "activeLogin_create");
				Roles.Member.Grant("activeLoginHistory_create", "activeLogin_create");

				return new ValueTask<object>(source);
			}, 20);
		}
	}
}