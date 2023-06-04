using Api.Eventing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;

namespace Api.Startup
{
	/// <summary>
	/// Helper class for grabbing service references.
	/// </summary>
	public static class Services
	{
		/// <summary>
		/// Environment that we're running in. Use IsDevelopment, IsStaging and IsProduction for common ones.
		/// Has been sanitised so will typically be "dev", "stage" and "prod".
		/// </summary>
		public static string Environment;
		
		/// <summary>
		/// Environment that we're running in, exactly as it appears in the appsettings file.
		/// </summary>
		public static string OriginalEnvironment;
		
		/// <summary>
		/// True when AfterStart has been called.
		/// </summary>
		public static bool Started;

		/// <summary>
		/// If services have not started yet, you can wait for this.
		/// </summary>
		public static TaskCompletionSource StartupWaiter = new TaskCompletionSource();

		/// <summary>
		/// The list of all service types sorted by their load order. Cleared after startup.
		/// </summary>
		public static List<Type> AllServiceTypes;

		/// <summary>
		/// A textual lookup of all services. Use Get instead. Textual key is e.g. "PageService".
		/// </summary>
		public static readonly ConcurrentDictionary<string, AutoService> AllByName = new ConcurrentDictionary<string, AutoService>();
		
		/// <summary>
		/// The lookup of services. Use Get instead.
		/// </summary>
		public static readonly ConcurrentDictionary<Type, AutoService> All = new ConcurrentDictionary<Type, AutoService>();
		
		/// <summary>
		/// A lookup specifically for AutoService implementations.
		/// </summary>
		public static readonly ConcurrentDictionary<Type, AutoService> AutoServices = new ConcurrentDictionary<Type, AutoService>();

		/// <summary>
		/// A lookup by content type ID to the autoService relating to it.
		/// </summary>
		public static readonly ConcurrentDictionary<int, AutoService> ServiceByContentType = new ConcurrentDictionary<int, AutoService>();

		/// <summary>
		/// A lookup by actual content type to the autoService relating to it.
		/// </summary>
		public static readonly ConcurrentDictionary<Type, AutoService> ServicedTypes = new ConcurrentDictionary<Type, AutoService>();

		/// <summary>
		/// The underlying service provider, used to obtain injected service instances.
		/// </summary>
		public static IServiceProvider Provider;

		/// <summary>
		/// Server type.
		/// </summary>
		/// <returns></returns>
		public static HostMapping HostMapping;

		/// <summary>
		/// Underlying host mappings.
		/// </summary>
		public static List<HostMapping> HostNameMappings;

		/// <summary>
		/// Called in an environment where the webserver is not needed or started. Registers and starts all services.
		/// Note that at least one service is expected to do some long lasting work; if no services ultimately do anything, the executable halts.
		/// </summary>
		public static void RegisterAndStart()
		{
			var collection = new ServiceCollection();
			RegisterInto(collection);
			Provider = collection.BuildServiceProvider();
			InstanceAll(Provider);
		}

		/// <summary>
		/// Sorts and then instances all services as singletons.
		/// </summary>
		public static void InstanceAll(IServiceProvider serviceProvider)
		{
			if (AllServiceTypes == null)
			{
				// Manual registration required to ensure service container is
				throw new Exception("Must register services before calling InstanceAll.");
			}

			// Next, we get *all* services so they are all instanced.
			// First they'll be sorted though so services that require loading early can do so.
			AllServiceTypes = AllServiceTypes.OrderBy(type => {

				var loadPriority = type.GetCustomAttribute<LoadPriorityAttribute>();
				if (loadPriority == null)
				{
					return 10;
				}

				return loadPriority.Priority;
			}).ToList();

			Task.Run(async () =>
			{
				foreach (var serviceType in AllServiceTypes)
				{
					var svc = serviceProvider.GetService(serviceType);

					if (svc == null)
					{
						continue;
					}

					try
					{
						// Trigger startup state change:
						await StateChange(true, svc);
					}
					catch (Exception e)
					{
						var autoService = svc as AutoService;
						Log.Error((autoService != null) ? autoService.LogTag : "", e);
					}
				}

				try
				{
					// Services are now all instanced - fire off service OnStart event:
					TriggerStart();
				}
				catch (Exception e)
				{
					Log.Error("", e);
				}

				AllServiceTypes = null;
			}).Wait();
		}

