using Api.Eventing;
using System;
using System.Collections.Generic;
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
		/// True when AfterStart has been called.
		/// </summary>
		public static bool Started;
		/// <summary>
		/// A textual lookup of all services. Use Get instead. Textual key is e.g. "PageService".
		/// </summary>
		public static readonly Dictionary<string, object> AllByName = new Dictionary<string, object>();
		
		/// <summary>
		/// The lookup of services. Use Get instead.
		/// </summary>
		public static readonly Dictionary<Type, object> All = new Dictionary<Type, object>();
		
		/// <summary>
		/// A lookup specifically for AutoService implementations.
		/// </summary>
		public static readonly Dictionary<Type, AutoService> AutoServices = new Dictionary<Type, AutoService>();

		/// <summary>
		/// A lookup by content type ID to the autoService relating to it.
		/// </summary>
		public static readonly Dictionary<int, AutoService> ServiceByContentType = new Dictionary<int, AutoService>();

		/// <summary>
		/// A lookup by actual content type to the autoService relating to it.
		/// </summary>
		public static readonly Dictionary<Type, AutoService> ServicedTypes = new Dictionary<Type, AutoService>();

		/// <summary>
		/// The underlying service provider, used to obtain injected service instances.
		/// </summary>
		public static IServiceProvider Provider;

		/// <summary>
		/// Gets a service by its textual interface name. Use this if you want to make a service optional and not a hard requirement for your module.
		/// </summary>
		/// <returns></returns>
		public static object Get(string name)
		{
			if(name == null || name.Length == 0)
			{
				return null;
			}
			
			AllByName.TryGetValue(name, out object result);
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
				if (All.ContainsKey(serviceType))
				{
					// Already registered.
					return;
				}

				All[serviceType] = service;
				AllByName[serviceType.Name] = service;

				// If it's an AutoService, add it to the lookup:
				if (autoService != null)
				{
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
					}

					await Events.Service.AfterCreate.Dispatch(ctx, autoService);
				}
			}
			else
			{
				// Shutdown - deregister this service.
				if (!All.Remove(serviceType))
				{
					// Wasn't registered anyway.
					return;
				}


				AllByName.Remove(serviceType.Name);

				if (autoService != null)
				{
					var ctx = new Contexts.Context()
					{
						IgnorePermissions = true
					};

					await Events.Service.BeforeDelete.Dispatch(ctx, autoService);
					
					if (autoServiceType != null)
					{
						AutoServices.Remove(autoServiceType);
					}

					if (autoService.ServicedType != null)
					{
						ServicedTypes.Remove(autoService.ServicedType);
						var contentId = Database.ContentTypes.GetId(autoService.ServicedType);
						ServiceByContentType.Remove(contentId);
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
		public static T Get<T>()
		{
			All.TryGetValue(typeof(T), out object result);
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

}
