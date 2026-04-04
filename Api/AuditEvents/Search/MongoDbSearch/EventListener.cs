using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Startup;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Api.AuditEvents;

/// <summary>
/// Binds MongoDB search to multiple collections based on the system config.
/// **If you aren't running on mongoDB, delete this file.**
/// </summary>
[EventListener]
public class MongoSearchEventListener
{

	/// <summary>
	/// Instanced automatically.
	/// </summary>
	public MongoSearchEventListener()
	{
		string eventCollectionName = MongoDBService.CollectionName("auditevent");
		IMongoCollection<AuditEvent> eventCollection = null;
		AuditEventService auditService = null;

		Events.AuditEvent.Search.AddEventListener(async (Context context, AuditLogSearch search) =>
		{
			if (eventCollection == null)
			{
				var dbService = Services.Get<MongoDBService>();
				var db = dbService.GetInitConnection();
				eventCollection = db.GetCollection<AuditEvent>(eventCollectionName);
			}

			if (auditService == null)
			{
				auditService = Services.Get<AuditEventService>();
			}

			int skip = search.Config.PageIndex * search.Config.PageSize;

			// Start and range always have values when the event triggers.
			var startDateUtc = search.Config.StartUtc.Value;
			var endDateUtc = startDateUtc.AddMinutes(search.Config.Range.Value);

			var createdRange = new BsonDocument
			{
				{ "$gte", startDateUtc },
				{ "$lte", endDateUtc }
			};

			// The possible content types:
			var contentTypes = await auditService.GetTypesToInclude(context);

			BsonDocument docMatch;

			if (search.Config.UserId.HasValue)
			{
				docMatch = new BsonDocument("$match",
					new BsonDocument("$and", new BsonArray
					{
						new BsonDocument("CreatedUtc", createdRange),
						new BsonDocument("UserId", search.Config.UserId.Value)
					})
				);
			}
			else
			{
				docMatch = new BsonDocument("$match", new BsonDocument("CreatedUtc", createdRange));
			}

			var pipeline = new List<BsonDocument>()
			{
				docMatch
			};

			var revisionProjection = new BsonDocument("$project", new BsonDocument
			{
				{ "_id", 1 },
				{ "Id", 1 },
				{ "CreatedUtc", 1 },
				{ "ContentId", 1 },
				{ "ActionType", 1 },
				{ "UserId", 1 }
			});

			for (var i = 0; i < contentTypes.Count; i++)
			{
				var contentType = contentTypes[i];

				// If the config specifies a type filter (it probably has one or two entries) and this type is not in it, do nothing.
				if (search.Config.ContentTypes != null)
				{
					if (search.Config.ContentTypes.Find(entry => entry == contentType.TypeName) == null)
					{
						// Not in the list - skip
						continue;
					}
				}

				// The types are cached so we can store some constant data on them if needed.
				var dbSet = contentType.MongoDbSet;

				if (dbSet == null)
				{
					dbSet = new BsonDocument("$set", new BsonDocument
					{
						{ "ContentType", contentType.RevisionTypeName }
					});
					contentType.MongoDbSet = dbSet;
				}

				var revColName = contentType.MongoDbCollection;

				if (revColName == null)
				{
					revColName = MongoDBService.CollectionName(contentType.RevisionTypeName);
					contentType.MongoDbCollection = revColName;
				}

				// Include from revisions with a filter (can repeat this n times)
				var revisionPipe = new BsonArray
				{
					docMatch,
					revisionProjection,
					dbSet
				};

				pipeline.Add(
					new BsonDocument("$unionWith", new BsonDocument
					{
						{ "coll", revColName },
						{ "pipeline", revisionPipe }
					})
				);
			}

			// Add the facets:
			pipeline.Add(
				// Collects both the total record count (totalCount) and the current page (data)
				new BsonDocument("$facet", new BsonDocument
				{
					{ "data", new BsonArray
						{
							new BsonDocument("$sort", new BsonDocument("CreatedUtc", -1)),
							new BsonDocument("$skip", skip),
							new BsonDocument("$limit", search.Config.PageSize)
						}
					},
					{ "totalCount", new BsonArray
						{
							new BsonDocument("$count", "count")
						}
					}
				})
			);

			var results = eventCollection
				.Aggregate<BsonDocument>(pipeline)
				.FirstOrDefault();

			var totalInfo = results["totalCount"].AsBsonArray;

			var total = totalInfo != null && totalInfo.Count > 0 ? totalInfo[0]["count"].AsInt32 : 0;

			var dataArray = results["data"].AsBsonArray;
			var auditEvents = dataArray
				.Select(bson => BsonSerializer.Deserialize<AuditEvent>(bson.AsBsonDocument))
				.ToList();

			search.ContentTypes = contentTypes;
			search.TotalResults = total;
			search.Events = auditEvents;
			search.Handled = true;
			
			return search;
		});
	}

}

public partial class AuditEventType
{
	/// <summary>
	/// The mongoDB collection name.
	/// </summary>
	public string MongoDbCollection;

	/// <summary>
	/// A cached $set doc.
	/// </summary>
	public BsonDocument MongoDbSet;
}
