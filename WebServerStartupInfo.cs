using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Api.Configuration;
using System.Collections.Generic;
using System.Reflection;

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
		/// An event which fires when the application is being configured.
		/// </summary>
		public static event Action<IApplicationBuilder> OnConfigureApplication;

		/// <summary>
		/// An event which fires when the application is shutting down.
		/// </summary>
		public static event Action OnShutdown;

		/// <summary>
		/// Create a new web startup info instance.
		/// </summary>
		/// <param name="env"></param>
		public WebServerStartupInfo(IHostingEnvironment env)
        {
        }

		private List<Type> _serviceTypes;

        /// <summary>
		/// Called by the runtime. This automatically looks for classes which end 
		/// with *Service and implement an interface of the same name preceeded with I.
		/// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
			
			// Start checking types:
			var allTypes = typeof(WebServerStartupInfo).Assembly.DefinedTypes;

			_serviceTypes = new List<Type>();

			foreach (var typeInfo in allTypes){
				// If it:
				// - Is a class
				// - Ends with *Service
				// - Implements an interface of the same name + I
				// Then we register it as a singleton.

				var typeName = typeInfo.Name;

				if (!typeInfo.IsClass || !typeName.EndsWith("Service"))
				{
					continue;
				}

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

				if (interfaceType == null)
				{
					continue;
				}

				Console.WriteLine("Registered service: " + typeName);

				// Ok! Got a valid service. We can now register it:
				services.AddSingleton(interfaceType, typeInfo.AsType());

				_serviceTypes.Add(interfaceType);
			}
			
			// Run the first event (IEventListener implementors can use).
			OnConfigureServices?.Invoke(services);
			
		}

		/// <summary>
		/// Configures the HTTP pipeline.
		/// </summary>
		public void Configure(
				IApplicationBuilder app, IHostingEnvironment env, 
				ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IApplicationLifetime applicationLifetime)
		{
			// Set the service provider:
			Services.Provider = serviceProvider;
			
			app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

			#if DEBUG
			app.UseDeveloperExceptionPage();
			#endif

			// Fire off an event so services can also extend app if they want (IEventListener implementors can use).
			OnConfigureApplication?.Invoke(app);
			
            app.UseMvc();
			
			// Next, we get *all* services so they are all instanced.
			foreach (var serviceInterfaceType in _serviceTypes)
			{
				var svc = serviceProvider.GetService(serviceInterfaceType);
				Services.All[serviceInterfaceType] = svc;
			}

			// Services are now all instanced - fire off service OnStart event:
			Services.TriggerStart();
			
			// Register shutdown event:
			applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);
			
		}
		
		/// <summary>
		/// Called when we're going down.
		/// </summary>
		private void OnApplicationStopping(){

			// Fire off the shutdown event:
			OnShutdown?.Invoke();

		}
	}
}
