using System.Collections.Generic;
using HtmlAgilityPack;
using System.Linq;
using Api.Translate;
using Api.Eventing;
using Api.SearchCrawler;
using System.Collections.Concurrent;
using System;
using System.Text;
using Api.Tags;
using Api.Database;
using System.Threading.Tasks;
using Nest;
using Elasticsearch.Net;
using Context = Api.Contexts.Context;
using Page = Api.Pages.Page;
using Api.Pages;
using Newtonsoft.Json;

namespace Api.SearchElastic
{
    /// <summary>
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/introduction.html
    /// 
    /// Using elastic search v8 with 7.17 client (v8 client is still wip)
    /// https://www.elastic.co/guide/en/elasticsearch/client/net-api/7.17/connecting-to-elasticsearch-v8.html
    /// 
    /// How to get the client fingerprint 
    /// https://www.elastic.co/guide/en/elasticsearch/reference/8.1/configuring-stack-security.html#_connect_clients_to_elasticsearch_5
    /// 
    /// 
    /// </summary>
    public partial class SearchElasticService : AutoService
    {
        private SearchElasticServiceConfig _cfg;
        private ElasticClient _client;
        private readonly LocaleService _locales;
        private readonly PageService _pageService;
        private readonly TagService _tagService;

        private ConcurrentDictionary<string, CrawledPageMeta> _processed = new ConcurrentDictionary<string, CrawledPageMeta>();
        private ConcurrentDictionary<string, string> _existingDocHashes = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public SearchElasticService(LocaleService locales, PageService pageService, TagService tagService)
        {
            _locales = locales;
            _pageService = pageService;
            _tagService = tagService;

            if (!IsConfigured())
            {
                return;
            }

            SetupClient();

            var setupForTypeMethod = GetType().GetMethod(nameof(SetupForType));

            // subscribe to site crawler which will extract pages for all locales
            Events.Crawler.PageCrawledNoPrimaryContent.AddEventListener(async (Context ctx, CrawledPageMeta pageMeta) =>
            {
                if (_client != null)
                {
                    var tags = await _tagService.ListBySource<Page, uint>(ctx, _pageService, pageMeta.Page.Id, "Tags", DataOptions.IgnorePermissions);

                    await ProcessPage<object>(ctx, pageMeta, tags, "", null);
                }
                return pageMeta;
            });

            // subscribe to events triggered by content types so we can add index listeners 
            Events.Service.AfterCreate.AddEventListener((Context ctx, AutoService service) =>
            {
                if (service == null)
                {
                    return new ValueTask<AutoService>(service);
                }
                // Get the content type for this service and event group:
                var servicedType = service.ServicedType;
                if (servicedType == null)
                {
                    // Things like the ffmpeg service.
                    return new ValueTask<AutoService>(service);
                }

                // Add List event:
                var setupType = setupForTypeMethod.MakeGenericMethod(new Type[] {
                    servicedType,
                    service.IdType
                });

                setupType.Invoke(this, new object[] {
                    service
                });
                return new ValueTask<AutoService>(service);
            });

            // subscribe to site crawler status change event
            Events.Crawler.CrawlerStatus.AddEventListener(async (Context ctx, SearchCrawlerStatus status) =>
            {
                if (status == SearchCrawlerStatus.Started)
                {
                    SetupClient();
                    if (_client != null)
                    {
                        GetIndexedDocuments();

                        // reset the cached list so that we can delete old content later
                        _processed = new ConcurrentDictionary<string, CrawledPageMeta>();

                        Console.WriteLine("Elastic Search - Index - Starting");
                    }
                }

                if (status == SearchCrawlerStatus.Completed)
                {
                    SetupClient();
                    if (_client != null)
                    {
                        // all done so get the content and delete any old pages
                        Purge();

                        Console.WriteLine("Elastic Search - Index - Completed");
                    }
                }

                return status;
            });
        }

