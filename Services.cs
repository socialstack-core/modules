using Api.Eventing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
		/// </summary>
		public static string Environment;
		/// <summary>
		/// True when AfterStart has been called.
		/// </summary>
		public static bool Started;
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
		/// True if this is the dev environment. Any of {null}, "dev" or "development" are accepted.
		/// </summary>
		/// <returns></returns>
		public static bool IsDevelopment()
		{
			return string.IsNullOrEmpty(Environment) || Environment == "dev" || Environment == "development";
		}

		/// <summary>
		/// True if this is the production environment. Any of "prod", "production" or "live" are accepted.
		/// </summary>
		/// <returns></returns>
		public static bool IsProduction()
		{
			return Environment == "prod" || Environment == "production" || Environment == "live";
		}

		/// <summary>
		/// True if this is the stage environment. Any of "stage" or "staging" are accepted.
		/// </summary>
		/// <returns></returns>
		public static bool IsStaging()
		{
			return Environment == "stage" || Environment == "staging";
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
				// If it's an AutoService, add it to the lookup:
				if (autoService != null)
				{
					All[serviceType] = autoService;
					AllByName[serviceType.Name.ToLower()] = autoService;
					
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
				AllByName.Remove(serviceType.Name.ToLower(), out _);

				if (autoService != null)
				{
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
		private static Type GetAutoServiceType(Type type)
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
