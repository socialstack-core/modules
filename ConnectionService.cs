using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Api.Connections
{
	/// <summary>
	/// Handles connections (subscribers).
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ConnectionService : AutoService<Connection>, IConnectionService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ConnectionService() : base(Events.Connection)
        {
			InstallAdminPages("Connections", "fa:fa-user-friends", new string[] { "id", "email" });
		}
	}
}