        /// <summary>
        /// Handler for content types to expose content related data such as tags
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="ID"></typeparam>
        /// <param name="service"></param>
        public void SetupForType<T, ID>(AutoService<T, ID> service)
            where T : Content<ID>, new()
            where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
        {
            // If it's a mapping type, no-op.
            if (ContentTypes.IsAssignableToGenericType(typeof(T), typeof(Mapping<,>)))
            {
                return;
            }

            // Content types that can be used by the page system all appear here.    
            // Let's hook up to the PageCrawled event which tells us when a page of this primary object type (and from this service) has been crawled:
            service.EventGroup.PageCrawled.AddEventListener(async (Context ctx, CrawledPageMeta pageMeta, T po) =>
            {
                SetupClient();
                if (_client != null)
                {
                    var tags = await _tagService.ListBySource<Page, uint>(ctx, _pageService, pageMeta.Page.Id, "Tags", DataOptions.IgnorePermissions);

                    // Page crawled! The PO is the given one
                    var poTags = await _tagService.ListBySource<T, ID>(ctx, service, po.Id, "Tags", DataOptions.IgnorePermissions);

                    await ProcessPage(ctx, pageMeta, tags.Union(poTags), po.Type, po);
                }
                return pageMeta;
            });
        }

        /// <summary>
        /// Remove any orphaned docs from the search index
        /// </summary>
        private void GetIndexedDocuments()
        {
            _existingDocHashes = new ConcurrentDictionary<string, string>();

            SetupClient();
            if (_client == null)
            {
                return;
            }

            var ctx = new Context();

            // Get all the current locales:
            var locales = _locales.Where("").ListAll(ctx).Result;

            // For each locale..
            foreach (var locale in locales)
            {
                ctx.LocaleId = locale.Id;

                // get the indexed docs for the locale
                var elasticDocs = GetAllDocuments(ctx);

                foreach (var doc in elasticDocs)
                {
                    if (!string.IsNullOrWhiteSpace(doc.Hash) && !string.IsNullOrWhiteSpace(doc.CheckSum))
                    {
                        _existingDocHashes.TryAdd(doc.Hash, doc.CheckSum);
                    }
                }
            }
        }

