using Api.Eventing;
using System;
using System.Collections.Generic;

namespace Api.Startup
{
	/// <summary>
	/// Helper class for grabbing service references.
	/// </summary>
	public static class Services
	{
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
		public static Dictionary<Type, object> AutoServices = new Dictionary<Type, object>();

		/// <summary>
		/// The underlying service provider, used to obtain injected service instances.
		/// </summary>
		public static IServiceProvider Provider;

		/// <summary>
		/// Gets a service by its textual non-interface name. Use this if you want to make a service optional and not a hard requirement for your module.
		/// </summary>
		/// <typeparam name="T">The services interface.</typeparam>
		/// <returns></returns>
		public static object Get(string name)
		{
			AllByName.TryGetValue(name, out object result);
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
			Provider = null;
			await Events.ServicesAfterStart.Dispatch(null, null);
		}

	}
}
