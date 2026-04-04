using MongoDB.Bson;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Database;

public partial class MongoDBService
{
	/// <summary>
	/// Pings the MongoDB database to verify connectivity.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>True if the database is reachable and responds successfully; otherwise, false.</returns>
	public async ValueTask<bool> PingAsync(CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(ConnectionString))
		{
			return false;
		}

		try
		{
			var db = GetConnection();
			await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
			
			return true;
		}
		catch
		{
			return false;
		}
	}
}