		/// <summary>
		/// Registers services into the given collection and applies the types to AllServiceTypes.
		/// At this point they are not sorted or instanced.
		/// </summary>
		/// <param name="services"></param>
		public static void RegisterInto(IServiceCollection services)
		{
			// Start checking types:
			var allTypes = typeof(Services).Assembly.DefinedTypes;

			var _serviceTypes = new List<Type>();

			foreach (var typeInfo in allTypes)
			{
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

				Log.Info("services", "Registered service: " + typeName);
			}

			AllServiceTypes = _serviceTypes;
		}

		/// <summary>
		/// Gets a mapping for the given host name or returns a default if none.
		/// </summary>
		/// <param name="hostName"></param>
		/// <returns></returns>
		public static HostMapping GetHostMapping(string hostName)
		{
			if (HostNameMappings != null)
			{
				foreach (var hostMapping in HostNameMappings)
				{
					var reg = new Regex(hostMapping.Regex, RegexOptions.IgnoreCase);

					if (reg.Match(hostName).Success)
					{
						// It's one of these!
						return hostMapping;
					}
				}
			}

			return new HostMapping()
			{
				HostType = "api",
				ShouldSync = true
			};
		}

		/// <summary>
		/// True if this server has the given host type.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public static bool HasHostType(string typeName)
		{
			// Scope for multiple
			return HostType == typeName;
		}
			
