using System;
using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Microsoft.Extensions.Configuration;
using Api.Configuration;
using Api.StackTools;
using System.Diagnostics;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Net;
using Api.Signatures;
using Api.AutoForms;

namespace Api.ContentSync
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(2)]
	public partial class ClusteredServerService : AutoService<ClusteredServer>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ClusteredServerService() : base(Events.ClusteredServer)
		{
			
		}
	}
}
