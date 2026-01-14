using Api.Database;
using System.Threading.Tasks;
using Api.Eventing;
using MongoDB.Driver;
using Api.Startup;

namespace Api.Counters
{

	/// <summary>
	/// Provides atomic counters
	/// MongoDB required.
	/// </summary>
	public partial class CounterService : AutoService<Counter>
	{
		private IMongoCollection<Counter> _counterCollection;

		/// <summary>
		///	Provides atomic counters
		/// MongoDB required.
		/// </summary>
		public CounterService() : base(Events.Counter)
		{
		}

		/// <summary>
		/// Returns the formatted id, e.g. "WEB-00123".
		/// </summary>
		public async ValueTask<string> GetCounter(string key, string prefix = "WEB", int padWidth = 5, string separator = "-")
		{
			// Get next numeric value (atomic)
			var seq = await GetNextSequence(key);

			if (!string.IsNullOrWhiteSpace(prefix))
			{
				return $"{prefix}{separator}{seq.ToString($"D{padWidth}")}";
			}
			else
			{
				return $"{seq.ToString($"D{padWidth}")}";
			}
		}

		private async ValueTask<long> GetNextSequence(string key)
		{
			var dbService = Services.Get<MongoDBService>();

			if (_counterCollection == null)
			{
				var counterCollectionName = MongoDBService.CollectionName("counter");
				var db = dbService.GetConnection();
				_counterCollection = db.GetCollection<Counter>(counterCollectionName);
			}

		var filter = Builders<Counter>.Filter.Eq(c => c._id, key);
		
		var update = Builders<Counter>.Update
			.Inc(c => c.Sequence, 1)
			.SetOnInsert(c => c._id, key)
			.SetOnInsert(c => c.Id, await dbService.AssignId(this));

		var options = new FindOneAndUpdateOptions<Counter>
		{
			ReturnDocument = ReturnDocument.After,
			IsUpsert = true
		};

		var result = await _counterCollection.FindOneAndUpdateAsync(filter, update, options);
		return result.Sequence;
		}
	}
}

