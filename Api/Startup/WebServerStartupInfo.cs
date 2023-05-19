using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Api.Configuration;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
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
		/// An event which fires when services are being configured.
		/// </summary>
		public static event Action<IServiceCollection> OnConfigureServices;

		/// <summary>
		/// An event which fires when services are being configured.
		/// </summary>
		public static event Action<MvcOptions> OnConfigureMvc;

		/// <summary>
		/// An event which fires when Configure occurs.
		/// </summary>
		public static event Action<IApplicationBuilder, ILoggerFactory, IServiceProvider> OnConfigure;

		/// <summary>
		/// An event which fires when the application is being configured.
		/// </summary>
		public static event Action<IApplicationBuilder> OnConfigureApplication;

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
#if NETCOREAPP2_2 || NETCOREAPP2_1
			services.AddMvc();
#else
			mvcBuilder = services.AddControllers(options => {

				OnConfigureMvc?.Invoke(options);

			}).AddNewtonsoftJson();
#endif

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

			// Run the first event (IEventListener implementors can use).
			OnConfigureServices?.Invoke(services);
			
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
			OnConfigure?.Invoke(app, loggerFactory, serviceProvider);
			
            // Set the service provider:
            Services.Provider = serviceProvider;

#if !NETCOREAPP2_1 && !NETCOREAPP2_2

			// Fire off an event so services can also extend app if they want (IEventListener implementors can use).
			OnConfigureApplication?.Invoke(app);

			app.UseRouting();
#endif

			app.UseCors(options => SetupCors(options));

			app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
		
			app.UseExceptionHandler(errorApp =>
			{
				errorApp.Run(async context =>
				{
					context.Response.ContentType = "application/json";
					var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
					var e = exceptionHandlerPathFeature?.Error;
					var publicError = (e as PublicException);
					
					if(publicError != null)
					{
						Log.Info("", publicError.Message);
						context.Response.StatusCode = publicError.StatusCode;
						await context.Response.WriteAsync(publicError.ToJson());
					}
					else
					{
						if(e != null){
							Log.Error("", e);
						}
						context.Response.StatusCode = 500;
						await context.Response.WriteAsync("{\"message\": \"An internal error has occurred - please try again later.\", \"code\": \"server_error\"}");
					}
				});
			});
		
#if NETCOREAPP2_1 || NETCOREAPP2_2
			
			// Fire off an event so services can also extend app if they want (IEventListener implementors can use).
			OnConfigureApplication?.Invoke(app);

            app.UseMvc();
#else
			app.UseEndpoints(endpoints =>
			{
				// Mapping of endpoints goes here:
				endpoints.MapControllers();
			});
#endif

			// Instance all services:
			Services.InstanceAll(serviceProvider);

		}
		
	}
}
