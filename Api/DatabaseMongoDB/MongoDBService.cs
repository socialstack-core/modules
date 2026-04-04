using Api.Configuration;
using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Database;

/// <summary>
/// MongoDB database service.
/// Connects to a database with the given connection string.
/// </summary>
[LoadPriority(1)]
public partial class MongoDBService : AutoService
{

	/// <summary>
	/// The table name to use for a particular type.
	/// This is generally used on types which are DatabaseRow instances.
	/// </summary>
	public static string CollectionName(string entityName)
	{
		// Just prefixed (e.g. site_product by default):
		var name = AppSettings.DatabaseTablePrefix + entityName.ToLower();

		if (name.Length > 120)
		{
			name = name.Substring(0, 120);
		}

		return name;
	}

	/// <summary>
	/// Returns a connection string, or null, if it isn't configured.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public static ConnectionString GetConfiguredConnectionString()
	{
		return Api.Database.ConnectionString.Get("Mongo");
	}

	/// <summary>
	/// The connection string to use.
	/// </summary>
	public string ConnectionString { get; set; }

	/// <summary>
	/// The latest DB schema.
	/// </summary>
	public Schema Schema { get; set; }

	/// <summary>
	/// Create a new database connector with the given connection string.
	/// </summary>
	public MongoDBService() {
		// Load from appsettings and add a change handler.
		LoadFromAppSettings();

		AppSettings.OnChange += () => {
			LoadFromAppSettings();
		};

		Events.Healthz.RunChecks.AddEventListener(async (Context context, HealthzChecks checks) => {

			var mongoOk = false;
			string mongoMessage = null;

			if (string.IsNullOrWhiteSpace(ConnectionString))
			{
				mongoMessage = "Mongo connection string not configured";
			}
			else
			{
				mongoOk = await PingAsync();
				if (!mongoOk)
				{
					mongoMessage = "MongoDB ping failed";
				}
			}

			checks.mongo = new { ok = mongoOk, message = mongoMessage };

			if (!mongoOk)
			{
				checks.Ok = false;
			}

			return checks;
		});
	}

	/// <summary>
	/// Indicates the connection string should be loaded or reloaded.
	/// </summary>
	private void LoadFromAppSettings()
	{
		var cs = GetConfiguredConnectionString();
		ConnectionString = cs == null ? null : cs.ConnectionConfig;

		_client = null;
		_database = null;
	}

	private MongoClient _client;
	private IMongoDatabase _database;
	private IMongoDatabase _initdatabase;

	/// <summary>
	/// Gets a shared mongoDB client for connections to a particular database, specified by the ConnectionString.
	/// </summary>
	/// <returns></returns>
	internal IMongoDatabase GetConnection()
	{
		var db = _database;

		if (db == null)
		{
			var settings = MongoClientSettings.FromConnectionString(ConnectionString);
			settings.ConnectTimeout = TimeSpan.FromSeconds(3);
			settings.SocketTimeout = TimeSpan.FromSeconds(3);
			settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
			settings.MaxConnectionPoolSize = 100;
			var client = new MongoClient(settings);
			var mongoUrl = new MongoUrl(ConnectionString);
			db = client.GetDatabase(mongoUrl.DatabaseName);
			_client = client;
			_database = db;
		}

		return db;
	}

	/// <summary>
	/// Special connection used by init to allow for index creation and slower tasks 
	/// </summary>
	/// <returns></returns>
	internal IMongoDatabase GetInitConnection()
	{
		var db = _initdatabase;

		if (db == null)
		{
			var settings = MongoClientSettings.FromConnectionString(ConnectionString);
			settings.ConnectTimeout = TimeSpan.FromSeconds(3);
			settings.SocketTimeout = TimeSpan.FromMinutes(15);
			settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
			var client = new MongoClient(settings);
			var mongoUrl = new MongoUrl(ConnectionString);
			db = client.GetDatabase(mongoUrl.DatabaseName);
			_initdatabase = db;
		}
		return db;
	}

	private DateTime _lastCounterCacheTime;
	private Dictionary<string, MongoDBCounter> _counterCache;

	/// <summary>
	/// Adds a counter to the counter set if it is necessary to do so.
	/// </summary>
	/// <param name="counters"></param>
	/// <param name="collectionName"></param>
	/// <returns></returns>
	public async ValueTask AddCounterIfNecessary(IMongoCollection<MongoDBCounter> counters, string collectionName)
	{
		var now = DateTime.UtcNow;

		if (_counterCache == null || (now - _lastCounterCacheTime).TotalSeconds > 600)
		{
			// Setup counter cache
			_lastCounterCacheTime = now;

			var allCounters = await counters.Find(Builders<MongoDBCounter>.Filter.Empty).ToListAsync();

			var cc = new Dictionary<string, MongoDBCounter>();

			foreach (var counter in allCounters)
			{
				cc.TryAdd(counter._id, counter);
			}

			_counterCache = cc;
		}

		if (_counterCache.ContainsKey(collectionName))
		{
			// Don't try to add it.
			return;
		}

		try
		{
			await counters.InsertOneAsync(new MongoDBCounter()
			{
				_id = collectionName,
				AutoInc = 0
			});
		}
		catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
		{
			// Some other server was starting up at the same time and added it - this is fine.
			// We'll just assume the counter cache is now stale.
			_counterCache = null;
		}
	}
	