		/// <summary>
		/// True if the given host type is declared in this cluster.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public static bool IsHostTypeDefined(string typeName)
		{
			if (typeName == "api")
			{
				return true;
			}

			if (HostNameMappings != null)
			{
				foreach (var hostMapping in HostNameMappings)
				{
					if (hostMapping.HostType == typeName)
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Server type.
		/// </summary>
		/// <returns></returns>
		public static string HostType
		{
			get
			{
				return HostMapping != null ? HostMapping.HostType : "api";
			}
		}

		/// <summary>
		/// Sanitises the given environment name, handling common variants.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static string SanitiseEnvironment(string name)
		{
			name = name.ToLower().Trim();

			if (string.IsNullOrEmpty(name) || name == "dev" || name == "development")
			{
				return "dev";
			}

			if (name == "stage" || name == "staging" || name == "preprod" || name == "preproduction")
			{
				return "stage";
			}

			if (name == "prod" || name == "production" || name == "live")
			{
				return "prod";
			}

			return name;
		}

		/// <summary>
		/// True if this is the dev environment. Any of {null}, "dev" or "development" are accepted.
		/// </summary>
		/// <returns></returns>
		public static bool IsDevelopment()
		{
			return Environment == "dev";
		}

		/// <summary>
		/// True if this is the production environment. Any of "prod", "production" or "live" are accepted.
		/// </summary>
		/// <returns></returns>
		public static bool IsProduction()
		{
			return Environment == "prod";
		}

		/// <summary>
		/// True if this is the stage environment. Any of "stage" or "staging" are accepted.
		/// </summary>
		/// <returns></returns>
		public static bool IsStaging()
		{
			return Environment == "stage";
		}

		/// <summary>
		/// Gets a service by its textual interface name. Use this if you want to make a service optional and not a hard requirement for your module.
		/// </summary>
		/// <returns></returns>
		public static AutoService Get(string name)
		{
			if(name == null || name.Length == 0)
			{
				return null;
			}
			
			AllByName.TryGetValue(name.ToLower(), out AutoService result);
			return result;
		}

		/// <summary>
		/// Will either register or deregister the given service, and trigger the service's StateChange event.
		/// </summary>
		/// <param name="startup">True if starting up, false if shutting down</param>
		/// <param name="service"></param>
		public static async ValueTask StateChange(bool startup, object service)
		{
			var serviceType = service.GetType();

			var autoServiceType = GetAutoServiceType(service.GetType());
			var autoService = service as AutoService;
			
			if (startup)
			{
				// If it's an AutoService, add it to the lookups:
				if (autoService != null)
				{
					All[serviceType] = autoService;
					AllByName[
						autoService.InstanceType == null ? 
						serviceType.Name.ToLower() : 
						(autoService.InstanceType.Name.ToLower() + "service")
					] = autoService;
					
					var ctx = new Contexts.Context() {
						IgnorePermissions = true
					};

					// If it's cache only, make sure it has IDs allocated.
					if (!autoService.DataIsPersistent)
					{
						autoService.Synced = true;
					}

					await Events.Service.BeforeCreate.Dispatch(ctx, autoService);
					
					if (autoServiceType != null)
					{
						AutoServices[autoServiceType] = autoService;
					}

					if (autoService.ServicedType != null)
					{
						ServicedTypes[autoService.ServicedType] = autoService;
						var contentId = Api.Database.ContentTypes.GetId(autoService.ServicedType);
						ServiceByContentType[contentId] = autoService;
						Api.Database.ContentTypes.StateChange(true, autoService, autoService.ServicedType);
					}

					// Load the content fields. This is important to make sure e.g. ListAs is loaded and available as a global field.
					autoService.GetContentFields();

					await Events.Service.AfterCreate.Dispatch(ctx, autoService);
				}
			}
			else
			{
				// Shutdown - deregister this service.
				All.Remove(serviceType, out _);
				
				if (autoService != null)
				{
					var serviceName = autoService.InstanceType == null ? serviceType.Name.ToLower() : 
						(autoService.InstanceType.Name.ToLower() + "service");

					AllByName.Remove(serviceName, out _);

					var ctx = new Contexts.Context()
					{
						IgnorePermissions = true
					};

					await Events.Service.BeforeDelete.Dispatch(ctx, autoService);
					
					if (autoServiceType != null)
					{
						AutoServices.Remove(autoServiceType, out _);
					}

					if (autoService.ServicedType != null)
					{
						ServicedTypes.Remove(autoService.ServicedType, out _);
						var contentId = Database.ContentTypes.GetId(autoService.ServicedType);
						ServiceByContentType.Remove(contentId, out _);
						Api.Database.ContentTypes.StateChange(false, autoService, autoService.ServicedType);
					}

					await Events.Service.AfterDelete.Dispatch(ctx, autoService);
				}
			}
		}

		/// <summary>
		/// Attempts to find the AutoService type for the given type, or null if it isn't one.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static Type GetAutoServiceType(Type type)
		{

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

		/// <summary>
		/// Gets a service by the content type that it serves.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static AutoService GetByContentType(Type type)
		{
			ServicedTypes.TryGetValue(type, out AutoService result);
			return result;
		}

		/// <summary>
		/// Gets a service by the content type ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static AutoService GetByContentTypeId(int id)
		{
			ServiceByContentType.TryGetValue(id, out AutoService result);
			return result;
		}

		/// <summary>
		/// Gets a service without using injection. 
		/// Useful for when in entity classes etc.
		/// </summary>
		/// <typeparam name="T">The services interface.</typeparam>
		/// <returns></returns>
		public static T Get<T>() where T : AutoService
		{
			All.TryGetValue(typeof(T), out AutoService result);
			return (T)result;
		}

		/// <summary>
		/// Call this to trigger the OnStart event.
		/// </summary>
		public static async void TriggerStart()
		{
			Started = true;
			Provider = null;

			await Events.Service.AfterStart.Dispatch(new Contexts.Context(), null);

			if (StartupWaiter != null)
			{
				// Done! anything that was waiting for the startup waiter is now good to go.
				StartupWaiter.TrySetResult();
				StartupWaiter = null;
			}
		}

	}

	/// <summary>
	/// Host type config from "HostTypes" appsettings block.
	/// </summary>
	public class HostTypeConfig
	{
		/// <summary>
		/// Used to map hostname to a particular host type.
		/// </summary>
		public List<HostMapping> HostNameMappings { get; set; }
	}

	/// <summary>
	/// Host name mapping
	/// </summary>
	public class HostMapping
	{
		/// <summary>
		/// The hostname regex to use.
		/// </summary>
		public string Regex { get; set; }

		/// <summary>
		/// Host type e.g. "api, "huddle", "transcoder" etc.
		/// </summary>
		public string HostType { get; set; }

		/// <summary>
		/// True if this host type should sync with its cluster.
		/// </summary>
		public bool ShouldSync { get; set; } = true;
	}

}