        /// <summary>
        /// Remove any orphaned docs from the search index
        /// </summary>
        private void Purge()
        {
            if (_processed.Any())
            {
                var ctx = new Context();

                // Get all the current locales:
                var locales = _locales.Where("").ListAll(ctx).Result;

                // For each locale..
                foreach (var locale in locales)
                {
                    ctx.LocaleId = locale.Id;

                    // get the indexed docs for the locale
                    var elasticDocs = GetAllDocuments(ctx);

                    foreach (var doc in elasticDocs)
                    {
                        // purge any docs in the index we have not just processed
                        if (!string.IsNullOrWhiteSpace(doc.Id) &&
                            !string.IsNullOrWhiteSpace(doc.Hash) &&
                            !_processed.ContainsKey(doc.Hash))
                        {
                            _client.Delete(new DeleteRequest(GetIndex(ctx), doc.Id));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extract and index content from the current page
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="pageDocument"></param>
        /// <param name="tags"></param>
        /// <param name="contentType"></param>
        private async Task<bool> ProcessPage<T>(Context ctx, CrawledPageMeta pageDocument, IEnumerable<Tag> tags, string contentType, T po)
        {
            var content = new List<string>();

            // use hashset to ignore duplicate headings
            var headings = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            var page = new HtmlDocument();
            page.LoadHtml(pageDocument.BodyHtml);

            foreach (var node in page.DocumentNode.Descendants())
            {
                if (_cfg.HeaderTags.Contains(node.Name.ToLower()) && !string.IsNullOrWhiteSpace(node.InnerText))
                {
                    headings.Add(node.InnerText);
                }

                if (node.NodeType == HtmlNodeType.Text &&
                    node.ParentNode.Name != "script" &&
                    node.ParentNode.Name != "style"
                )
                {
                    content.Add(node.InnerText);
                }
            }

            var document = new Document()
            {
                Id = pageDocument.Url,
                Title = pageDocument.Page.Title,
                Url = pageDocument.Url,
                Hash = GetHash($"{ctx.LocaleId}-{pageDocument.Url}"),
                Headings = headings,
                Content = string.Join(" ", content),
                ContentType = contentType,
                Tags = tags.Select(s => s.Name),
                Keywords = string.Join(" ", tags.Select(s => s.Name)),
                PrimaryObject = po
            };

            document.CheckSum = GetHash(document);

            // has the document changed ? 
            if (_existingDocHashes.ContainsKey(document.Hash) && _existingDocHashes[document.Hash] == document.CheckSum)
            {
                Console.WriteLine($"Elastic Search - Ignoring - {ctx.LocaleId} {document.Url} {document.Hash} {document.CheckSum}");
                return true;
            }

            document.TimeStamp = DateTime.UtcNow;

            Console.WriteLine($"Elastic Search - Index - {ctx.LocaleId} {document.Url} {document.Hash} {document.CheckSum}");

            // add the document into index
            var response = await _client.IndexAsync(document, request => request.Index(GetIndex(ctx)));

            if (response.IsValid)
            {
                // keep track of the docs we have processed 
                _processed.TryAdd(document.Hash, pageDocument);
                return true;
            }
            else
            {
                Console.WriteLine($"Elastic Search - Index Failed - {ctx.LocaleId} {document.Url} {response.ServerError} {response.DebugInformation}");
            }
            return false;
        }


        /// <summary>
        /// Gets an md5 lowercase hash for the given content.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string GetHash(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private string GetHash(Document input)
        {
            // Use object to calculate MD5 hash/fingerprint
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                var jsonObject = JsonConvert.SerializeObject(input, Formatting.None);
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(jsonObject));

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private List<Document> GetAllDocuments(Context ctx)
        {
            List<Document> indexedList = new List<Document>();

            var scanResults = _client.Search<Document>(s => s
                            .Index(GetIndex(ctx))
                            .From(0)
                            .Size(1000)
                            .Source(sf => sf
                                .Includes(i => i
                                    .Fields(
                                        f => f.Url,
                                        f => f.Hash,
                                        f => f.CheckSum
                                    )
                                )
                            )
                            .Scroll("5m")
                        );

            if (scanResults != null && scanResults.Documents.Any())
            {
                var documents = scanResults?.Documents;

                while (documents.Any())
                {
                    indexedList.AddRange(documents);

                    var scrollRequest = new ScrollRequest(scanResults.ScrollId, "5m");
                    documents = _client.Scroll<Document>(scrollRequest).Documents;
                }
            }
            return indexedList;
        }

        private string GetIndex(Context ctx)
        {
            return $"{ctx.LocaleId}-{_cfg.IndexName}";
        }

        private bool IsConfigured()
        {
            _cfg = GetConfig<SearchElasticServiceConfig>();
            return !string.IsNullOrWhiteSpace(_cfg.InstanceUrl);
        }

        private bool SetupClient()
        {
            if (_client != null)
            {
                return true;
            }

            _cfg = GetConfig<SearchElasticServiceConfig>();

            // Not configured
            if (string.IsNullOrWhiteSpace(_cfg.InstanceUrl))
            {
                return false;
            }

            var pool = new SingleNodeConnectionPool(new Uri("https://localhost:9200"));

            var settings = new ConnectionSettings(pool)
                .CertificateFingerprint(_cfg.FingerPrint)
                .BasicAuthentication(_cfg.UserName, _cfg.Password)
                .EnableApiVersioningHeader();

            _client = new ElasticClient(settings);

            if (_client != null)
            {
                var ping = _client.Ping();
                if (ping != null && ping.IsValid)
                {
                    SetMappings();
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to connect to Elastic Server {ping.DebugInformation}");
                }
            }
            _client = null;
            return false;
        }

        private void SetMappings()
        {
            SetupClient();
            if (_client == null)
            {
                return;
            }

            var context = new Context();

            // Get all the current locales:
            var all_locales = _locales.Where("").ListAll(context).Result;

            // For each locale..
            foreach (var locale in all_locales)
            {
                context.LocaleId = locale.Id;

                var createIndexResponse = _client.Indices.Create(GetIndex(context), c => c
                    .Map<Document>(m => m
                    .AutoMap<Document>()
                    )
                );
            }
        }

        /// <summary>
        /// Delete all the indexes
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Reset()
        {
            SetupClient();
            if (_client == null)
            {
                return false;
            }

            var context = new Context();

            // Get all the current locales:
            var all_locales = _locales.Where("").ListAll(context).Result;

            // For each locale..
            foreach (var locale in all_locales)
            {
                context.LocaleId = locale.Id;
                await _client.Indices.DeleteAsync(GetIndex(context));
            }

            // nullify the client so the mappings are recreated
            _client = null;

            return true;
        }


        /// <summary>
        /// Perform a basic search with highlighted results for title/content
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="query"></param>
        /// <param name="from"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public async Task<DocumentsResult> Query(Context ctx, string query, string tags, string contentTypes, int from = 0, int size = 10)
        {
            SetupClient();
            if (_client == null)
            {
                return new DocumentsResult();
            }

            SearchDescriptor<Document> search;
            var filters = new List<Func<QueryContainerDescriptor<Document>, QueryContainer>>();


            if (!string.IsNullOrWhiteSpace(tags))
            {
                foreach (var tag in tags.Split(",", StringSplitOptions.RemoveEmptyEntries))
                {
                    filters.Add(fq => fq.Term(t => t.Field(f => f.Tags).Value(tag)));
                }
            }

            if (!string.IsNullOrWhiteSpace(contentTypes))
            {
                foreach (var tag in contentTypes.Split(",", StringSplitOptions.RemoveEmptyEntries))
                {
                    filters.Add(fq => fq.Term(t => t.Field(f => f.ContentType).Value(tag)));
                }
            }

            search = new SearchDescriptor<Document>()
                .Index(GetIndex(ctx))
                .Explain()
                .Query(qu => qu
                    .Bool(b => b
                        .Filter(filters)
                        .Must(must => must
                            .QueryString(qs => qs
                               .Query(query)
                               .Fields(f => f
                                    .Field(f => f.Title, 5)
                                    .Field(f => f.Headings, 3)
                                    .Field(f => f.Keywords, 2)
                                    .Field(f => f.Content)
                                )
                            )
                        )
                    )
                )
                .From(from)
                .Size(size)
                .Aggregations(ag => ag
                    .Terms("tags", term => term
                        .Field(field => field.Tags)
                    )
                )
                .Highlight(h => h
                     .Fields(f => f.Field("*"))
                    .PreTags("<strong>")
                    .PostTags("</strong>")
                );

            var response = await _client.SearchAsync<Document>(search);

            if (response.IsValid && response.Hits.Any())
            {
                return new DocumentsResult()
                {
                    Results = MapResults(response),
                    Aggregations = new List<Aggregation>()
                    {
                        new Aggregation() {
                            Name = "tags",
                            Buckets = response.Aggregations
                            .Terms("tags")
                            .Buckets
                            .Select(bucket => new Bucket() { Key = bucket.Key, Count = bucket.DocCount })
                            .OrderBy(s => s.Key)
                            .ToList()
                        }
                    }
                };
            }

            return new DocumentsResult();
        }

        private List<Document> MapResults(ISearchResponse<Document> searchResult)
        {
            var results = new List<Document>();

            var hits = searchResult.Hits
                .OrderByDescending(hit => hit.Score)
                .DistinctBy(hit => hit.Source.Url);

            foreach (var hit in hits)
            {
                var document = hit.Source;

                // Merge highlights and add the match to the result list
                var titlehighlight = string.Join(" ", hit.Highlight.Where(h => h.Key == "title").SelectMany(h => h.Value));
                if (!string.IsNullOrWhiteSpace(titlehighlight))
                {
                    document.Title = titlehighlight;
                }

                document.Highlights = string.Join(" ", hit.Highlight.Where(h => h.Key == "content").SelectMany(h => h.Value));

                document.Score = hit.Score;
                results.Add(document);

            }
            return results;
        }

    }
}

