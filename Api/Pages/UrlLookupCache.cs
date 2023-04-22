using Api.ColourConsole;
using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.Pages
{

	/// <summary>
	/// Caches information about URLs on the site in order to resolve a URL quickly for a given role.
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
		/// Gets the root node.
		/// </summary>
		public UrlLookupNode Root => rootPage;

		/// <summary>
		/// The 404 page. Its url is always /404
		/// </summary>
		public Page NotFoundPage;

		/// <summary>
		/// List of page URLs and their associated page ID.
		/// </summary>
		public List<PageIdAndUrl> PageUrlList = new List<PageIdAndUrl>();

		private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

		/// <summary>
		/// Add a redirect to the cache.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="redirectTo"></param>
		/// <returns></returns>
		public UrlLookupNode Add(string url, Func<UrlInfo, UrlLookupNode, List<string>, ValueTask<string>> redirectTo)
		{
			var node = AddInternal(url, null, out int index);

			if (node != null && index != -1)
			{
				node.Terminals[index].Redirection = redirectTo;
			}

			return node;
		}

		/// <summary>
		/// Adds the given url to the cache, returning the node that it ended at and the index as well.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="page"></param>
		/// <param name="index">The index in the UrlLookupNode's page array that it was added to.</param>
		private UrlLookupNode AddInternal(string url, Page page, out int index)
		{
			if (string.IsNullOrEmpty(url))
			{
				index = -1;
				return null;
			}

			var tokenSet = new List<PageUrlToken>();

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
							tokenSet.Add(new PageUrlToken()
							{
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
                                    WriteColourLine.Warning("[WARN] Bad page URL using a type that doesn't exist. It was " + url + " using type " + contentType);
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

						if (pg == rootPage && part == "en-admin")
						{
							// This is admin root.
							next.IsAdmin = true;
						}

						if (pg.IsAdmin)
						{
							// Carry admin state.
							next.IsAdmin = true;
						}

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
					index = -1;
					return null;
				}
			}

			if (pg == null)
			{
				index = -1;
				return null;
			}

			var prepend = page != null && page.PreferIfLoggedIn;

			if (pg.Terminals == null)
			{
				pg.Terminals = new UrlLookupTerminal[1];
			}
			else
			{
				var newPgSet = new UrlLookupTerminal[pg.Terminals.Length + 1];
				Array.Copy(pg.Terminals, 0, newPgSet, prepend ? 1 : 0, pg.Terminals.Length);
				pg.Terminals = newPgSet;
			}

			var tokenNames = tokenSet.Select(token => token.RawToken).ToList();
			string tokenNamesJson;

			if (tokenNames == null || tokenNames.Count == 0)
			{
				tokenNamesJson = "null";
			}
			else
			{
				tokenNamesJson = Newtonsoft.Json.JsonConvert.SerializeObject(tokenNames, jsonSettings);
			}

			var terminal = new UrlLookupTerminal()
			{
				Page = page,
				UrlTokens = tokenSet,
				UrlTokenNames = tokenNames,
				UrlTokenNamesJson = tokenNamesJson
			};

			// If there are >1, ensure that the page at the start of the set is the one that is favoured (if any are favoured).
			if (prepend)
			{
				index = 0;
				pg.Terminals[0] = terminal;
			}
			else
			{
				index = pg.Terminals.Length - 1;
				pg.Terminals[index] = terminal;
			}
			
			return pg;
		}

		/// <summary>
		/// Adds the given page to the cache.
		/// </summary>
		/// <param name="page"></param>
		public UrlLookupNode Add(Page page)
		{

			if (page == null)
			{
				return null;
			}

			if (page.Url == "/404")
			{
				NotFoundPage = page;
			}

			var pg = AddInternal(page.Url, page, out int index);

			if (pg == null)
			{
				// skipped
				return null;
			}

			PageUrlList.Add(new PageIdAndUrl()
			{
				PageId = page.Id,
				Url = page.Url
			});

			return pg;
		}

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
				Add(page);
			}

		}

		/// <summary>
		/// Gets the page to use for the given URL.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="urlInfo"></param>
		/// <param name="searchQuery">Optional, Including the ? at the start</param>
		/// <returns></returns>
		public async ValueTask<PageWithTokens> GetPage(Context context, UrlInfo urlInfo, Microsoft.AspNetCore.Http.QueryString searchQuery)
		{
			var curNode = rootPage;

			if (curNode == null)
			{
				return new PageWithTokens()
				{
					StatusCode = 404,
					Page = null,
					TokenValues = null,
					TokenNamesJson = "null"
				};
			}

			List<string> wildcardTokens = null;

			if (urlInfo.Length > 0)
			{
				var parts = urlInfo.AllocateString().Split('/');

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
							StatusCode = 404,
							Page = null,
							TokenValues = null,
							TokenNamesJson = "null"
						};
					}

					curNode = nextNode;
				}
			}

			var index = -1;
			var terminals = curNode.Terminals;

			if (terminals == null)
			{
				// 404
				return new PageWithTokens()
				{
					StatusCode = 404,
					Page = null,
					TokenValues = null,
					TokenNamesJson = "null"
				};
			}

			// There will always be at least one terminal if the array is not null.
			object primaryObject = null;
			var notFoundCount = 0;
			var termCount = terminals.Length;

			// Next, we'll perform permission testing.
			// The login URL is always permitted no matter what.
			if (urlInfo.Matches("login"))
			{
				// Always permitted (assuming it exists!)
				// This helps avoid any redirect cycles in the event the user is unable to see either the login or homepage.

				// Use terminal 0 in this situ - the login page can't vary.
				index = 0;
			}
			else
			{
				var role = context.Role;

				if (role == null)
				{
					role = Roles.Public;
				}

				var pageLoadCapability = Events.Page.GetLoadCapability();

				for (var i = 0; i < termCount; i++)
				{
					var terminal = curNode.Terminals[i];
					var pageToTest = terminal.Page;

					// terminals with no page (usually a redirect node)
					// are always granted.
					if (pageToTest == null || await role.IsGranted(pageLoadCapability, context, pageToTest, false))
					{
						// If there is a primary object, ensure it exists.
						// If not, it's like this page doesn't exist at all.

						if (terminal.UrlTokens != null && wildcardTokens != null && wildcardTokens.Count > 0)
						{
							var countA = terminal.UrlTokens.Count;

							var primaryToken = terminal.UrlTokens[countA - 1];

							// The actual value from the URL:
							var primaryTokenValue = wildcardTokens[wildcardTokens.Count - 1];

							if (primaryToken.ContentType != null)
							{
								if (primaryToken.IsId)
								{
									if (ulong.TryParse(primaryTokenValue, out ulong primaryObjectId))
									{
										primaryObject = await primaryToken.Service.GetObject(context, primaryObjectId);
									}
								}
								else
								{
									var filterString = "";

									for (var j = 0; j < countA; j++)
									{
										if (j != 0)
										{
											filterString += " and ";
										}

										var fieldName = terminal.UrlTokens[j].FieldName;
										filterString += fieldName + "=?";
									}

									primaryObject = await primaryToken.Service.GetObjectByFilter(context, filterString, wildcardTokens);
								}

								if (!curNode.IsAdmin && primaryObject == null)
								{
									// Exclude admin pages because of the /add URL which has no primary object but must still route.
									// The contentType is not null meaning this is a content specific frontend URL but the content referenced does not exist.
									// Therefore, we can safely generate the 404 page instead.
									notFoundCount++;
									continue;
								}
							}
						}

						index = i;
						break;
					}
				}
			}

			if (index == -1)
			{
				if (notFoundCount == termCount)
				{
					// All pages rejected it as a 404.
					return new PageWithTokens()
					{
						StatusCode = 404,
						Page = null,
						TokenValues = null,
						TokenNamesJson = "null"
					};
				}

				// The user was rejected on a permission error.
				// This can often lead to a redirect to the login page, but we'll make the handling of 
				// this project specific such that it can e.g. redirect to a subscribe page or whatever it would like to do.
				return new PageWithTokens()
				{
					StatusCode = 302,
					Page = null,
					TokenValues = null,
					TokenNamesJson = "null",
					RedirectTo = "/login?then=" +
						System.Web.HttpUtility.UrlEncode(searchQuery.HasValue ? urlInfo.Url + searchQuery.Value : urlInfo.Url)
				};
			}

			// A node and index has been selected.
			var redir = terminals[index].Redirection;
			if (redir != null)
			{
				var targetUrl = await redir(urlInfo, curNode, wildcardTokens);

				if (targetUrl == null)
				{
					// 404
					return new PageWithTokens()
					{
						StatusCode = 404,
						Page = null,
						TokenValues = null,
						TokenNamesJson = "null"
					};
				}

				return new PageWithTokens()
				{
					StatusCode = 302,
					RedirectTo = targetUrl,
					TokenNamesJson = "null"
				};
			}

			var page = terminals[index].Page;

			if (page == null)
			{
				// Seemingly empty terminal - 404
				return new PageWithTokens()
				{
					StatusCode = 404,
					Page = null,
					TokenValues = null,
					TokenNamesJson = "null"
				};
			}

			return new PageWithTokens()
			{
				StatusCode = 200,
				Page = page,
				PrimaryObject = primaryObject,
				Tokens = terminals[index].UrlTokens,
				TokenNames = terminals[index].UrlTokenNames,
				TokenNamesJson = terminals[index].UrlTokenNamesJson,
				TokenValues = wildcardTokens,
				Multiple = terminals.Length > 1
			};
		}
	}

	/// <summary>
	/// URL lookup information.
	/// </summary>
	public struct UrlInfo
	{
		/// <summary>
		/// The URL. Start and length can be used to indicate the URL lookup engine should ignore some part of this string.
		/// </summary>
		public string Url;
		/// <summary>
		/// Defaults to 0. Set this to avoid allocating substrings.
		/// </summary>
		public int Start;
		/// <summary>
		/// Defaults to Url.Length. Set this to avoid allocating substrings.
		/// </summary>
		public int Length;
		/// <summary>
		/// The host domain of the current request.
		/// </summary>
		public string Host;
		/// <summary>
		/// Optionally request a redirect to the given URL. It will be a 302/ non-permanent.
		/// </summary>
		public string RedirectTo;

		/// <summary>
		/// True if the substring identified by this UrlInfo matches the given text (exactly, case sensitive).
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public bool Matches(string text)
		{
			if (text == null || text.Length != Length)
			{
				return false;
			}

			for (var i = 0; i < Length; i++)
			{
				if (text[i] != Url[Start + i])
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Allocates a lowercased substring
		/// </summary>
		/// <returns></returns>
		public string ToLower()
		{
			if (Url == null)
			{
				return null;
			}

			if (Length == Url.Length)
			{
				return Url.ToLower();
			}

			return string.Create(Length, this, (Span<char> span, UrlInfo info) => {

				for (var i = 0; i < info.Length; i++)
				{
					span[i] = char.ToLower(info.Url[info.Start + i]);
				}

			});
		}
		
		/// <summary>
		/// Allocates a substring
		/// </summary>
		/// <returns></returns>
		public string AllocateString()
		{
			if (Url == null)
			{
				return null;
			}

			if (Length == Url.Length)
			{
				return Url;
			}

			return Url.Substring(Start, Length);
		}

		/// <summary>
		/// A substring which also lowercases to avoid a double allocation.
		/// </summary>
		/// <returns></returns>
		public string LowercaseSubstring(int start, int length)
		{
			if (Url == null)
			{
				return null;
			}

			start += Start;

			if (length > Length)
			{
				length = Length;
			}

			if (length == Url.Length && start == 0)
			{
				return Url.ToLower();
			}

			return string.Create(length, this, (Span<char> span, UrlInfo info) => {

				for (var i = 0; i < length; i++)
				{
					span[i] = char.ToLower(info.Url[start + i]);
				}

			});
		}
	}

	/// <summary>
	/// A page and token values from the URL.
	/// </summary>
	public struct PageWithTokens
	{
		/// <summary>
		/// The primary object for this page.
		/// </summary>
		public object PrimaryObject;
		/// <summary>
		/// Custom set the http status code.
		/// </summary>
		public int StatusCode;
		/// <summary>
		/// The tokens associated with the page itself.
		/// </summary>
		public List<PageUrlToken> Tokens;
		/// <summary>
		/// The tokens associated with the page itself (just their names).
		/// </summary>
		public List<string> TokenNames;
		/// <summary>
		/// The token names as preformatted JSON. Can be the string "null".
		/// </summary>
		public string TokenNamesJson;
		/// <summary>
		/// The page.
		/// </summary>
		public Page Page;
		/// <summary>
		/// Any token values in the URL.
		/// </summary>
		public List<string> TokenValues;
		/// <summary>
		/// Set if this is a redirection (to the given URL, as a 302).
		/// </summary>
		public string RedirectTo;
		/// <summary>
		/// True if there are multiple variants of a page and it shouldn't get cached
		/// </summary>
		public bool Multiple;
	}

	/// <summary>
	/// A leaf node in the URL lookup tree.
	/// </summary>
	public partial struct UrlLookupTerminal
	{
		/// <summary>
		/// Set if this node performs a redirect.
		/// </summary>
		public Func<UrlInfo, UrlLookupNode, List<string>, ValueTask<string>> Redirection;
		/// <summary>
		/// Page, if there is one.
		/// </summary>
		public Page Page;
		/// <summary>
		/// If this node has a page associated with it, this is the set of url tokens. The primary object is always derived from the last one.
		/// </summary>
		public List<PageUrlToken> UrlTokens;
		/// <summary>
		/// If this node has a page associated with it, this is the set of url tokens. The primary object is always derived from the last one. This is just the names only.
		/// </summary>
		public List<string> UrlTokenNames;
		/// <summary>
		/// Preformatted JSON array of the url token names. ["A", "B", ..]. will be the string "null" if it is null.
		/// </summary>
		public string UrlTokenNamesJson;
	}

	/// <summary>
	/// A node in the URL lookup tree.
	/// </summary>
	public partial class UrlLookupNode
	{
		/// <summary>
		/// Usually only has one page in it, but multiple pages on the same URL can happen with e.g. the homepage, if there are permission based matchings.
		/// </summary>
		public UrlLookupTerminal[] Terminals;
		/// <summary>
		/// True if this node (or any of its parents) are /en-admin/.
		/// </summary>
		public bool IsAdmin;
		/// <summary>
		/// The wildcard resolver if there is one for this node. Same as Children["*"].
		/// </summary>
		public UrlLookupNode Wildcard;
		/// <summary>
		/// The child nodes.
		/// </summary>
		public Dictionary<string, UrlLookupNode> Children = new Dictionary<string, UrlLookupNode>(StringComparer.OrdinalIgnoreCase);

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