using Api.Configuration;
using Api.Contexts;
using Api.Eventing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Api.Startup;


/// <summary>
/// Handles some global Kestrel related config.
/// </summary>
[EventListener]
public class KestrelInit
{
	/// <summary>
	/// Instanced automatically.
	/// </summary>
	public KestrelInit()
	{
		Events.WebServerStartup.ConfigureHost.AddEventListener((Context context, IWebHostBuilder builder) => {
			builder.UseKestrel(async options =>
			{
				var edgeMode = AppSettings.IsEdgeMode();

				await Events.Kestrel.BeforeConfigure.Dispatch(context, options, edgeMode);
				await Events.Kestrel.Configure.Dispatch(context, options, edgeMode);
				await Events.Kestrel.AfterConfigure.Dispatch(context, options, edgeMode);
			})
			.UseContentRoot(Directory.GetCurrentDirectory())
			.UseStartup(builder =>
			{
				return new WebServerStartupInfo();
			});

			return new ValueTask<IWebHostBuilder>(builder);
		});

		Events.Kestrel.Configure.AddEventListener((Context context, KestrelServerOptions options, bool edgeMode) => {

			if (edgeMode)
			{
				// Unencrypted endpoint which gets redirected
				options.ListenAnyIP(80, listenOptions =>
				{
					listenOptions.Protocols = HttpProtocols.Http1;
				});

				options.ListenAnyIP(443, listenOptions =>
				{
					if (AppSettings.GetString("Quic", null) != null)
					{
						// Dotnet adds the alt-svc header itself
						listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
					}
					else
					{
						listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
					}

					listenOptions.UseHttps(httpsOptions =>
					{
						httpsOptions.ServerCertificateSelector = (connectionContext, name) =>
						{
							return CertificateHolder.GetCertificate(name);
						};
					});
				});
			}
			else
			{
				var portNumber = AppSettings.GetInt32("Port", 5000);

				// If running inside a container, we'll need to listen to the 0.0.0.0 (any) interface:
				var ip = AppSettings.GetInt32("Container", 0) == 1 ? IPAddress.Any : IPAddress.Loopback;

				options.Listen(ip, portNumber, listenOpts =>
				{
					listenOpts.Protocols = HttpProtocols.Http1AndHttp2;
				});

				Log.Info("webserverservice", null, "Ready on " + ip + ":" + portNumber);
			}
			options.Limits.MaxRequestBodySize = AppSettings.GetInt64("MaxBodySize", 5120000000); // 5G by default

			return new ValueTask<KestrelServerOptions>(options);
		});

		Events.Kestrel.AfterConfigure.AddEventListener((Context context, KestrelServerOptions options, bool edgeMode) => {
			var apiSocketFile = GetSocketFilePath();

			if (apiSocketFile != null)
			{
				options.ListenUnixSocket(apiSocketFile);
			}

			return new ValueTask<KestrelServerOptions>(options);
		});

		Events.WebServerStartup.BeforeConfigureApplication.AddEventListener((Context context, IApplicationBuilder app) => {
			var edgeMode = AppSettings.IsEdgeMode();

			if (edgeMode)
			{
				// All traffic on port 80 is redirected
				// (inclusive of Let's Encrypt cert requests, which do run over broken https as well by design)
				app.MapWhen(ctx => ctx.Connection.LocalPort == 80, httpBranch =>
				{
					httpBranch.Run(async context =>
					{
						var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
						context.Response.Redirect(httpsUrl, permanent: true);
					});
				});
			}

			return new ValueTask<IApplicationBuilder>(app);
		});
		
		Events.WebServerStartup.HostReady.AddEventListener((Context context, IWebHost host) => {

			var apiSocketFile = GetSocketFilePath();

			if (apiSocketFile != null)
			{
				Chmod.Set(apiSocketFile); // 777
			}

			return new ValueTask<IWebHost>(host);
		});
	}

	private bool _loadedSocketPath;
	private string _socketFilePath;

	/// <summary>
	/// Null if a Unix socket should not start.
	/// </summary>
	/// <returns></returns>
	private string GetSocketFilePath()
	{
		if (_loadedSocketPath)
		{
			return _socketFilePath;
		}

		_loadedSocketPath = true;

		// Create the socket file if running on Linux *and* reverse proxy is active
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || AppSettings.GetString("LocalReverseProxy", null) == null)
		{
			_socketFilePath = null;
			return null;
		}

		_socketFilePath = System.IO.Path.GetFullPath("api.sock");

		try
		{
			// Delete if exists:
			System.IO.File.Delete(_socketFilePath);
		}
		catch 
		{
		}

		return _socketFilePath;
	}

}