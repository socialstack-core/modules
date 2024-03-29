﻿using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.UserFlags
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
				Roles.Member.Grant("userflag_create");

				/*
				Roles.Public.Grant("userFlag_create");
				Roles.Guest.Grant("userFlag_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("userFlag_load", "userFlag_list");
				Roles.Public.Revoke("userFlag_load", "userFlag_list");
				Roles.Member.Revoke("userFlag_load", "userFlag_list");
				*/

				return new ValueTask<object>(source);
			}, 20);
		}
	}
}