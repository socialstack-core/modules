﻿using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Answers
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
				// Public - the role used by anonymous users.
				Roles.Guest.Grant("answer_create");
				Roles.Member.Grant("answer_create");
				Roles.Guest.Grant("question_create");
				Roles.Member.Grant("question_create");
				return Task.FromResult(source);
			});
		}
	}
}