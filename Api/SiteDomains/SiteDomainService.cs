using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Microsoft.AspNetCore.Http;
using Api.Startup;
using System.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Web;
using System.Collections.Generic;
using System;

namespace Api.SiteDomains
{
    /// <summary>
    /// Handles websites with more than one domain name. 
    /// Each user is associated with a domain when the user is created and a context extension informs 
    /// the API which domain is effectively in use.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class SiteDomainService : AutoService<SiteDomain>
    {
        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public SiteDomainService() : base(Events.SiteDomain)
        {
            InstallAdminPages("Site Domains", "fa:fa-network-wired", new string[] { "id", "name", "code", "domain" });

            Cache(new CacheConfig<SiteDomain>()
            {
                LowFrequencySequentialIds = true,
                OnCacheLoaded = OnCacheLoaded
            });


            Events.SiteDomain.AfterUpdate.AddEventListener(async (Context context, SiteDomain siteDomain) =>
            {
                await UpdateMaps();
                return siteDomain;
            });

            Events.SiteDomain.AfterCreate.AddEventListener(async (Context context, SiteDomain siteDomain) =>
            {
                await UpdateMaps();
                return siteDomain;
            });

            Events.Page.BeforeParseUrl.AddEventListener(async (Context context, Pages.UrlInfo urlInfo, Microsoft.AspNetCore.Http.QueryString query) =>
            {
                if (_multiDomain)
                {
                    if (urlInfo.Url.StartsWith("/favicon") || urlInfo.Url.StartsWith("/en-admin") || string.IsNullOrWhiteSpace(urlInfo.Host))
                    {
                        return urlInfo;
                    }

                    // Does the domain have a page prefix?
                    var siteDomain = GetByDomain(urlInfo.Host);

                    // If so, apply it now.
                    if (siteDomain != null && !string.IsNullOrWhiteSpace(siteDomain.Code) && !siteDomain.IsRoot)
                    {
                        // Act like this pagePath is in the URL all the time.
                        var strippedUrl = urlInfo.AllocateString();

                        if (string.IsNullOrWhiteSpace(strippedUrl))
                        {
                            urlInfo.Url = siteDomain.Code;
                        }
                        else
                        {
                            // if url already prefixed with domain (maybe from admin panel)
                            if (_siteDomainPrefixMap.Any(dpm => strippedUrl.StartsWith(dpm.Key + "/", StringComparison.InvariantCultureIgnoreCase)))
                            {
                                return urlInfo;
                            }

                            urlInfo.Url = siteDomain.Code + "/" + strippedUrl;
                        }

                        urlInfo.Start = 0;
                        urlInfo.Length = urlInfo.Url.Length;
                    }
                }

                return urlInfo;
            });

            Events.Context.OnLoad.AddEventListener((Context context, HttpRequest request) =>
            {
                if (_multiDomain)
                {
                    var host = request.Host.Value;

                    // Get by site domain (if any)
                    SiteDomain siteDomain = null;

                    var path = request.Path.Value;

                    if (!string.IsNullOrWhiteSpace(path) && path != "/")
                    {
                        var pathDomain = _siteDomainPrefixMap.FirstOrDefault(dpm => path.StartsWith("/" + dpm.Key + "/", StringComparison.InvariantCultureIgnoreCase));
                        if (pathDomain.Value != null)
                        {
                            siteDomain = pathDomain.Value;
                        }
                    }

                    if (siteDomain == null)
                    {
                        siteDomain = GetByDomain(host);
                    }

                    if (siteDomain == null)
                    {
                        context.SiteDomainId = 0;
                    }
                    else if (siteDomain.Id != context.SiteDomainId)
                    {
                        context.SiteDomainId = siteDomain.Id;
                    }
                }

                return new ValueTask<HttpRequest>(request);
            });
        }

        /// <summary>
        /// Runs when the cache has loaded.
        /// </summary>
        /// <returns></returns>
        private async ValueTask OnCacheLoaded()
        {
            // Initial map build:
            await UpdateMaps();
        }

        private async ValueTask UpdateMaps()
        {
            var all = await Where(DataOptions.IgnorePermissions).ListAll(new Context());

            // Domain maps Domain map can be null if it is not a multi domain site.
            var domainMap = new ConcurrentDictionary<string, SiteDomain>();
            var domainPrefixMap = new ConcurrentDictionary<string, SiteDomain>();

            foreach (var siteDomain in all)
            {
                if (!siteDomain.IsDisabled && !string.IsNullOrWhiteSpace(siteDomain.Code) && !string.IsNullOrWhiteSpace(siteDomain.Domain))
                {
                    if (siteDomain.IsPrimary)
                    {
                        domainPrefixMap[siteDomain.Code] = siteDomain;
                    }

                    domainMap[siteDomain.Domain] = siteDomain;
                }
            }

            // ensure that every domain has a code based mapping (incase the primary fields are unset)
            foreach (var map in domainMap)
            {
                if (domainPrefixMap[map.Value.Code] == null)
                {
                    domainPrefixMap[map.Value.Code] = map.Value;
                }
            }

            if (domainMap.Count == 0)
            {
                _siteDomainJson = "";
                _siteDomainMap = null;
                _siteDomainPrefixMap = null;
				_siteDomainPrefixes = "";
				_multiDomain = false;
            }
            else
            {
                var siteDomainJson = new StringBuilder();
				List <string> siteDomainPrefixes = new List<string>();
				var delimiter = "";
                foreach (var map in domainPrefixMap)
                {
					siteDomainPrefixes.Add(map.Key);
                    siteDomainJson.Append(delimiter + "\"" + map.Key + "\": { \"url\": \"" + HttpUtility.HtmlAttributeEncode(map.Value.Domain) + "\"}");
                    delimiter = ",";
                }

                _siteDomainJson = "\"sites\":{" + siteDomainJson.ToString() + "}";
                _multiDomain = true;
                _siteDomainPrefixMap = domainPrefixMap;
				_siteDomainPrefixes = string.Join(",", siteDomainPrefixes);
				_siteDomainMap = domainMap;
            }
        }

        /// <summary>
        /// Expose prrformatted json string of domains for use in page rendering
        /// </summary>
        /// <returns></returns>
        public string GetSiteDomains()
        {
            return _siteDomainJson;
        }

		/// <summary>
		/// Expose domain prefixes
		/// </summary>
		/// <returns></returns>
		public string GetSiteDomainPrefixes()
		{
			return _siteDomainPrefixes;
		}

		/// <summary>
		/// Gets domain by its case insensitive url
		/// </summary>
		/// <param name="url"></param>
		/// <returns>null if not found.</returns>
		public SiteDomain GetByUrl(string url)
        {
            if (_siteDomainMap == null || string.IsNullOrWhiteSpace(url) || url == "/")
            {
                return null;
            }

            var pathDomain = _siteDomainPrefixMap.FirstOrDefault(dpm => url.StartsWith("/" + dpm.Key + "/", StringComparison.InvariantCultureIgnoreCase));
            if (pathDomain.Value != null)
            {
                return pathDomain.Value;
            }

            return null;
        }

        /// <summary>
        /// Gets domain by its case insensitive domain and optional port. "www.mysite.co.uk" or "localhost:5050"
        /// </summary>
        /// <param name="domain"></param>
        /// <returns>null if not found.</returns>
        public SiteDomain GetByDomain(string domain)
        {
            if (_siteDomainMap == null || string.IsNullOrWhiteSpace(domain))
            {
                return null;
            }

            if (_siteDomainMap.TryGetValue(domain, out SiteDomain result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Gets the primary domain by it's short code
        /// </summary>
        /// <param name="code"></param>
        /// <returns>null if not found.</returns>
        public SiteDomain GetByCode(string code)
        {
            if (_siteDomainPrefixMap == null || string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            if (_siteDomainPrefixMap.TryGetValue(code, out SiteDomain result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Gets the current domain mappings
        /// </summary>
        /// <returns>null if not found.</returns>
        public Dictionary<string, SiteDomain> GetDomainMappings()
        {
            if (_siteDomainMap == null)
            {
                return new Dictionary<string, SiteDomain>();
            }

            return _siteDomainMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, _siteDomainMap.Comparer);
        }


        /// <summary>
        /// A mapping of domain -> prefix.
        /// </summary>
        private ConcurrentDictionary<string, SiteDomain> _siteDomainMap;

        private ConcurrentDictionary<string, SiteDomain> _siteDomainPrefixMap;

        private string _siteDomainJson = "";

		private string _siteDomainPrefixes = "";

		/// <summary>
		/// True if the domain mappings are active
		/// </summary>
		private bool _multiDomain;

    }

}
