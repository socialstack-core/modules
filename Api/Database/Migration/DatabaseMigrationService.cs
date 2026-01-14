using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using Stripe;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Database;


/// <summary>
/// Used to migrate data from one DBE to another. Instanced automatically.
/// </summary>
public class DatabaseMigrationService : AutoService
{
	private Dictionary<string, DatabaseEngine> _engines;

	/// <summary>
	/// Loads a DBE. The engine name is lowercase and e.g. "mysql" or "mongodb".
	/// </summary>
	/// <param name="engineName"></param>
	/// <returns></returns>
	private async ValueTask<DatabaseEngine> GetEngine(string engineName)
	{
		engineName = engineName.Trim().ToLower();

		if (_engines == null)
		{
			_engines = new Dictionary<string, DatabaseEngine>();
		}
		else if (_engines.TryGetValue(engineName, out DatabaseEngine dbe))
		{
			return dbe;
		}

		var engine = new DatabaseEngine(engineName);
		await engine.LoadBindings(new Contexts.Context(1,1,1));
		_engines[engineName] = engine;
		return engine;
	}

	/// <summary>
	/// Migrates every content type from the source engine to the target engine.
	/// If one is provided, the typeFilter is executed on each type and 
	/// you can omit things from migrating by returning false.
	/// This makes the assumption that the target is empty - it simply loops over each item in source and creates it in the target.
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="typeFilter"></param>
	/// <returns></returns>
	public async ValueTask Migrate(string from, string to, Func<Type, bool> typeFilter = null)
	{
		Log.Info("ssmf", "SSMF (SocialStack Migration Framework) starting..");

		// Get both engines:
		var source = await GetEngine(from);
		var target = await GetEngine(to);

		var migrateTypeMethod = GetType().GetMethod(nameof(MigrateContent));

		foreach (var kvp in source.Bindings)
		{
			var service = kvp.Key;

			if (typeFilter != null)
			{
				if (!typeFilter(service.ServicedType))
				{
					Log.Info("ssmf", service.ServicedType + " skipped for migration by user provided type filter");
					continue;
				}
			}

			var sourceEvents = kvp.Value;
			var targetEvents = target.GetBinding(kvp.Key);

			if (targetEvents == null)
			{
				continue;
			}

			// This type is being migrated. Let's elevate in to a type specific context:
			var migrate = migrateTypeMethod.MakeGenericMethod(new Type[] {
				service.ServicedType,
				service.IdType
			});

			var vt = (ValueTask)migrate.Invoke(this, new object[] {
				service,
				sourceEvents,
				targetEvents
			});

			await vt;
		}

		Log.Info("ssmf", "Migration finished");
	}

	/// <summary>
	/// Migrates a specific content type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	/// <param name="service"></param>
	/// <param name="source"></param>
	/// <param name="target"></param>
	public async ValueTask MigrateContent<T, ID>(AutoService<T, ID> service, EventGroup<T, ID> source, EventGroup<T, ID> target)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		var ctx = new Context(1, 1, 1);

		// A filter for "everything" is..
		var filterA = service.Where("");
		var filterB = service.Where("");

		Log.Info("ssmf", "Copying all '" + service.EntityName + "' entities to target..");

		var queryPair = new QueryPair<T, ID>()
		{
			QueryA = filterA,
			QueryB = filterB,
			SrcA = null,
			SrcB = null,
			OnResult = async (Context resultCtx, T result, int index, object resultSrc, object resultSrcB) => {

				// brute force technique here - rows stream from the source and are created 1 at a time in the target.
				// if you're dealing with a very large set, you'll want to
				// block create them by buffering them up as a group of e.g. 10k rows and use the bulk create mechanism.
				await target.Create.Dispatch(ctx, result);

			}
		};

		await source.List.Dispatch(ctx, queryPair);

		filterA.Release();
		filterB.Release();
	}

}