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
		/// Create a new web startup info instance.
		/// </summary>
		public WebServerStartupInfo()
        {
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
				
				// Optional: You can also delclare an I{ServiceName} interface.
				// This interface is mostly for documentation purposes and some extensibility.
				// Get its interface list:

				var interfaces = typeInfo.ImplementedInterfaces;
				System.Type interfaceType = null;

				foreach(var serviceInterface in interfaces)
				{
					// The interface should be I+TheTypeName:
					if (serviceInterface.Name == "I" + typeName)
					{
						interfaceType = serviceInterface;
						break;
					}
				}

				Console.WriteLine("Registered service: " + typeName);

				if (interfaceType != null)
				{
					services.AddSingleton(interfaceType, typeInfo.AsType());
					_serviceTypes.Add(interfaceType);
				}
				else
				{
					// Add it as-is:
					services.AddSingleton(typeInfo.AsType());
					_serviceTypes.Add(typeInfo.AsType());
				}
				
				
			}
		
			services.AddCors(c =>  
			{  
				c.AddDefaultPolicy(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
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

#if DEBUG
			app.UseDeveloperExceptionPage();
#endif

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
			
            foreach (var serviceInterfaceType in _serviceTypes)
			{
				var svc = serviceProvider.GetService(serviceInterfaceType);
				
				Services.All[serviceInterfaceType] = svc;
				Services.AllByName[serviceInterfaceType.Name] = svc;

				// If it's an AutoService, add it to the lookup:
				var autoServiceType = GetAutoServiceType(svc.GetType());

				if (autoServiceType != null)
				{
					var autoService = svc as AutoService;
					Services.AutoServices[autoServiceType] = autoService;
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

				if (type.GetGenericTypeDefinition() == typeof(AutoService<>))
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
