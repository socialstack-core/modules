using Api.Blogs;
using Api.Permissions;
using Api.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Api.Eventing;


/// <summary>
/// Events are instanced automatically. 
/// You can however specify a custom type or instance them yourself if you'd like to do so.
/// </summary>
public partial class Events
{
	/// <summary>
	/// Set of events for the Kestrel webserver.
	/// </summary>
	public static KestrelEventGroup Kestrel;

	/// <summary>
	/// Set of events for the main application.
	/// </summary>
	public static WebServerStartupEventGroup WebServerStartup;

}

/// <summary>
/// Event group for the web services.
/// </summary>
public partial class WebServerStartupEventGroup : EventGroup
{

	/// <summary>
	/// The first thing that runs during app construction.
	/// </summary>
	public EventHandler<IApplicationBuilder> BeforeConfigureApplication;

	/// <summary>
	/// The main app configuration event.
	/// </summary>
	public EventHandler<IApplicationBuilder> ConfigureApplication;

	/// <summary>
	/// Called during service collection.
	/// </summary>
	public EventHandler<IServiceCollection> ConfigureServices;

	/// <summary>
	/// The last thing that runs during the app construction.
	/// </summary>
	public EventHandler<IApplicationBuilder> AfterConfigureApplication;

	/// <summary>
	/// During host configuring.
	/// </summary>
	public EventHandler<IWebHostBuilder> ConfigureHost;

	/// <summary>
	/// Host built and ready.
	/// </summary>
	public EventHandler<IWebHost> HostReady;

}

/// <summary>
/// Event group for the Kestrel webserver.
/// </summary>
public partial class KestrelEventGroup : EventGroup
{

	/// <summary>
	/// The first thing that runs during UseKestrel. 
	/// The bool is true if it is running in "Edge mode" meaning Kestrel is not frontend by a reverse proxy like NGINX.
	/// </summary>
	public EventHandler<KestrelServerOptions, bool> BeforeConfigure;

	/// <summary>
	/// The main configuration event. Clearing this results in the default behaviour being removed.
	/// </summary>
	public EventHandler<KestrelServerOptions, bool> Configure;

	/// <summary>
	/// The last thing that runs during UseKestrel.
	/// </summary>
	public EventHandler<KestrelServerOptions, bool> AfterConfigure;

}
