using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Api.ContentSync;
using System;
using Api.Users;
using Api.WebSockets;
using System.Text;

namespace Api.ActiveLogins
{
	/// <summary>
	/// Handles activeLogins.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ActiveLoginHistoryService : AutoService<ActiveLoginHistory>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ActiveLoginHistoryService() : base(Events.ActiveLoginHistory)
        {
			
		}
		
	}
	
}
