using Api.Eventing;
using System;
using System.Collections.Generic;
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
		/// A textual lookup of all services. Use Get instead. Textual key is e.g. "IPageService".
		/// </summary>
		public static Dictionary<string, object> AllByName = new Dictionary<string, object>();
		
		/// <summary>
		/// The lookup of services. Use Get instead.
		/// </summary>
		public static Dictionary<Type, object> All = new Dictionary<Type, object>();
		
		/// <summary>
		/// A lookup specifically for AutoService implementations.
		/// </summary>
		public static Dictionary<Type, AutoService> AutoServices = new Dictionary<Type, AutoService>();

		/// <summary>
		/// A lookup by content type ID to the autoService relating to it.
		/// </summary>
		public static Dictionary<int, AutoService> ContentTypes = new Dictionary<int, AutoService>();

		/// <summary>
		/// A lookup by actual content type to the autoService relating to it.
		/// </summary>
		public static Dictionary<Type, AutoService> ServicedTypes = new Dictionary<Type, AutoService>();

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
			
			if(name[0] != 'I'){
				// Convenience - this is specifically for interfaces, so just in case somebody asks for a service by its full name:
				name = "I" + name;
			}
			
			AllByName.TryGetValue(name, out object result);
			return result;
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
			ContentTypes.TryGetValue(id, out AutoService result);
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
			await Events.ServicesAfterStart.Dispatch(null, null);
		}

	}
}
