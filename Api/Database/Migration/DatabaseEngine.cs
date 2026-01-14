using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Database;


/// <summary>
/// Holds DBE specific bindings for the purposes of migrating data between them.
/// </summary>
public class DatabaseEngine
{
	/// <summary>
	/// The DBE key.
	/// </summary>
	public string Key;

	/// <summary>
	/// The underlying service bindings.
	/// </summary>
	private Dictionary<AutoService, EventGroup> _serviceBindings;

	/// <summary>
	/// Creates a new DBE instance with the given key e.g. "mysql" or "mongodb".
	/// </summary>
	/// <param name="key"></param>
	public DatabaseEngine(string key)
	{
		Key = key;
	}

	/// <summary>
	/// The bindings in the DBE.
	/// </summary>
	public Dictionary<AutoService, EventGroup> Bindings => _serviceBindings;

	/// <summary>
	/// Gets the bound event group for the given service, or null if it is not bound.
	/// </summary>
	/// <param name="svc"></param>
	/// <returns></returns>
	public EventGroup GetBinding(AutoService svc)
	{
		_serviceBindings.TryGetValue(svc, out EventGroup result);
		return result;
	}

	/// <summary>
	/// Loads the service bindings.
	/// </summary>
	/// <param name="ctx"></param>
	/// <returns></returns>
	public async ValueTask LoadBindings(Context ctx)
	{
		_serviceBindings = new Dictionary<AutoService, EventGroup>();

		foreach (var kvp in Services.AutoServices)
		{
			var service = kvp.Value;

			if (service.ServicedType == null)
			{
				continue;
			}

			// Instance a service typed EventGroup.
			var egType = typeof(EventGroup<,>).MakeGenericType(new System.Type[] {
				service.ServicedType,
				service.IdType
			});

			var eg = (EventGroup)Activator.CreateInstance(egType);
			_serviceBindings[service] = eg;

			await Events.DatabaseMigration.LoadService.Dispatch(ctx, service, Key, eg);
		}

	}


}