using Api.AutoForms;
using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Startup;
using Api.Startup.Routing;
using Api.Translate;
using Api.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Api.Pages
{
	/// <summary>
	/// Handles permalinks. Very similar concept to how wordpress works here: these are like
	/// URL aliases which are retained such that you can minimise link breakage when content changes.
	/// Unlike redirects which are handled by the webserver, permalinks are silent aliases but do have a concept of 
	/// canonical links: when multiple permalinks target the same thing, the most recently created permalink is the 
	/// canonical one. Requests that arrived via the old ones will be redirected.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(9)]
	public partial class PermalinkService : AutoService<Permalink>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PermalinkService(PageService pages, PageGroupService pageGroups) : base(Events.Permalink)
        {
			InstallAdminPages("Permalinks", "fa:fa-link", new string[] { "url", "target" });

			Events.Permalink.BeforeUpdate.AddEventListener(async (Context context, Permalink toUpdate, Permalink orig) => {
				if (toUpdate == null)
				{
					return toUpdate;
				}
				
				if (toUpdate.Url != orig.Url)
				{
					throw new PublicException("Permalink URLs cannot be edited", "permalink/is-permanent");
				}

				if (toUpdate.Target != orig.Target && toUpdate.Target != "404")
				{
					if (toUpdate.Target != null)
					{
						toUpdate.Target = toUpdate.Target.Trim().ToLower();
					}

					// If the target has changed, we need to make sure this permalinks CreatedUtc is actually before
					// the current canonical permalink for the same target.
					var currentCanonical = await GetCanonical(context, toUpdate.Target);

					if (currentCanonical != null && currentCanonical.Id != orig.Id && currentCanonical.CreatedUtc < toUpdate.CreatedUtc)
					{
						// Only creation of a permalink can change the canonical order.
						// Push its created date to before the canonical one.
						toUpdate.CreatedUtc = currentCanonical.CreatedUtc.AddYears(-1);
					}
				}

				return toUpdate;
			});

			Events.Permalink.BeforeCreate.AddEventListener(async (Context context, Permalink permalink) => {

				if(string.IsNullOrEmpty(permalink.Url) || permalink.Url[0] != '/' || permalink.Url.StartsWith("http://") || permalink.Url.StartsWith("https://") || permalink.Url.StartsWith("://"))
				{
					throw new PublicException("Permalink URL must be a valid path starting with /", "permalink/url_invalid");
				}

				permalink.Url = permalink.Url.Trim();

				// Does this permalink exist?
				// If so, fail. The generate command disambiguates internally so it won't generate collisions.
				var exists = await Where("Url=?", DataOptions.IgnorePermissions).Bind(permalink.Url).First(context);

				if (exists != null)
				{
					throw new PublicException("Permalink URL already exists (its ID is #" + exists.Id + "). Edit the target of the existing one instead.", "permalink/url_exists");
				}

				return permalink;
			});

			Events.Permalink.AfterCreate.AddEventListener((Context context, Permalink link) => {
				_srcDictionary = null;
				Router.RequestRebuild();

				return new ValueTask<Permalink>(link);
			});

			Events.Permalink.AfterDelete.AddEventListener((Context context, Permalink link) => {
				_srcDictionary = null;
				Router.RequestRebuild();

				return new ValueTask<Permalink>(link);
			});

			Events.Permalink.AfterUpdate.AddEventListener((Context context, Permalink link, ChangedFields changes) => {
				_srcDictionary = null;
				Router.RequestRebuild();

				return new ValueTask<Permalink>(link);
			});

			Events.Page.BeforeCreate.AddEventListener(async (Context context, Page page) =>
			{
				if (string.IsNullOrEmpty(page.Url) && page.PageGroupId == 0 && string.IsNullOrEmpty(page.Key))
				{
					throw new PublicException("A url or group is required. If you're making a homepage, use /", "page_url_required");
				}

				if (page.PageGroupId != 0)
				{
					// Group always dictates the URL (currently)
					var group = await pageGroups.Get(context, page.PageGroupId);

					if (group == null)
					{
						throw new PublicException("Page group does not exist", "group/not_found");
					}

					var prefix = group.Url;

					if (string.IsNullOrWhiteSpace(prefix))
					{
						prefix = "/";
					}
					else
					{
						prefix = prefix.Trim();
						if (prefix[0] != '/')
						{
							prefix = "/" + prefix;
						}
					}

					if (prefix[prefix.Length - 1] != '/')
					{
						prefix += "/";
					}

					var titleSlug = GenerateNormalizedSlug(page.Title.GetFallback());
					page.Url = prefix + titleSlug;
				}

				return page;
			});

			Events.Page.AfterCreate.AddEventListener(async (Context context, Page page) =>
			{
				// Create a permalink targeting this page:
				if (!string.IsNullOrEmpty(page.Url))
				{
					string target = null;
					
					if (!string.IsNullOrEmpty(page.Key))
					{
						if (PageKeyIsPrimary(page.Key, out AutoService svc, out bool isAdminGroup, out string specificContentId))
						{
							// The page key is pointing at primary content, so the permalink should do so as well.
							// Page keys are a superset, so we'll need to construct a new target locator.
							target = CreatePrimaryTargetLocator(svc.ServicedType, specificContentId, isAdminGroup);
						}
					}

					if (target == null)
					{
						target = "page:" + page.Id;
					}

					// If page.Url exists already as a permalink, delete the permalink. Permalinks are, as the name suggests, immutable.
					var existing = await Where("Url=?", DataOptions.IgnorePermissions).Bind(page.Url).First(context);

					if (existing != null)
					{
						await Delete(context, existing, DataOptions.IgnorePermissions);
					}

					await Create(context, new Permalink()
					{
						Url = page.Url,
						Target = target,
					}, DataOptions.IgnorePermissions);
				}

				Router.RequestRebuild();

				return page;
			});

			Events.Page.AfterUpdate.AddEventListener((Context context, Page page, ChangedFields diff) =>
			{
				// Need to update the two caches. We'll just wipe them for now:
				Router.RequestRebuild();

				return new ValueTask<Page>(page);
			});

			Events.Page.AfterDelete.AddEventListener(async (Context context, Page page) =>
			{
				if (page == null)
				{
					return null;
				}

				// Delete permalinks:
				await DeleteLinksTo(context, page);

				// Need to update the two caches. We'll just wipe them for now:
				Router.RequestRebuild();

				return page;
			});

			Events.Page.Received.AddEventListener((Context context, Page page, int mode) => {

				// Doesn't matter what the change was - we'll wipe the caches.
				Router.RequestRebuild();

				return new ValueTask<Page>(page);
			});
		
			Events.Router.CollectRoutes.AddEventListener(async (Context context, RouterBuilder builder) => {

				// Get the 404 page:
				var notFoundPage = await pages.Where("Key=?", DataOptions.IgnorePermissions).Bind("404").First(context);

				if (notFoundPage != null)
				{
					builder.Status_404 = new PageTerminalBehaviour(notFoundPage, null, null);
				}

				// Collect all permalinks and add them as rewrite routes.
				var permalinkSet = await GetSourcesByTarget(context);

				// A lookup by content type.
				Dictionary<Type, PrimaryUrlLookup> primaryLookup = new Dictionary<Type, PrimaryUrlLookup>();

				foreach (var kvp in permalinkSet)
				{
					// The target node in the router is..
					var target = kvp.Key;

					// The sources for that target are..
					var sources = kvp.Value;

					if (target == null)
					{
						continue;
					}

					var addedPage = false;

					if (target.StartsWith("primary:") || target.StartsWith("admin_primary:"))
					{
						addedPage = true;

						if (TargetIsPrimaryLocator(target, out AutoService pTargetService, out bool pTargetIsAdmin, out string pTargetContentId))
						{
							var getNode = builder.GetGetNode();

							if (sources.Count > 0)
							{
								var explicitPageId = sources[0].PageId;

								// Pages are cached so we can ask for it here without a time penalty.
								var page = explicitPageId == 0 ? 
									await pages.Where("Key=?", DataOptions.IgnorePermissions).Bind(target).First(context) :
									await pages.Get(context, explicitPageId, DataOptions.IgnorePermissions);

								if (page == null && pTargetContentId != null)
								{
									//  Try fallback locator (e.g. primary:user)
									var fallback = CreatePrimaryTargetLocator(pTargetService.ServicedType, null, pTargetIsAdmin);
									page = await pages.Where("Key=?", DataOptions.IgnorePermissions).Bind(fallback).First(context);
								}

								if (page != null)
								{
									var linkUrl = sources[0].Url;

									// (mandatory on these targets)
									if (PageKeyIsPrimary(page.Key, out AutoService _, out bool _, out string pageSpecificContentId))
									{
										var specificContentId = pTargetContentId;

										if (specificContentId == null)
										{
											// Currently from the key lookup this won't happen,
											// but it's considered for fancier page key matching later.
											specificContentId = pageSpecificContentId;
										}

										if (!pTargetIsAdmin)
										{
											if (!primaryLookup.TryGetValue(pTargetService.ServicedType, out PrimaryUrlLookup urlLookup))
											{
												urlLookup = pTargetService.CreatePrimaryUrlLookup();
												primaryLookup[pTargetService.ServicedType] = urlLookup;
											}

											ulong specificId = 0;

											if (specificContentId != null)
											{
												ulong.TryParse(specificContentId, out specificId);
											}

											urlLookup.Add(linkUrl, specificId);
										}

										if (linkUrl != null && !linkUrl.Contains('?'))
										{
											// Sources with query strings are only added to the reverse lookup.
											var newTerminal = new PageTerminalBehaviour(page, pTargetService, specificContentId);

											// Any custom handling for this terminal can run here.
											// For example, revisions checks the Page.PrimaryContentIsRevisions flag here.
											await Events.Permalink.BeforeAddTerminal.Dispatch(context, newTerminal);

											getNode.AddCustomBehaviour(linkUrl, newTerminal);
										}
									}
								}
							}
						}
					}
					else if (target.StartsWith("page:"))
					{
						addedPage = true;

						var getNode = builder.GetGetNode();

						if (sources.Count > 0 && uint.TryParse(target.Substring(5), out uint pageId))
						{
							// Pages are cached so we can ask for it here without a time penalty.
							var page = await pages.Get(context, pageId, DataOptions.IgnorePermissions);

							if (page != null)
							{
								var linkUrl = sources[0].Url;

								// Is the page primary content of some kind?
								if (PageKeyIsPrimary(page.Key, out AutoService primaryContentService, out bool isAdminGroup, out string specificContentId))
								{
									if (!isAdminGroup)
									{
										if (!primaryLookup.TryGetValue(primaryContentService.ServicedType, out PrimaryUrlLookup urlLookup))
										{
											urlLookup = primaryContentService.CreatePrimaryUrlLookup();
											primaryLookup[primaryContentService.ServicedType] = urlLookup;
										}

										ulong specificId = 0;

										if (specificContentId != null)
										{
											ulong.TryParse(specificContentId, out specificId);
										}

										urlLookup.Add(linkUrl, specificId);
									}
								}
								else if (!string.IsNullOrEmpty(page.PrimaryContentType))
								{
									// Obtain the relevant service:
									primaryContentService = Services.Get(page.PrimaryContentType + "Service");
								}

								if (linkUrl != null && !linkUrl.Contains('?'))
								{
									// Sources with query strings are only added to the reverse lookup.
									var newTerminal = new PageTerminalBehaviour(page, primaryContentService, specificContentId);

									// Any custom handling for this terminal can run here.
									// For example, revisions checks the Page.PrimaryContentIsRevisions flag here.
									await Events.Permalink.BeforeAddTerminal.Dispatch(context, newTerminal);

									getNode.AddCustomBehaviour(linkUrl, newTerminal);
								}
							}
						}
					}

					for(var i=0;i<sources.Count;i++)
					{
						var src = sources[i];

						if (src != null && src.Url != null && src.Url.Contains('?'))
						{
							// Sources with query strings are only added to the reverse lookup.
							continue;
						}

						if (i == 0)
						{
							if (addedPage)
							{
								// Note that it might not have actually added it due to the ID failing to parse, or the page not existing.
								// This is ok: we'll favour robustness in this scenario.
								continue;
							}
							builder.AddRewrite(src.Url, target);
						}
						else
						{
							// Redirect to the canonical one.
							builder.AddRedirect(src.Url, sources[0].Url, src.Id);
						}
					}
				}

				foreach (var kvp in primaryLookup)
				{
					// Set to the target svc.
					var svc = kvp.Value.GetService();
					svc.UpdatePrimaryUrlLookup(kvp.Value);
				}

				return builder;
			}, 20); // Ensure permalinks are added after pages

			Cache();
		}

		private Dictionary<string, List<Permalink>> _srcDictionary;

		/// <summary>
		/// Generates a slug for the given phrase.
		/// </summary>
		/// <param name="phrase"></param>
		/// <returns></returns>
		public string GenerateNormalizedSlug(string phrase)
		{
			if (string.IsNullOrWhiteSpace(phrase))
				return string.Empty;

			// remove accents
			string str = RemoveDiacritics(phrase);

			// custom replacements (ß, ø, æ, etc.)
			str = str.Replace("ß", "ss")
					 .Replace("ø", "o")
					 .Replace("Ø", "O")
					 .Replace("æ", "ae")
					 .Replace("Æ", "Ae");

			// lowercase
			str = str.ToLowerInvariant();

			// replace anything not alphanumeric with hyphens
			str = Regex.Replace(str, @"[^a-z0-9]+", "-");

			// trim extra hyphens
			str = str.Trim('-');

			// Replace double occurences of - or _
			str = Regex.Replace(str, @"([-_]){2,}", "$1", RegexOptions.Compiled);

			return str;
		}

		/// <summary>
		/// Removes diacritics from the given text.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public string RemoveDiacritics(string text)
		{
			var normalizedString = text.Normalize(NormalizationForm.FormD);
			var stringBuilder = new StringBuilder();

			foreach (var c in normalizedString)
			{
				var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
				if (unicodeCategory != UnicodeCategory.NonSpacingMark)
				{
					stringBuilder.Append(c);
				}
			}

			return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
		}

		/// <summary>
		/// Deletes all permalinks to a given page.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="page"></param>
		/// <returns></returns>
		public async ValueTask DeleteLinksTo(Context context, Page page)
		{
			// Any permalink with a target of "page:{pageId}"
			var links = await Where("Target=?", DataOptions.IgnorePermissions)
				.Bind("page:" + page.Id)
				.ListAll(context);

			if (links == null)
			{
				return;
			}

			foreach (var link in links)
			{
				await Delete(context, link, DataOptions.IgnorePermissions);
			}
		}

		/// <summary>
		/// Creates a target string for a permalink which points at the primary page for the given piece of content. See Permalink.Target for more info.
		/// These permalinks are of the form "primary:user:x" or "primary:user" if the object is null. When the routing tree is being updated, they are resolved 
		/// to the actual target page which would either be the fallback primary user page or a specific one if it exists.
		/// This way, if overriding pages for a specific content object are created, historical permalinks remain permanent.
		/// </summary>
		/// <param name="svc">The service that the object originated from.</param>
		/// <param name="context"></param>
		/// <param name="urlPattern"></param>
		/// <param name="targetObject"></param>
		/// <returns></returns>
		public async ValueTask<Permalink> Create<T, ID>(Context context, string urlPattern, AutoService<T, ID> svc, Content<ID> targetObject = null)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			if (targetObject == null)
			{
				// Object required
				throw new PublicException("Object required to generate a permalink", "target/required");
			}

			// Permalink target which will be for whichever page wants to handle this as its primary content.
			// If a specific page for this content exists, it will ultimately pick that.
			var linkTarget = CreatePrimaryTargetLocator(svc, targetObject);

			return await Create(
				context,
				new Permalink()
				{
					Url = SubstitutePattern(urlPattern, targetObject),
					Target = linkTarget
				},
				DataOptions.IgnorePermissions
			);
		}

		/// <summary>
		/// Auto register the given service for permalinks using a simple pattern which can contain /${content.tokens} referencing fields of the content object.
		/// The path is then appended to the object referenced by the given relative field. This relative field must be a virtual field referencing
		/// another piece of content with an established permalink.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="relativeField"></param>
		/// <param name="permalinkGenerator">Generate a permalink with a URL relative to the given object's primary URL.</param>
		/// <param name="svc"></param>
		/// <exception cref="PublicException"></exception>
		public void Generate<T, ID>(string relativeField, Func<Context, T, Permalink> permalinkGenerator, AutoService<T, ID> svc)
			where T : VersionLinkedContent<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var relativeLoader = new RelativeUrlLoader(relativeField, svc);

			Generate(async (Context context, T obj) => {

				var url = await relativeLoader.GetRelativeTo(context, obj);

				var permalink = permalinkGenerator(context, obj);

				if (permalink == null)
				{
					return null;
				}

				var urlPattern = permalink.Url;

				if (!urlPattern.StartsWith("/"))
				{
					urlPattern = "/" + urlPattern;
				}

				if (url.EndsWith("/"))
				{
					url = url + urlPattern.Substring(1);
				}
				else
				{
					url = url + urlPattern;
				}

				permalink.Url = url;
				return permalink;
			}, svc);
		}

		/// <summary>
		/// Auto register the given service for permalinks using a simple pattern which can contain /${content.tokens} referencing fields of the content object.
		/// The path is then appended to the object referenced by the given relative field. This relative field must be a virtual field referencing
		/// another piece of content with an established permalink.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="relativeField"></param>
		/// <param name="urlPattern">Relative to the given object's primary URL.</param>
		/// <param name="svc"></param>
		/// <exception cref="PublicException"></exception>
		public void Generate<T, ID>(string relativeField, string urlPattern, AutoService<T, ID> svc)
			where T : VersionLinkedContent<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			if (!urlPattern.StartsWith("/"))
			{
				urlPattern = "/" + urlPattern;
			}

			var relativeLoader = new RelativeUrlLoader(relativeField, svc);

			Generate(async (Context context, T obj) => {

				// urlPattern was normalised above to start with / always
				var url = await relativeLoader.GetRelativeTo(context, obj);

				if (url.EndsWith("/"))
				{
					url = url + urlPattern.Substring(1);
				}
				else
				{
					url = url + urlPattern;
				}

				return new Permalink() { Url = url };
			}, svc);
		}

		/// <summary>
		/// Auto register the given service for permalinks using a simple pattern which can contain /${content.tokens} referencing fields of the content object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="urlPattern"></param>
		/// <param name="svc"></param>
		/// <exception cref="PublicException"></exception>
		public void Generate<T, ID>(string urlPattern, AutoService<T, ID> svc)
			where T : VersionLinkedContent<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			Generate((Context context, T obj) => new ValueTask<Permalink>(new Permalink() { Url = urlPattern }), svc);
		}

		/// <summary>
		/// Auto register the given service for permalinks.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="permalinkGenerator"></param>
		/// <param name="svc"></param>
		/// <exception cref="PublicException"></exception>
		public void Generate<T, ID>(Func<Context, T, ValueTask<Permalink>> permalinkGenerator, AutoService<T, ID> svc)
			where T : VersionLinkedContent<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			svc.EventGroup.BeforeCreate.AddEventListener(async (Context context, T content) => {
				if (content == null)
				{
					return content;
				}

				if (string.IsNullOrEmpty(content.Slug))
				{
					var name = await svc.GetMetaString(context, "title", content);

					if (string.IsNullOrEmpty(name))
					{
						throw new PublicException("A name or title is required in order to generate the URL", "name/required");
					}

					content.Slug = GenerateNormalizedSlug(name);
				}

				return content;
			});

			svc.EventGroup.AfterDelete.AddEventListener(async (Context context, T content) => {
				if (content == null)
				{
					return content;
				}

				// Any permalinks targeting this content?
				// If so, mark them as dangling by setting them to a target of 404.
				var locator = CreatePrimaryTargetLocator(svc, content);
				var targets = await Where("Target=?").Bind(locator).ListAll(context);

				foreach (var link in targets)
				{
					await Update(context, link, (Context ctx, Permalink toUpdate, Permalink orig) => {

						toUpdate.Target = "404";

					}, DataOptions.IgnorePermissions);
				}

				return content;
			});

			svc.EventGroup.BeforeUpdate.AddEventListener(async (Context context, T content, T orig) => {
				if (content == null)
				{
					return content;
				}

				if (content.Slug != orig.Slug && !string.IsNullOrEmpty(content.Slug))
				{
					var permalink = await permalinkGenerator(context, content);

					if (permalink == null || string.IsNullOrEmpty(permalink.Url))
					{
						return content;
					}

					if (!permalink.Url.StartsWith("/"))
					{
						permalink.Url = "/" + permalink.Url;
					}

					if (string.IsNullOrEmpty(permalink.Target))
					{
						// Permalink target which will be for whichever page wants to handle this as its primary content.
						// If a specific page for this content exists, it will ultimately pick that.
						permalink.Target = CreatePrimaryTargetLocator(svc, content);
					}

					// These kinds of permalinks can use convenience ${tokens} in the Url.
					permalink.Url = SubstitutePattern(permalink.Url, content);

					await Disambiguate(context, permalink);

					await Create(
						context,
						permalink,
						DataOptions.IgnorePermissions
					);
				}

				return content;
			});

			svc.EventGroup.AfterCreate.AddEventListener(async (Context context, T content) => {
				if (content == null)
				{
					return null;
				}

				Permalink permalink = await permalinkGenerator(context, content);

				if (permalink == null || string.IsNullOrEmpty(permalink.Url))
				{
					return content;
				}

				if (!permalink.Url.StartsWith("/"))
				{
					permalink.Url = "/" + permalink.Url;
				}

				if (string.IsNullOrEmpty(permalink.Target))
				{
					// Permalink target which will be for whichever page wants to handle this as its primary content.
					// If a specific page for this content exists, it will ultimately pick that.
					permalink.Target = CreatePrimaryTargetLocator(svc, content);
				}

				// These kinds of permalinks can use convenience ${tokens} in the Url.
				permalink.Url = SubstitutePattern(permalink.Url, content);

				await Disambiguate(context, permalink);

				await Create(
					context,
					permalink,
					DataOptions.IgnorePermissions
				);
				return content;
			});

		}

		private async ValueTask Disambiguate(Context context, Permalink permalink)
		{
			// Disambiguate - if the url already exists, don't hijack it through creation.
			// Instead, append a number to it.
			for (var inc = 0; inc < 50; inc++)
			{
				var testedUrl = inc == 0 ? permalink.Url : permalink.Url + "-" + inc;

				var anyOnThisUrl = await Where("Url=?", DataOptions.IgnorePermissions)
					.Bind(testedUrl)
					.First(context);

				if (anyOnThisUrl == null)
				{
					if (inc != 0)
					{
						permalink.Url = testedUrl;
					}

					return;
				}
			}

			throw new PublicException("Failed to disambiguate permalink URL '" + permalink.Url + "'.", "permalink/too-many-variants");
		}

		private string SubstitutePattern(string urlPattern, object content)
		{
			if (content == null)
			{
				throw new ArgumentNullException(nameof(content), "Content object cannot be null.");
			}

			// Capture just the word characters inside ${content.Name}
			var matches = Regex.Matches(urlPattern, @"\$\{content\.(\w+)\}");
			string result = urlPattern;
			var contentType = content.GetType();

			foreach (Match match in matches)
			{
				string fullPlaceholder = match.Value; // e.g., "${content.Slug}"
				string memberName = match.Groups[1].Value; // e.g., "Slug"
				object memberValue;

				// Find the field/ property on the top-level object
				FieldInfo fieldInfo = contentType.GetField(memberName);
				if (fieldInfo != null)
				{
					memberValue = fieldInfo.GetValue(content);
				}
				else
				{
					PropertyInfo propInfo = contentType.GetProperty(memberName);
					if (propInfo == null)
					{
						// If neither exists, throw an exception
						throw new InvalidOperationException($"Member '{memberName}' (property or field) not found on object of type {contentType.Name}.");
					}
					memberValue = propInfo.GetValue(content);
				}

				// --- Validation Checks ---

				// 1. Throw if the value is null
				if (memberValue == null)
				{
					throw new InvalidOperationException($"The value for '{memberName}' is null.");
				}

				// 2. Throw if the referenced field is not a string
				if (memberValue is not string valueAsString)
				{
					throw new InvalidCastException($"The property '{memberName}' is of type {memberValue.GetType().Name}, but a string is required.");
				}

				// 3. Throw if the string is empty
				if (string.IsNullOrEmpty(valueAsString))
				{
					throw new InvalidOperationException($"The value for '{memberName}' cannot be an empty string.");
				}

				// Perform the substitution
				result = result.Replace(fullPlaceholder, valueAsString);
			}

			return result;
		}

		/// <summary>
		/// Creates a target string for a permalink which points at the primary page for the given piece of content. See Permalink.Target for more info.
		/// These permalinks are of the form "primary:user:x" or "primary:user" if the object is null. When the routing tree is being updated, they are resolved 
		/// to the actual target page which would either be the fallback primary user page or a specific one if it exists.
		/// This way, if overriding pages for a specific content object are created, historical permalinks remain permanent.
		/// </summary>
		/// <param name="svc">The service that the object originated from.</param>
		/// <param name="targetObject"></param>
		/// <param name="adminGroup">Optionally generate it as a permalink to the admin panel primary page (usually of the form /en-admin/user/x).</param>
		/// <returns></returns>
		public string CreatePrimaryTargetLocator<T, ID>(AutoService<T, ID> svc, Content<ID> targetObject = null, bool adminGroup = false)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			return CreatePrimaryTargetLocator(svc.ServicedType, targetObject == null ? null : targetObject.Id.ToString(), adminGroup);
		}

		private string CreatePrimaryTargetLocator(Type servicedType, string specificContentId, bool adminGroup)
		{
			var contentType = servicedType.Name.ToLower();

			if (specificContentId == null)
			{
				if (adminGroup)
				{
					return "admin_primary:" + contentType;
				}

				return "primary:" + contentType;
			}

			if (adminGroup)
			{
				return "admin_primary:" + contentType + ":" + specificContentId;
			}

			return "primary:" + contentType + ":" + specificContentId;
		}

		/// <summary>
		/// True if a permalink target is a primary: or admin_primary: locator.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="service"></param>
		/// <param name="isAdminGroup"></param>
		/// <param name="specificContentId"></param>
		/// <returns></returns>
		public bool TargetIsPrimaryLocator(string key, out AutoService service, out bool isAdminGroup, out string specificContentId)
		{
			// Page keys are a superset of the locator format.
			return PageKeyIsPrimary(key, out service, out isAdminGroup, out specificContentId);
		}

		/// <summary>
		/// True if the given page Key is a primary content one. See Page.Key for more details. 
		/// Returns the relevant service and also a specific content ID if there is one.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="service"></param>
		/// <param name="isAdminGroup"></param>
		/// <param name="specificContentId"></param>
		/// <returns></returns>
		private bool PageKeyIsPrimary(string key, out AutoService service, out bool isAdminGroup, out string specificContentId)
		{
			// By definition:
			// primary:user
			// admin_primary:user
			// primary:user:42
			// primary:user@PageId:42
			// admin_primary:user:42 (not that this would ever happen, but we support it anyway!)

			if (key == null)
			{
				service = null;
				isAdminGroup = false;
				specificContentId = null;
				return false;
			}

			isAdminGroup = key.StartsWith("admin_");

			var primaryIndex = key.IndexOf("primary:");

			if (primaryIndex == -1)
			{
				service = null;
				specificContentId = null;
				return false;
			}

			// The index of the first letter of the type.
			var typeStart = primaryIndex + 8;

			var specificContentIndex = key.IndexOf(':', typeStart);

			var typeName = (specificContentIndex == -1) ? 
				key.Substring(typeStart) : 
				key.Substring(typeStart, specificContentIndex - typeStart);

			service = Services.Get(typeName + "service");

			if (specificContentIndex == -1)
			{
				specificContentId = null;
			}
			else
			{
				specificContentId = key.Substring(specificContentIndex + 1);
			}

			return service != null;
		}

		/// <summary>
		/// Gets the set of permalinks for a given Target string (primary, non-admin).
		/// </summary>
		/// <param name="context"></param>
		/// <param name="contentType"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public async ValueTask<List<Permalink>> GetPermalinksForTarget(Context context, string contentType, ulong id)
		{
			if (contentType == null || id == 0)
			{
				return null;
			}

			var target = "primary:" + contentType.ToLower() + ":" + id;
			var sources = await GetSourcesByTarget(context);
			sources.TryGetValue(target, out List<Permalink> result);
			return result;
		}

		/// <summary>
		/// Gets the set of permalinks for a given Target string. Usually either page:x or a primary locator which you can use the overload for.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public async ValueTask<List<Permalink>> GetPermalinksForTarget(Context context, string target)
		{
			var sources = await GetSourcesByTarget(context);
			sources.TryGetValue(target, out List<Permalink> result);
			return result;
		}

		/// <summary>
		/// Gets a dictionary for all target URLs to all their sources.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async ValueTask<Dictionary<string, List<Permalink>>> GetSourcesByTarget(Context context)
		{
			var result = _srcDictionary;

			if (result == null)
			{
				result = await CreateSourcesByTarget(context);
				_srcDictionary = result;
			}

			return result;
		}

		/// <summary>
		/// A dictionary from target URLs to all its sources. 
		/// The first one in the list is always the canonical entry.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private async ValueTask<Dictionary<string, List<Permalink>>> CreateSourcesByTarget(Context context)
		{
			var all = await Where(DataOptions.IgnorePermissions).ListAll(context);
			var result = new Dictionary<string, List<Permalink>>();

			foreach (var link in all)
			{
				if (string.IsNullOrEmpty(link.Target) || link.Target == "404")
				{
					// Skip danglers
					continue;
				}

				AddToDictionary(link, result);
			}

			return result;
		}

		private void AddToDictionary(Permalink link, Dictionary<string, List<Permalink>> set)
		{
			if (!set.TryGetValue(link.Target, out List<Permalink> sources))
			{
				sources = new List<Permalink>();
				set[link.Target] = sources;
				sources.Add(link);
				return;
			}

			// There will always be at least 1 entry in the set
			// so check if this incoming one is newer.
			var canon = sources[0];

			if (link.CreatedUtc > canon.CreatedUtc)
			{
				// This is the new canonical entry.
				sources[0] = link;
				sources.Add(canon);
			}
			else
			{
				sources.Add(link);
			}
		}

		private int IndexOfSource(string src, List<Permalink> links)
		{
			for(var i=0;i<links.Count;i++)
			{
				var link = links[i];

				if (link.Url == src)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Gets the canonical link if there is one. The provided target URL must be absolute and exclude any 
		/// domain. It should also not end in a trailing /.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="targetUrl"></param>
		/// <returns></returns>
		public async ValueTask<string> GetCanonicalLink(Context context, string targetUrl)
		{
			var dict = await GetSourcesByTarget(context);

			if (dict.TryGetValue(targetUrl, out List<Permalink> sources))
			{
				// The first source is the canonical link.
				return sources[0].Url;
			}

			// It is the canonical link.
			return targetUrl;
		}

		/// <summary>
		/// Gets the canonical link if there is one. The provided target is e.g. `primary:type:id`
		/// </summary>
		/// <param name="context"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public async ValueTask<Permalink> GetCanonical(Context context, string target)
		{
			var dict = await GetSourcesByTarget(context);

			if (dict.TryGetValue(target, out List<Permalink> sources) && sources.Count > 0)
			{
				// The first source is the canonical link.
				return sources[0];
			}

			return null;
		}

		/// <summary>
		/// Bulk creates a block of permalinks. Entries will be skipped if the exact pairing already exists.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="links"></param>
		/// <returns></returns>
		public async ValueTask BulkCreate(Context context, List<PermalinkUrlTarget> links)
		{
			// Get the (usually cached) set of all permalinks:
			var set = await GetSourcesByTarget(context);

			// Todo: this doesn't block non-unique sources.

			var setToCreate = new List<Permalink>();

			foreach (var link in links)
			{
				// If it already exists, skip it.
				// These are also blocked by the database using a unique index.
				if (set.TryGetValue(link.Target, out List<Permalink> sources))
				{
					var linkIndex = IndexOfSource(link.Url, sources);

					if (linkIndex == 0)
					{
						// It's already the canonical link.
						continue;
					}
					else if (linkIndex > 0)
					{
						// It exists but as the non-canonical link.
						// Update it to set its createdUtc to now:
						await Update(context, sources[linkIndex], (Context ctx, Permalink toUpdate, Permalink orig) => {
							toUpdate.CreatedUtc = DateTime.UtcNow;
						}, DataOptions.IgnorePermissions);
						continue;
					}
				}

				setToCreate.Add(new Permalink() {
					Url = link.Url,
					Target = link.Target,
					CreatedUtc = DateTime.UtcNow,
					UserId = context.UserId
				});
			}

			await CreateAll(context, setToCreate, DataOptions.IgnorePermissions);
		}

	}
    
}
