using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Api.Pages
{

	/// <summary>
	/// Caches information about URLs on the site in order to aid with generating a URL for a particular piece of content.
	/// </summary>
	public partial class UrlLookupCache
	{
		/// <summary>
		/// </summary>
		public UrlLookupCache()
		{
		}

		private UrlLookupNode rootPage = new UrlLookupNode();

		/// <summary>
		/// The 404 page. Its url is always /404
		/// </summary>
		public Page NotFoundPage;

		/// <summary>
		/// List of page URLs and their associated page ID.
		/// </summary>
		public List<PageIdAndUrl> PageUrlList = new List<PageIdAndUrl>();

		/// <summary>
		/// Loads the cache from the given list of all pages.
		/// </summary>
		/// <param name="allPages"></param>
		public void Load(List<Page> allPages)
		{
			// Loop over pages and establish which scope it belongs to:
			for (var p = 0; p < allPages.Count; p++)
			{
				var page = allPages[p];

				if (page == null)
				{
					continue;
				}

				var url = page.Url;

				if (string.IsNullOrEmpty(url))
				{
					continue;
				}

				var tokenSet = new List<PageUrlToken>();

				if (url == "/404")
				{
					NotFoundPage = page;
				}

				if (url.Length != 0 && url[0] == '/')
				{
					url = url.Substring(1);
				}

				if (url.Length != 0 && url[url.Length - 1] == '/')
				{
					url = url.Substring(0, url.Length - 1);
				}

				// URL parts:

				var pg = rootPage;

				if (url.Length != 0)
				{
					var parts = url.Split('/');
					var skip = false;

					for (var i = 0; i < parts.Length; i++)
					{

						var part = parts[i];
						string token = null;

						if (part.Length != 0)
						{
							if (part[0] == ':')
							{
								token = part.Substring(1);
								tokenSet.Add(new PageUrlToken() {
									RawToken = token
								});
							}
							else if (part[0] == '{')
							{
								token = (part[part.Length - 1] == '}') ? part.Substring(1, part.Length - 2) : part.Substring(1);

								var dotIndex = token.IndexOf('.');

								if (dotIndex != -1)
								{
									var contentType = token.Substring(0, dotIndex);
									var fieldName = token.Substring(dotIndex + 1);
									var type = Api.Database.ContentTypes.GetType(contentType);

									if (type == null)
									{
										Console.WriteLine("[WARN] Bad page URL using a type that doesn't exist. It was page " + page.Id + " using type " + contentType);
										skip = true;
										break;
									}

									var service = Api.Startup.Services.GetByContentType(type);

									tokenSet.Add(new PageUrlToken()
									{
										RawToken = token,
										TypeName = contentType,
										FieldName = fieldName,
										ContentType = type,
										Service = service,
										IsId = fieldName.ToLower() == "id",
										ContentTypeId = Api.Database.ContentTypes.GetId(type)
									});
								}
								else
								{
									tokenSet.Add(new PageUrlToken()
									{
										RawToken = token
									});
								}
								
							}
						}
						
						if (token != null)
						{
							// Anything. Treat these tokens as *:
							part = "*";
						}

						if (!pg.Children.TryGetValue(part, out UrlLookupNode next))
						{
							pg.Children[part] = next = new UrlLookupNode();

							if (token != null)
							{
								// It's the wildcard one:
								pg.Wildcard = next;
							}
						}

						pg = next;
					}

					if (skip)
					{
						continue;
					}
				}

				pg.Page = page;
				pg.UrlTokens = tokenSet;
				pg.UrlTokenNames = tokenSet.Select(token => token.RawToken).ToList();

				PageUrlList.Add(new PageIdAndUrl()
				{
					PageId = page.Id,
					Url = url
				});

			}

		}

		/// <summary>
		/// Gets the page to use for the given URL.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public PageWithTokens GetPage(string url)
		{
			url = url.Split('?')[0].Trim();
			if (url[0] == '/')
			{
				url = url.Substring(1);
			}

			if (url.Length != 0 && url[url.Length - 1] == '/')
			{
				url = url.Substring(0, url.Length - 1);
			}

			var curNode = rootPage;

			if (curNode == null)
			{
				return new PageWithTokens()
				{
					Page = null,
					TokenValues = null
				};
			}

			List<string> wildcardTokens = null;

			if (url.Length != 0)
			{
				var parts = url.Split('/');

				for (var i = 0; i < parts.Length; i++)
				{
					if (!curNode.Children.TryGetValue(parts[i], out UrlLookupNode nextNode))
					{
						nextNode = curNode.Wildcard;

						if (nextNode != null)
						{
							// Using a wildcard node. Add token value to set:
							if (wildcardTokens == null)
							{
								wildcardTokens = new List<string>();
							}

							wildcardTokens.Add(parts[i]);
						}
					}

					if (nextNode == null)
					{
						// 404
						return new PageWithTokens()
						{
							Page = null,
							TokenValues = null
						};
					}

					curNode = nextNode;
				}
			}

			return new PageWithTokens()
			{
				Page = curNode.Page,
				Tokens = curNode.UrlTokens,
				TokenNames = curNode.UrlTokenNames,
				TokenValues = wildcardTokens
			};
		}
	}

	/// <summary>
	/// A page and token values from the URL.
	/// </summary>
	public struct PageWithTokens
	{
		/// <summary>
		/// The tokens associated with the page itself.
		/// </summary>
		public List<PageUrlToken> Tokens;
		/// <summary>
		/// The tokens associated with the page itself (just their names).
		/// </summary>
		public List<string> TokenNames;
		/// <summary>
		/// The page.
		/// </summary>
		public Page Page;
		/// <summary>
		/// Any token values in the URL.
		/// </summary>
		public List<string> TokenValues;
	}

	/// <summary>
	/// A node in the URL lookup tree.
	/// </summary>
	public partial class UrlLookupNode
	{
		/// <summary>
		/// If this node has a page associated with it, this is the set of url tokens. The primary object is always derived from the last one.
		/// </summary>
		public List<PageUrlToken> UrlTokens;
		/// <summary>
		/// If this node has a page associated with it, this is the set of url tokens. The primary object is always derived from the last one. This is just the names only.
		/// </summary>
		public List<string> UrlTokenNames;
		/// <summary>
		/// The page
		/// </summary>
		public Page Page;
		/// <summary>
		/// The wildcard resolver if there is one for this node. Same as Children["*"].
		/// </summary>
		public UrlLookupNode Wildcard;
		/// <summary>
		/// The child nodes.
		/// </summary>
		public Dictionary<string, UrlLookupNode> Children = new Dictionary<string, UrlLookupNode>();

	}

	/// <summary>
	/// A particular {urltoken} with the content type and field ref loaded.
	/// </summary>
	public partial class PageUrlToken
	{

		/// <summary>
		/// The raw token value.
		/// </summary>
		public string RawToken;

		/// <summary>
		/// The type name.
		/// </summary>
		public string TypeName;

		/// <summary>
		/// The field name.
		/// </summary>
		public string FieldName;

		/// <summary>
		/// The content type.
		/// </summary>
		public Type ContentType;

		/// <summary>
		/// True if this id the ID field.
		/// </summary>
		public bool IsId;

		/// <summary>
		/// The service for the content type (if there is one).
		/// </summary>
		public AutoService Service;

		/// <summary>
		/// The field/ property info.
		/// </summary>
		public MemberInfo FieldOrProperty;

		/// <summary>
		/// THe ID of the content type.
		/// </summary>
		public int ContentTypeId;
	}


}