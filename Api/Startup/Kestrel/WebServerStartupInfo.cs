using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Api.Configuration;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Api.SocketServerLibrary;
using Api.Startup.Routing;
using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;

namespace Api.Startup
{
	/// <summary>
	/// Used by ASP.NET Core as the startup object.
	/// It discovers and registers all available services.
	/// </summary>
	public class WebServerStartupInfo
    {
		/// <summary>
		/// Cors configuration.
		/// </summary>
		private readonly CorsConfig _corsConfig;

		/// <summary>
		/// Create a new web startup info instance.
		/// </summary>
		public WebServerStartupInfo()
        {
			_corsConfig = AppSettings.GetSection("Cors").Get<CorsConfig>();

			if (_corsConfig == null)
			{
				// Ensure it's always set:
				_corsConfig = new CorsConfig();
			}
		}

		/// <summary>
		/// IMVCBuilder, available during the OnConfigureServices event.
		/// </summary>
		public static IMvcBuilder mvcBuilder;

		/// <summary>
		/// Called by the runtime. This automatically looks for classes which end 
		/// with *Service and implement an interface of the same name preceeded with I.
		/// </summary>
		public void ConfigureServices(IServiceCollection services)
        {
			// Remove .NET size limitations:
			services.Configure<FormOptions>(x =>
			{
				x.ValueLengthLimit = int.MaxValue;
				x.MultipartBodyLengthLimit = long.MaxValue; // In case of multipart
			});

			Services.RegisterInto(services);

			services.AddCors(c =>
			{
				c.AddDefaultPolicy(options => SetupCors(options));
			});

			// Run the first event (EventListener implementors can use).
			Task.Run(async () => {
				await Events.WebServerStartup.ConfigureServices.Dispatch(new Context(1,0,1), services);
			}).Wait();
		}

		private void SetupCors(CorsPolicyBuilder options)
		{
			if (_corsConfig.Origins != null && _corsConfig.Origins.Length != 0)
			{
				// Use specific origins:
				options.WithOrigins(_corsConfig.Origins);
			}
			else
			{
				// Any:
				options.AllowAnyOrigin();
			}

			if (_corsConfig.AllowCredentials)
			{
				options.AllowCredentials();
			}

			if (_corsConfig.Headers != null && _corsConfig.Headers.Length != 0)
			{
				// Use specific origins:
				options.WithHeaders(_corsConfig.Headers);
			}
			else
			{
				// Any:
				options.AllowAnyHeader();
			}

			if (_corsConfig.Methods != null && _corsConfig.Methods.Length != 0)
			{
				// Use specific methods:
				options.WithMethods(_corsConfig.Methods);
			}
			else
			{
				// Any:
				options.AllowAnyMethod();
			}

			if (_corsConfig.ExposedHeaders != null && _corsConfig.ExposedHeaders.Length != 0)
			{
				options.WithExposedHeaders(_corsConfig.ExposedHeaders);
			}

		}

		/// <summary>
		/// Configures the HTTP pipeline.
		/// </summary>
		public void Configure(
				IApplicationBuilder app, 
				ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
		{
			// Fire off an event so services can also extend app if they want (EventListener implementors can use).
			Task.Run(async () => {
				// Set the service provider:
				Services.Provider = serviceProvider;

				var startupContext = new Context(1, 0, 1);
				
				await Events.WebServerStartup.BeforeConfigureApplication.Dispatch(startupContext, app);
				await Events.WebServerStartup.ConfigureApplication.Dispatch(startupContext, app);

				Events.Service.AfterStart.AddEventListener(async (Context c, object svc) => {

					// All services are ready. Collect custom routes.
					await RouterBuilder.Start();
					Log.Info("router", "Web router started");

					return svc;
				}, 50);

				// Instance all services:
				Services.InstanceAll(serviceProvider);

				var htmlService = Services.Get<Pages.HtmlService>();

				app.Use(async (HttpContext httpContext, RequestDelegate next) =>
				{
					var router = Router.CurrentRouter;

					if (router == null)
					{
						httpContext.Response.StatusCode = 503;
						httpContext.Response.Headers.Append("Pragma", "no-cache");
						httpContext.Response.Headers.Append("Cache-Control", "no-cache");
						httpContext.Response.Headers.Append("Retry-After", "5");

						var writer = Writer.GetPooled();
						writer.Start(null);
						writer.WriteASCII("This site is currently starting after some maintenance. Please check back in a moment.");
						await writer.CopyToAsync(httpContext.Response.Body);
						writer.Release();
						return;
					}

					try
					{
						var context = await httpContext.Request.GetBasicContext();
						var handled = await router.HandleRequest(httpContext, context);

						if (!handled)
						{
							// This mainly occurs when primary content was not found.
							// I.e. the page existed and thus so did the route, but the
							// route could not handle the request without the content.
							await router.Run404(httpContext, context);
						}
					}
					catch (PublicException publicError)
					{
						httpContext.Response.ContentType = "application/json";

						Log.Info("", publicError.Message);
						httpContext.Response.StatusCode = publicError.StatusCode;
						var writer = Writer.GetPooled();
						writer.Start(null);
						writer.WriteASCII("{\"message\":");
						writer.WriteEscaped(publicError.Response.Message);
						writer.WriteASCII(",\"code\":");
						writer.WriteEscaped(publicError.Response.Code);
						writer.WriteASCII("}");
						await writer.CopyToAsync(httpContext.Response.Body);
						writer.Release();

					}
					catch(Exception e)
					{
						if (e != null)
						{
							Log.Error("", e);
						}

						httpContext.Response.StatusCode = 500;
						await httpContext.Response.WriteAsync("{\"message\": \"An internal error has occurred - please try again later.\", \"code\": \"server/error\"}");
					}
				});

				app.UseCors(options => SetupCors(options));

				app.UseForwardedHeaders(new ForwardedHeadersOptions
				{
					ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
				});

				await Events.WebServerStartup.AfterConfigureApplication.Dispatch(startupContext, app);
			}).Wait();

		}

	}
}
