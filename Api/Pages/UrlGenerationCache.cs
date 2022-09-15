using Api.Database;
using System;
using System.Collections.Generic;

namespace Api.Pages
{
	
	/// <summary>
	/// Caches information about URLs on the site in order to aid with generating a URL for a particular piece of content.
	/// </summary>
	public partial class UrlGenerationCache
	{
		/// <summary>
		/// The mapping of content type -> its generation meta, per scope.
		/// </summary>
		public Dictionary<Type, UrlGenerationMeta>[] MetasPerScope;
		
		/// <summary>
		/// </summary>
		public UrlGenerationCache()
		{
			// Create the scopes:
			MetasPerScope = new Dictionary<Type, UrlGenerationMeta>[UrlGenerationScope.All.Length];

			for (var i = 0; i < UrlGenerationScope.All.Length; i++)
			{
				// Create a lookup:
				MetasPerScope[i] = new Dictionary<Type, UrlGenerationMeta>();
			}
		}

		/// <summary>
		/// Loads the cache from the given list of all pages.
		/// </summary>
		/// <param name="allPages"></param>
		public void Load(List<Page> allPages)
		{
			// Clear the lookups:
			for (var i = 0; i < UrlGenerationScope.All.Length; i++)
			{
				MetasPerScope[i].Clear();
			}

			// Loop over pages and establish which scope it belongs to:
			for (var p = 0; p < allPages.Count; p++)
			{
				var pageInfo = allPages[p];

				if (pageInfo == null || pageInfo.Url == null)
				{
					continue;
				}

				if (!pageInfo.Url.Contains('{'))
				{
					// Not a URL of interest to us because it contains no {contentType.field} references.
					continue;
				}

				// The list of all scopes is already priority sorted, so:
				var claimingScope = UrlGenerationScope.All[0];

				for (var i = UrlGenerationScope.All.Length - 1; i >= 0; i--)
				{
					// Get the scope prefix:
					var scope = UrlGenerationScope.All[i];

					if (scope.Prefix == null)
					{
						// This scope will claim this page always.
						claimingScope = scope;
						break;
					}
					else if (pageInfo.Url.StartsWith(scope.Prefix))
					{
						// This scope will claim this page.
						claimingScope = scope;
						break;
					}

				}

				// This page belongs to claimingScope. Get the lookup for it:
				var lookup = GetLookup(claimingScope);

				// Next, parse the URL to establish its primary content type. That's the one used last.
				// The number of pieces after it is important too - the URL with the lowest number will ultimately "win" for that content type.
				// If multiple win at the same depth, the pages must define a condition that 
				var pieces = pageInfo.Url.Split('/');

				for (var i = pieces.Length - 1; i >= 0; i--)
				{
					var piece = pieces[i].Trim();
					if (piece.Length > 0 && piece[0] == '{')
					{
						// Found the last one. Its # of pieces after it is..
						var piecesAfter = (pieces.Length - 1) - i;
						string typeAndField;

						if (piece[piece.Length - 1] == '}')
						{
							typeAndField = piece.Substring(1, piece.Length - 2);
						}
						else
						{
							// Just in caase somebody forgot the one on the end
							typeAndField = piece.Substring(1);
						}

						// typeAndField is e.g. "page.url". Get the content type:
						var firstDot = typeAndField.IndexOf('.');

						Type contentType;

						if (firstDot == -1)
						{
							// All of it
							contentType = ContentTypes.GetType(typeAndField);
						}
						else
						{
							contentType = ContentTypes.GetType(typeAndField.Substring(0, firstDot));
						}

						if (contentType == null)
						{
							// Unrecognised content type
							continue;
						}

						if (!lookup.TryGetValue(contentType, out UrlGenerationMeta meta))
						{
							meta = new UrlGenerationMeta(contentType);
							lookup[contentType] = meta;
						}

						meta.AddPage(pageInfo.Id, pieces, piecesAfter);

						break;
					}
				}
			}

		}

		/// <summary>
		/// Gets the lookup for a particular scope. Defaults to the UI scope if not specified.
		/// </summary>
		public Dictionary<Type, UrlGenerationMeta> GetLookup(UrlGenerationScope scope = null)
		{
			if(scope == null)
			{
				// UI scope:
				return MetasPerScope[0];
			}
			
			return MetasPerScope[scope.Id - 1];
		}
		
	}
	
}