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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Api.Payments;

/// <summary>
/// Binds MongoDB full text Atlas search, when it's available, to product searches.
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
		bool? atlasIdentityChecked = null;
		IMongoCollection<Product> productCollection = null;
		ProductSearchService searchService = null;

		string productCollectionName = MongoDBService.CollectionName("product");

		Events.Product.Search.AddEventListener(async (Context context, ProductSearch search) =>
		{

			var hasAtlas = atlasIdentityChecked;

			if (!hasAtlas.HasValue)
			{
				hasAtlas = false;

				try
				{
					// Atlas has not been checked yet.
					// This can happen repeatedly but it's fine - it's pretty fast and safe anyway.
					var dbService = Services.Get<MongoDBService>();
					var db = dbService.GetConnection();

					// Hello mongoDB! What are you?
					var helloResult = db.RunCommand<BsonDocument>(new BsonDocument("hello", 1));

					if (helloResult.TryGetValue("setName", out var setName))
					{
						if (setName.AsString.Contains("atlas"))
						{
							Log.Info("productsearch", "Atlas search enabled on '" + setName.AsString + "'");
							hasAtlas = true;
						}
						else
						{
							Log.Info("productsearch", "Atlas search disabled due to non-atlas setName: " + setName.AsString);
						}
					}
					else
					{
						Log.Info("productsearch", "MongoDB did not reply to the hello message with a setName so atlas search is disabled.");
					}
				}
				catch (MongoCommandException mce)
				{
					if (mce.CodeName == "CommandNotFound")
					{
						Log.Info("productsearch", "MongoDB did not reply to the hello message with a setName so atlas search is disabled.");
					}
					else
					{
						throw;
					}
				}
				catch (MongoException me)
				{
					Log.Warn("productsearch", me, "MongoDB atlas feature check threw an error. This is harmless but should be fixed.");
				}

				atlasIdentityChecked = hasAtlas;
			}

			if (productCollection == null)
			{
				var dbService = Services.Get<MongoDBService>();
				var db = dbService.GetConnection();
				productCollection = db.GetCollection<Product>(productCollectionName);
			}

			// passed in from product search pages
			bool logRequests = search.CustomParameters != null && search.CustomParameters.TryGetValue("logRequests", out var value2) && value2 is bool b2 && b2;

			string query = search.Query?.ToLower();

			int skip = search.PageIndex * search.PageSize;

			if (hasAtlas.Value)
			{
				// Atlas Search pipeline
				Stopwatch queryTimer = logRequests ? Stopwatch.StartNew() : null;

				if (queryTimer != null)
				{
					queryTimer.Start();
				}

				if (searchService == null)
				{
					searchService = Services.Get<ProductSearchService>();
				}

				var compoundDoc = new BsonDocument();
				var shouldArr = new BsonArray();
				var mustArr = new BsonArray();

				// Check query for any id values (admin only ?)
				BsonDocument idMatchDoc = null;
				if (search.IsAdminPanel)
				{
					List<string> potentialIds = null;
					if (!string.IsNullOrWhiteSpace(query))
					{
						potentialIds = ExtractIdCandidates(query);
					}

					// if we found a possible id in the query then boost/filter on it
					if (potentialIds != null)
					{
						var idArray = new BsonArray();
						foreach (var s in potentialIds)
						{
							if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
							{
								idArray.Add(new BsonInt64(n));
							}
						}

						if (idArray.Count > 0)
						{
							idMatchDoc = new BsonDocument("in", new BsonDocument
						{
							{ "path", "Id" },
							{ "value", idArray },
							{ "score", new BsonDocument("boost", new BsonDocument("value", 100)) }
						});

							// if expansive can just add clause, if reductive gets added in a special case later 
							if (search.SearchType == ProductSearchType.Expansive)
							{
								// as expansive can just be added into "should" list
								shouldArr.Add(idMatchDoc);
							}
						}
					}
				}

				// add text clauses if a query was provided
				if (!string.IsNullOrWhiteSpace(query))
				{
					if (search.SearchType == ProductSearchType.Expansive)
					{
						shouldArr.Add(new BsonDocument("text", new BsonDocument
						{
							{ "query", query },
							{ "path", "Name.en" },
							{ "score", new BsonDocument("boost", new BsonDocument("value", 5)) }
						}));

						// matches by desc not boosted
						shouldArr.Add(new BsonDocument("text", new BsonDocument
						{
							{ "query", query },
							{ "path", "DescriptionRaw" },
						}));
					}
					else
					{
						var tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

						foreach (var t in tokens)
						{
							var perTokenShould = new BsonArray
							{
								// Name.en boosted
								new BsonDocument("text", new BsonDocument
								{
									{ "path", "Name.en" },
									{ "query", t },
									{ "score", new BsonDocument("boost", new BsonDocument("value", 5)) }
								}),

								// matches by desc not boosted
								new BsonDocument("text", new BsonDocument {
									{ "path",  "DescriptionRaw" },
									{ "query", t }
								}),

								// matches by sku not boosted
								new BsonDocument("text", new BsonDocument {
									{ "path",  "Sku" },
									{ "query", t }
								})
							};

							// This token is required, but can land in Name OR Description
							mustArr.Add(new BsonDocument("compound", new BsonDocument {
								{ "should", perTokenShould },
								{ "minimumShouldMatch", 1 }
							}));
						}
					}
				}

				// boost featured 
				if (search.IncludeDynamicBoosts)
				{
					shouldArr.Add(new BsonDocument("equals", new BsonDocument {
						{ "path", "IsFeatured" },
						{ "value", true },
						{ "score", new BsonDocument("boost", new BsonDocument("value", 20)) }
					}));
				}

				// add any custom SHOULD clauses such as boosts for purchased products etc 
				shouldArr = await Events.Product.SearchShould.Dispatch(context, shouldArr, search);

				// add any custom MUST clauses such as hiding drafts or discontinued products
				mustArr = await Events.Product.SearchMust.Dispatch(context, mustArr, search);

				if (idMatchDoc != null && search.SearchType == ProductSearchType.Reductive)
				{
					// if we have skus then force them to appear regardless of any other criteria 
					// by created a wrapper around the existing clauses 

					// so in effect is a sku OR matches based on other filters 

					var compoundBody = new BsonDocument();

					if (shouldArr.Count > 0)
					{
						compoundBody["should"] = shouldArr;
					}

					if (mustArr.Count > 0)
					{
						compoundBody["must"] = mustArr;
					}

					BsonDocument? normalBranch = null;
					if (compoundBody.ElementCount > 0)
					{
						normalBranch = new BsonDocument("compound", compoundBody);
					}

					// combine together for OR type query 
					var topShould = new BsonArray();

					if (idMatchDoc != null)
					{
						topShould.Add(idMatchDoc);
					}

					if (normalBranch != null)
					{
						topShould.Add(normalBranch);
					}

					compoundDoc["should"] = topShould;
					compoundDoc["minimumShouldMatch"] = 1;
				}
				else
				{
					if (shouldArr.Count == 0 && mustArr.Count == 0)
					{
						// Admin panel with no query and no filters -
						// atlas requires the compound to at least contain something, so we
						// require _id to exist which it always does on MongoDB/ Atlas.
						mustArr.Add(new BsonDocument("exists", new BsonDocument("path", "_id")));
					}

					if (shouldArr.Count > 0)
					{
						compoundDoc["should"] = shouldArr;

						if (search.SearchType == ProductSearchType.Expansive)
						{
							// with a query -> force at least one text match
							// otherwise allow boosting only
							compoundDoc["minimumShouldMatch"] = string.IsNullOrEmpty(query) ? 0 : 1;
						}
					}

					if (mustArr.Count > 0)
					{
						compoundDoc["must"] = mustArr;
					}
				}


				// now set any filters and facets 
				List<BsonDocument> filterClauses = new();
				if (search.AppliedFacets != null)
				{
					foreach (var facet in search.AppliedFacets)
					{
						var ids = facet.Ids;

						if (ids == null || ids.Count == 0 || facet.Mapping == null)
						{
							continue;
						}

						var lcName = facet.Mapping.ToLower();

						if (lcName == "productcategories")
						{
							lcName = "childofcategories";
						}

						string mappingPath = "Mappings." + lcName;

						if (search.SearchType == ProductSearchType.Reductive)
						{
							foreach (var id in ids)
							{
								filterClauses.Add(new BsonDocument("equals", new BsonDocument
							{
								{ "path", mappingPath },
								{ "value", (long)id }
							}));
							}
						}
						else
						{
							var termClauses = ids.Select(id =>
								new BsonDocument("equals", new BsonDocument
								{
									{ "path", mappingPath },
									{ "value", (long)id }
								})
							);

							filterClauses.Add(new BsonDocument("compound", new BsonDocument
							{
								{ "should", new BsonArray(termClauses) },
								{ "minimumShouldMatch", 1 }
							}));
						}
					}
				}

				// TODO: There is now a property on Product called "ContinueSellingWithNoStock"
				// if this is true, then InStock filter shouldn't apply to them
				// as yes they're out of physical stock, they can still access the stock 
				// to make a sale.
				if (search.InStockOnly)
				{
					filterClauses.Add(new BsonDocument("compound", new BsonDocument
					{
						{ "must", new BsonArray
						{
							new BsonDocument("exists", new BsonDocument
							{
								{ "path", "Stock" }
							}),
							new BsonDocument("range", new BsonDocument
							{
								{ "path", "Stock" },
								{ "gt", 0 }
							})
						}
						}
					}));
				}

				// if we are in a public product search and dont want to see drafts/variants children etc 
				if (search.HideInactiveProducts)
				{
					// Sku must exist AND contain a non-empty value
					// Sku must exist and contain at least one non-whitespace character
					filterClauses.Add(new BsonDocument("compound", new BsonDocument
					{
						{ "must", new BsonArray
							{
								new BsonDocument("exists", new BsonDocument
								{
									{ "path", "Sku" }
								}),
								new BsonDocument("regex", new BsonDocument
								{
								  { "path","Sku" },
								  { "query",".+" },
								  { "allowAnalyzedField",true }
								})
							}
						}
					}));

					// ignore variants and templates
					filterClauses.Add(new BsonDocument("compound", new BsonDocument
					{
						{ "should", new BsonArray
							{
								// ParentId == 0
								new BsonDocument("equals", new BsonDocument
								{
									{ "path", "ParentId" },
									{ "value", 0 }
								}),
								// ParentId does NOT exist
								new BsonDocument("compound", new BsonDocument
								{
									{ "mustNot", new BsonArray
										{
											new BsonDocument("exists", new BsonDocument { { "path", "ParentId" } })
										}
									}
								})
							}
						},
						{ "minimumShouldMatch", 1 }
					}));

					// ignore hidden products 
					filterClauses.Add(new BsonDocument("compound", new BsonDocument
					{
						{ "mustNot", new BsonArray
							{
								new BsonDocument("equals", new BsonDocument
								{
									{ "path", "Hidden" },
									{ "value", true }
								})
							}
						}
					}));
				}

				// add any custom filters such as exclusions etc
				filterClauses = await Events.Product.SearchFilter.Dispatch(context, filterClauses, search);

				// hide any specifically passed product ids 
				if (search.ExcludedIds != null && search.ExcludedIds.Count > 0)
				{
					AddExclusionFilter(search.ExcludedIds, filterClauses);
				}

				if (filterClauses.Count > 0)
				{
					compoundDoc["filter"] = new BsonArray(filterClauses);
				}

				var searchStage = new BsonDocument
				{
					{ "index", searchService.CurrentConfig().AtlasIndex ?? "default" },
					{ "compound", compoundDoc },
					{ "returnStoredSource", true }
				};

				// the core data and facets 
				var facets = new BsonDocument
				{
					{ "products", new BsonArray
					{
						new BsonDocument("$skip", skip),
						new BsonDocument("$limit", search.PageSize),
						new BsonDocument("$project", new BsonDocument
						{
							{ "_id", 1 },
							{ "__score", 1 }
						}),
						new BsonDocument("$lookup", new BsonDocument
						{
							{ "from", productCollectionName },
							{ "localField", "_id" },
							{ "foreignField", "_id" },
							{ "as", "fullDoc" },
						}),
						new BsonDocument("$unwind", "$fullDoc"),
						new BsonDocument // computed field must be given the [ComputedSearch] attribute to ensure its mapped 
						{
							{ "$set", new BsonDocument
								{
									{ "fullDoc.Score", "$__score"},
								}
							}
						},
						new BsonDocument("$replaceRoot", new BsonDocument
						{
							{ "newRoot", "$fullDoc" }
						})
					}
					},
					{ "productcategoriesFacet", new BsonArray
						{
							new BsonDocument("$unwind", "$Mappings.childofcategories"),
							new BsonDocument("$sortByCount", "$Mappings.childofcategories")
						}
					},
					{ "attributesFacet", new BsonArray
						{
							new BsonDocument("$unwind", "$Mappings.attributes"),
							new BsonDocument("$sortByCount", "$Mappings.attributes")
						}
					}
				};

				// Build a sub-pipeline to compute min/max of the first tier price
				if (search.IncludePriceStats)
				{
					var priceStatsFacet = new BsonArray
					{
						// Safely extract PriceTiersJson[0].Amount.en -> basePrice
						new BsonDocument("$project", new BsonDocument
						{
							{ "basePrice",
								new BsonDocument("$arrayElemAt", new BsonArray
								{
									new BsonDocument("$map", new BsonDocument
									{
										{ "input", new BsonDocument("$slice", new BsonArray { "$PriceTiersJson", 1 }) }, // take first elem if any
										{ "as", "t" },
										{ "in", "$$t.Amount.en" } // becomes null if missing
									}),
									0
								})
							}
						}),
						new BsonDocument("$group", new BsonDocument
						{
							{ "_id", BsonNull.Value },
							{ "minBasePrice", new BsonDocument("$min", "$basePrice") },
							{ "maxBasePrice", new BsonDocument("$max", "$basePrice") }
						})
					};

					facets.Add("priceStats", priceStatsFacet);
				}

				// get the total
				facets.Add("totalCount", new BsonArray {
					new BsonDocument("$count", "count")
				});

				var facetStage = new BsonDocument("$facet", facets);

				var pipeline = new List<BsonDocument>
				{
					new BsonDocument("$search", searchStage),
					new BsonDocument("$set", new BsonDocument
					{
						{ "__score", new BsonDocument("$meta", "searchScore") }
					})
				};

				// add in any match rules such as on price which is tiered
				// as prices are held in tiers need to be handled differently after the initial search 
				var matchFirstTier = BuildPriceTierMatch(search.MinPrice, search.MaxPrice);
				if (matchFirstTier != null)
				{
					pipeline.Add(matchFirstTier);
				}

				if (!string.IsNullOrEmpty(search.SortOrder.Field))
				{
					if (search.SortOrder.Field == "Name.en")
					{
						pipeline.Add(new BsonDocument("$addFields", new BsonDocument("sortName",
							new BsonDocument("$toLower",
								new BsonDocument("$cond", new BsonArray
								{
									new BsonDocument("$isArray", "$Name.en"),
									new BsonDocument("$arrayElemAt", new BsonArray { "$Name.en", 0 }),
									"$Name.en"
								})
							)
						)));

						pipeline.Add(new BsonDocument(
							"$sort",
							new BsonDocument("sortName", search.SortOrder.Direction == SortDirection.ASC ? 1 : -1)
						));
					}
					else if (search.SortOrder.Field == "price")
					{
						// assuming all product have a price....even if zero (as faster)
						pipeline.Add(new BsonDocument(
							"$sort",
							new BsonDocument("PriceTiersJson.0.Amount.en", search.SortOrder.Direction == SortDirection.ASC ? 1 : -1)
						));
					}
					else if (search.SortOrder.Field == "popular")
					{
						pipeline.Add(new BsonDocument(
							"$sort",
							new BsonDocument("Popularity", search.SortOrder.Direction == SortDirection.ASC ? 1 : -1)
						));
					}
					else if (search.SortOrder.Field != "relevance")
					{
						pipeline.Add(new BsonDocument(
							"$sort",
							new BsonDocument(search.SortOrder.Field, search.SortOrder.Direction == SortDirection.ASC ? 1 : -1)
						));
					}
				}

				pipeline.Add(facetStage);

				var doc = await productCollection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();

				var products = doc["products"].AsBsonArray
					.Select(d => BsonSerializer.Deserialize<Product>(d.AsBsonDocument))
					.ToList();

				var categoryFacet = ToCategoryFacets(doc["productcategoriesFacet"].AsBsonArray);
				var attributeFacet = ToAttributeFacets(doc["attributesFacet"].AsBsonArray);

				// the total count
				int totalCount = 0;
				var countArray = doc.GetValue("totalCount", new BsonArray()).AsBsonArray;
				if (countArray.Count > 0)
				{
					totalCount = countArray[0]["count"].AsInt32;
				}

				// get the min and max prices (if part of response)
				int? minPounds = null;
				int? maxPounds = null;

				if (search.IncludePriceStats)
				{
					BsonDocument statsDoc = null;
					var statsArray = doc.GetValue("priceStats", new BsonArray()).AsBsonArray;
					if (statsArray.Count > 0 && statsArray[0].IsBsonDocument)
					{
						statsDoc = statsArray[0].AsBsonDocument;

						if (statsDoc != null)
						{
							double? minPence = null;
							double? maxPence = null;

							if (statsDoc.TryGetValue("minBasePrice", out var minVal) && !minVal.IsBsonNull && minVal.IsNumeric)
							{
								minPence = minVal.ToDouble();
							}

							if (statsDoc.TryGetValue("maxBasePrice", out var maxVal) && !maxVal.IsBsonNull && maxVal.IsNumeric)
							{
								maxPence = maxVal.ToDouble();
							}

							minPounds = minPence.HasValue ? (int)Math.Floor(minPence.Value / 100d) : null;
							maxPounds = maxPence.HasValue ? (int)Math.Ceiling(maxPence.Value / 100d) : null;
						}
					}
				}

				search.Products = products;
				search.Handled = true;

				search.Total = totalCount;

				search.ResultFacets = new ProductSearchFacets()
				{
					Categories = categoryFacet,
					Attributes = attributeFacet,
					Prices = new List<ProductPriceFacet>() {
						new ProductPriceFacet() { Key = "min" , Value = minPounds },
						new ProductPriceFacet() { Key = "max" , Value = maxPounds }
					}
				};

				if (queryTimer != null)
				{
					queryTimer.Stop();
					Log.Info("ProductSearch", $"Search query [[{query}]] returned {totalCount} in {queryTimer.ElapsedMilliseconds}ms");
				}

			}
			else
			{
				// Non-atlas search - doesn't perform fulltext search or return facets.
				// Basic facets are instead derived from the (paginated) returned product set only
				// (that happens in ProductSearchService).

				// Escape special regex chars so your query is safe
				var escaped = string.IsNullOrEmpty(query) ? "" : Regex.Escape(query);

				// Add .* around it for "contains" match
				var pattern = $".*{escaped}.*";

				var filter = Builders<Product>.Filter.Or(
					Builders<Product>.Filter.Regex("Name.en", new BsonRegularExpression(pattern, "i")),
					Builders<Product>.Filter.Regex("DescriptionRaw", new BsonRegularExpression(pattern, "i"))
				);

				var childFilters = new List<FilterDefinition<Product>>();

				if (search.AppliedFacets != null)
				{
					foreach (var facet in search.AppliedFacets)
					{
						var ids = facet.Ids;

						if (ids == null || ids.Count == 0 || facet.Mapping == null)
						{
							continue;
						}

						if (facet.Mapping.ToLower() == "productcategories")
						{
							childFilters.Add(Builders<Product>.Filter.In("Mappings.childofcategories", new BsonArray(ids)));
						}
						else
						{
							childFilters.Add(Builders<Product>.Filter.In("Mappings." + facet.Mapping.ToLower(), new BsonArray(ids)));
						}
					}
				}

				// TODO: There is now a property on Product called "ContinueSellingWithNoStock"
				// if this is true, then InStock filter shouldn't apply to them
				// as yes they're out of physical stock, they can still access the stock 
				// to make a sale.
				if (search.InStockOnly)
				{
					childFilters.Add(
						Builders<Product>.Filter.And(
							Builders<Product>.Filter.Exists("Stock", true),
							Builders<Product>.Filter.Ne("Stock", 0)
						)
					);
				}

				if (search.HideInactiveProducts)
				{
					// ignore variants and templates
					childFilters.Add(
						Builders<Product>.Filter.And(
						Builders<Product>.Filter.Exists("ParentId", true),
						Builders<Product>.Filter.Eq("ParentId", 0)
						)
					);

					// ignore hidden products 
					childFilters.Add(
						Builders<Product>.Filter.Ne("Hidden", true)
					);

					// Sku must exist AND contain a non-empty value
					// Sku must exist and contain at least one non-whitespace character
					childFilters.Add(
						Builders<Product>.Filter.And(
						Builders<Product>.Filter.Exists("Sku", true),
						Builders<Product>.Filter.Ne("Sku", "")
						)
					);
				}

				// prices 
				if (search.MinPrice.HasValue)
				{
					childFilters.Add(
						Builders<Product>.Filter.And(
							Builders<Product>.Filter.Exists("PriceTiersJson.0.Amount.en", true),
							Builders<Product>.Filter.Gte("PriceTiersJson.0.Amount.en", search.MinPrice.Value)
						)
					);
				}

				if (search.MaxPrice.HasValue)
				{
					childFilters.Add(
						Builders<Product>.Filter.And(
							Builders<Product>.Filter.Exists("PriceTiersJson.0.Amount.en", true),
							Builders<Product>.Filter.Lte("PriceTiersJson.0.Amount.en", search.MaxPrice.Value)
						)
					);
				}

				// add any custom filters such as exclusions etc
				if (Events.Product.BasicSearchFilter.HasListeners())
				{
					childFilters = await Events.Product.BasicSearchFilter.Dispatch(context, childFilters, search);
				}

				// hide any specifically passed product ids 
				if (search.ExcludedIds != null && search.ExcludedIds.Count > 0)
				{
					childFilters.Add(
						Builders<Product>.Filter.And(
							Builders<Product>.Filter.Not(Builders<Product>.Filter.In("Id", new BsonArray(search.ExcludedIds)))
						)
					);
				}

				if (childFilters.Count > 0)
				{
					childFilters = childFilters.Prepend(filter).ToList();
					filter = Builders<Product>.Filter.And(childFilters);
				}

				Task<List<Product>> productsTask;
				var field = search.SortOrder.Field;

				if (string.IsNullOrEmpty(field) || field == "relevance")
				{
					// Non-atlas search doesn't support relevance so this is just the default lack of sorting.
					productsTask = productCollection
						.Find(filter)
						.Skip(skip)
						.Limit(search.PageSize)
						.ToListAsync();
				}
				else
				{
					if (field == "price")
					{
						field = "PriceTiersJson.0.Amount.en";
					}

					if (field == "popular")
					{
						field = "Popularity";
					}

					var sortBuilder = Builders<Product>.Sort;

					SortDefinition<Product> sortDefinition;

					var direction = search.SortOrder.Direction == SortDirection.ASC;

					sortDefinition = direction
						? sortBuilder.Ascending(field)
						: sortBuilder.Descending(field);

					productsTask = productCollection
						.Find(filter)
						.Sort(sortDefinition)
						.Skip(skip)
						.Limit(search.PageSize)
						.ToListAsync();
				}

				// get totals 
				var totalTask = productCollection.CountDocumentsAsync(filter);

				Task<Product> minPriceDocTask = null;
				Task<Product> maxPriceDocTask = null;

				if (search.IncludePriceStats)
				{
					// limit min/max re products that actually have a price
					var pricePath = "PriceTiersJson.0.Amount.en";
					var priceExists = Builders<Product>.Filter.Exists(pricePath, true);

					minPriceDocTask = productCollection
						.Find(filter & priceExists)
						.Sort(Builders<Product>.Sort.Ascending(pricePath))
						.Limit(1)
						.FirstOrDefaultAsync();

					maxPriceDocTask = productCollection
						.Find(filter & priceExists)
						.Sort(Builders<Product>.Sort.Descending(pricePath))
						.Limit(1)
						.FirstOrDefaultAsync();
				}

				if (search.IncludePriceStats)
				{
					await Task.WhenAll(productsTask, totalTask, minPriceDocTask, maxPriceDocTask);
				}
				else
				{
					await Task.WhenAll(productsTask, totalTask);
				}

				var products = await productsTask;
				var total = await totalTask;

				int? minPounds = null;
				int? maxPounds = null;

				if (search.IncludePriceStats && total > 0)
				{
					if (minPriceDocTask != null && minPriceDocTask.Result != null && minPriceDocTask.Result.PriceTiers != null)
					{
						var minPence = (double)minPriceDocTask.Result.PriceTiers.FirstOrDefault().Amount.GetFallback();
						minPounds = (int)Math.Floor(minPence / 100d);
					}

					if (maxPriceDocTask != null && maxPriceDocTask.Result != null && maxPriceDocTask.Result.PriceTiers != null)
					{
						var maxPence = (double)maxPriceDocTask.Result.PriceTiers.FirstOrDefault().Amount.GetFallback();
						maxPounds = (int)Math.Ceiling(maxPence / 100d);
					}
				}

				search.Total = (int)total;
				search.Products = products;
				search.Handled = true;

				search.ResultFacets = new ProductSearchFacets()
				{
					Prices = new List<ProductPriceFacet>() {
						new ProductPriceFacet() { Key = "min" , Value = minPounds },
						new ProductPriceFacet() { Key = "max" , Value = maxPounds }
					}
				};

			}
			return search;
		});
	}

	/// <summary>
	/// Add a mongo search filter for enforcing that a product is NOT within a list of ids
	/// </summary>
	/// <param name="ids"></param>
	/// <param name="filters"></param>
	private void AddExclusionFilter(IEnumerable<uint> ids, List<BsonDocument> filters)
	{
		if (ids == null || ids.Count() == 0)
		{
			return;
		}

		// only return items whose Id is NOT in the list of ids
		var exclusionClause = new BsonDocument("compound", new BsonDocument
		{
			{ "mustNot", new BsonArray
				{
					new BsonDocument("in", new BsonDocument
					{
						{ "path", "Id" },
						{ "value", new BsonArray(ids) }
					})
				}
			}
		});

		filters.Add(exclusionClause);
	}



	// as prices are held in tiers need to be handled differently after the initial search 
	private BsonDocument BuildPriceTierMatch(double? minPrice, double? maxPrice)
	{
		if (!minPrice.HasValue && !maxPrice.HasValue)
		{
			return null;
		}

		var amountPath = "PriceTiersJson.0.Amount.en"; // first element only
		var range = new BsonDocument();
		if (minPrice.HasValue && minPrice.Value > 0)
		{
			range["$gte"] = minPrice.Value;
		}
		if (maxPrice.HasValue && maxPrice.Value > 0)
		{
			range["$lte"] = maxPrice.Value;
		}

		return new BsonDocument("$match", new BsonDocument(amountPath, range));
	}

	private List<AttributeValueFacet> ToAttributeFacets(BsonArray values)
	{
		var vals = new List<AttributeValueFacet>();

		foreach (var bsonVal in values)
		{
			var id = bsonVal["_id"].AsInt64;
			var count = bsonVal["count"].AsInt32;

			vals.Add(new AttributeValueFacet()
			{
				AttributeValueId = (uint)id,
				Count = count
			});
		}

		return vals;
	}

	private List<ProductCategoryFacet> ToCategoryFacets(BsonArray values)
	{
		var vals = new List<ProductCategoryFacet>();

		foreach (var bsonVal in values)
		{
			var id = bsonVal["_id"].AsInt64;
			var count = bsonVal["count"].AsInt32;

			vals.Add(new ProductCategoryFacet()
			{
				ProductCategoryId = (uint)id,
				Count = count
			});
		}

		return vals;
	}

	static readonly Regex NumberRegex = new Regex(@"(?<![+\-.\w])\d+(?![+\-.\w])", RegexOptions.Compiled); // whole-number tokens
	static List<string> ExtractIdCandidates(string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return null;
		}

		List<string> result = null;

		foreach (var m in NumberRegex.EnumerateMatches(query))
		{
			var token = query.Substring(m.Index, m.Length);

			if (string.IsNullOrWhiteSpace(token))
			{
				continue;
			}

			if (result == null)
			{
				result = new List<string>()
				{
					token.Trim()
				};
			}
			else
			{
				result.Add(token.Trim());
			}
		}

		return result;
	}


}
