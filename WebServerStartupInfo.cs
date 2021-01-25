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
using Api.Database;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Api.Configuration;

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
		private CorsConfig _corsConfig;

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

		private List<Type> _serviceTypes;

        /// <summary>
		/// Called by the runtime. This automatically looks for classes which end 
		/// with *Service and implement an interface of the same name preceeded with I.
		/// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
#if NETCOREAPP2_2 || NETCOREAPP2_1
			services.AddMvc();
#else
			services.AddControllers().AddNewtonsoftJson();
#endif

			// Remove .NET size limitations:
			services.Configure<FormOptions>(x =>
			{
				x.ValueLengthLimit = int.MaxValue;
				x.MultipartBodyLengthLimit = long.MaxValue; // In case of multipart
			});
			
			// Start checking types:
			var allTypes = typeof(WebServerStartupInfo).Assembly.DefinedTypes;

			_serviceTypes = new List<Type>();

			foreach (var typeInfo in allTypes){
				// If it:
				// - Is a class
				// - Ends with *Service, with a specific exclusion for AutoService.
				// Then we register it as a singleton.

				var typeName = typeInfo.Name;

				if (!typeInfo.IsClass || !typeName.EndsWith("Service") || typeName == "AutoService")
				{
					continue;
				}

				// Must also be in the Api.* namespace:
				if (typeInfo.Namespace == null || !typeInfo.Namespace.StartsWith("Api."))
				{
					continue;
				}
				
				// Ok! Got a valid service. We can now register it:
				services.AddSingleton(typeInfo.AsType());
				_serviceTypes.Add(typeInfo.AsType());
				
				Console.WriteLine("Registered service: " + typeName);
			}
		
			services.AddCors(c =>  
			{  
				c.AddDefaultPolicy(options => {

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

					options.AllowAnyHeader().AllowAnyMethod();
				});
			});

			// Run the first event (IEventListener implementors can use).
			OnConfigureServices?.Invoke(services);
			
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

			app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Token"));

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
						var response = publicError.Apply(context.Response);
						context.Response.StatusCode = publicError.StatusCode;
						await context.Response.WriteAsync(publicError.ToJson());
					}
					else
					{
						if(e != null){
							Console.WriteLine(e.ToString());
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


			// Next, we get *all* services so they are all instanced.
			// First they'll be sorted though so services that require loading early can do so.
			_serviceTypes = _serviceTypes.OrderBy(type => {

				var loadPriority = type.GetCustomAttribute<LoadPriorityAttribute>();
				if (loadPriority == null)
				{
					return 10;
				}

				return loadPriority.Priority;
			}).ToList();
			
            foreach (var serviceType in _serviceTypes)
			{
				var svc = serviceProvider.GetService(serviceType);
				
				Services.All[serviceType] = svc;
				Services.AllByName[serviceType.Name] = svc;

				// If it's an AutoService, add it to the lookup:
				var autoServiceType = GetAutoServiceType(svc.GetType());

				if (autoServiceType != null)
				{
					var autoService = svc as AutoService;
					Services.AutoServices[autoServiceType] = autoService;
					Services.ServicedTypes[autoService.ServicedType] = autoService;
					var contentId = ContentTypes.GetId(autoService.ServicedType);
					Services.ContentTypes[contentId] = autoService;
				}
				
			}

			// Services are now all instanced - fire off service OnStart event:
			Services.TriggerStart();
		}
		
		/// <summary>
		/// Attempts to find the AutoService type for the given type, or null if it isn't one.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private Type GetAutoServiceType(Type type){
			
			if (type.IsGenericType)
			{

				if (type.GetGenericTypeDefinition() == typeof(AutoService<,>))
				{
					// Yep, this is an AutoService type.
					return type;
				}

			}

			if (type.BaseType != null)
			{
				return GetAutoServiceType(type.BaseType);
			}

			return null;
		}
		
	}
}