	/// <summary>
	/// Use this to pre-assign an ID. Can be used during BeforeCreate to get the ID ahead of when it would otherwise be set.
	/// </summary>
	/// <returns></returns>
	public async ValueTask<ID> AssignId<T, ID>(AutoService<T, ID> service) where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		var entityName = service.EntityName;
		var link = GetConnection();
		var counters = link.GetCollection<MongoDBCounter>("counters");
		var collectionName = CollectionName(entityName);

		// This call isn't needed - the service is already started up and thus has called this already.
		// await AddCounterIfNecessary(counters, collectionName);

		var counterFilter = Builders<MongoDBCounter>.Filter.Eq(c => c._id, collectionName);
		var counterUpdateBuilder = Builders<MongoDBCounter>.Update;
		var counterUpdate = counterUpdateBuilder.Inc(c => c.AutoInc, 1);

		var options = new FindOneAndUpdateOptions<MongoDBCounter>
		{
			IsUpsert = true,
			ReturnDocument = ReturnDocument.After
		};

		// Assign an ID:
		var counter = await counters.FindOneAndUpdateAsync(counterFilter, counterUpdate, options);
		var newId = counter.AutoInc;

		// Map this ID via the svc:
		return service.ConvertId((ulong)newId);
	}

	/// <summary>
	/// Gets a list of results from the cache, calling the given callback each time one is discovered.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="queryPair">Both filterA and filterB must have values.</param>
	/// <param name="collection"></param>
	public async ValueTask<int> GetResults<T, ID, INSTANCE_TYPE>(
		Context context, QueryPair<T, ID> queryPair, IMongoCollection<INSTANCE_TYPE> collection
	)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		where INSTANCE_TYPE : T
	{
		string localeCode = null;
		if (context != null && context.LocaleId > 1)
		{
			var localeId = context.LocaleId;
			var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ? ContentTypes.Locales[localeId - 1] : null);
			localeCode = locale?.Code;
		}

		if (localeCode == null)
		{
			localeCode = "en";
		}

		var srcA = queryPair.SrcA;
		var srcB = queryPair.SrcB;
		var onResult = queryPair.OnResult;
		var includeTotal = queryPair.QueryA == null ? false : queryPair.QueryA.IncludeTotal;

		int total = 0;

		// FilterA is the user filter. It provides things like the collector and some contextual args.
		var filterA = queryPair.QueryA;

		// FilterB is the permission system filter. Like filterA it can be null.
		var filterB = queryPair.QueryB;

		FilterDefinition<INSTANCE_TYPE> filterDefA = filterA == null ? null : filterA.ToMongo<INSTANCE_TYPE>(
			localeCode,
			context,
			filterA
		);

		FilterDefinition<INSTANCE_TYPE> filterDefB = filterB == null ? null : filterB.ToMongo<INSTANCE_TYPE>(
			localeCode,
			context,
			filterA // Contextual functionality originate from the user filter, not the permission one here. 
		);

		FilterDefinition<INSTANCE_TYPE> filter;

		if (filterDefA == null)
		{
			if (filterDefB == null)
			{
				filter = Builders<INSTANCE_TYPE>.Filter.Empty;
			}
			else
			{
				filter = filterDefB;
			}
		}
		else if (filterDefB == null)
		{
			filter = filterDefA;
		}
		else
		{
			filter = Builders<INSTANCE_TYPE>.Filter.And(filterDefA, filterDefB);
		}

		if (includeTotal)
		{
			var count = await collection.CountDocumentsAsync(filter);
			total = (int)count;
		}

		var index = 0;

		var findOptions = new FindOptions<INSTANCE_TYPE>
		{
			Limit = (filterA.PageSize > 0 ? filterA.PageSize : null),
			Skip = (filterA.Offset > 0 ? filterA.Offset : 0),
			Collation = new Collation(localeCode, false, CollationCaseFirst.Off, CollationStrength.Secondary)
		};

		if (filterA.SortField != null)
		{
			findOptions.Sort = filterA.SortAscending
			? Builders<INSTANCE_TYPE>.Sort.Ascending(filterA.SortField.Name)
			: Builders<INSTANCE_TYPE>.Sort.Descending(filterA.SortField.Name);
		}

		var cursor = await collection.FindAsync(filter, findOptions);

		while (await cursor.MoveNextAsync())
		{
			var result = cursor.Current;

			foreach (var item in result)
			{
				await onResult(context, item, index, srcA, srcB);
				index++;
			}
		}

		return total;
	}

}

/// <summary>
/// A counter in mongoDB.
/// </summary>
public class MongoDBCounter
{
	/// <summary>
	/// The collection ID.
	/// </summary>
	public string _id { get; set; }

	/// <summary>
	/// The current latest value.
	/// </summary>
	public long AutoInc { get; set; }
}