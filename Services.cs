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
		/// The lookup of services. Use Get instead.
		/// </summary>
		public static Dictionary<Type, object> All = new Dictionary<Type, object>();

		/// <summary>
		/// The underlying service provider, used to obtain injected service instances.
		/// </summary>
		public static IServiceProvider Provider;
		
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
