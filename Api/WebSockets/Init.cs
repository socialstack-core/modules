using Api.CanvasRenderer;
using Api.Configuration;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.WebSockets;

/// <summary>
/// Instances events during the very earliest phases of startup.
/// </summary>
[EventListener]
public class Init
{
	
	/// <summary>
	/// Called automatically.
	/// </summary>
	public Init()
	{
		// Mounting for Edge only mode.
		Events.WebServerStartup.BeforeConfigureApplication.AddEventListener((Context context, IApplicationBuilder app) => {

			var edgeMode = AppSettings.IsEdgeMode();

			if (app == null || !edgeMode)
			{
				return new ValueTask<IApplicationBuilder>(app);
			}

			// Running in Edge mode.
			// Use the dotnet built in websocket server here.
			app.UseWebSockets();

			return new ValueTask<IApplicationBuilder>(app);
		});
	}
	
}
