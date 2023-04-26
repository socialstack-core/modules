using Api.Database;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Api.Pages
{
	
	/// <summary>
	/// Stores information about URLs which helps in generating them.
	/// </summary>
	public partial class UrlGenerationMeta
	{
		/// <summary>
		/// The primary type for this meta. This is just the key in the dictionary of the cache.
		/// </summary>
		public Type PrimaryType;

		/// <summary>
		/// Possible pages. This is almost always just 1 option.
		/// </summary>
		private readonly List<UrlGenerator> Pages = new List<UrlGenerator>();

		/// <summary>
		/// Creates a new meta object for the given primary type.
		/// </summary>
		/// <param name="primaryType"></param>
		public UrlGenerationMeta(Type primaryType)
		{
			PrimaryType = primaryType;
		}

		/// <summary>
		/// Adds the given page info to this meta. Most have just one suitable page.
		/// The lowest "piecesAfter" wins. If there are multiple at this same depth, they both win and a condition 
		/// is required to select the correct one for a given piece of content. It raises an event and the conditional is handled by code.
		/// </summary>
		/// <param name="pageId"></param>
		/// <param name="urlPieces"></param>
		/// <param name="piecesAfter"></param>
		public void AddPage(uint pageId, string[] urlPieces, int piecesAfter)
		{
			if (Pages.Count != 0 && piecesAfter > Pages[0].PiecesAfter)
			{
				// Reject - this page is a more specific url than the current winning candidate.
				// For example, /news/{blogpost.slug}/ is the winning candidate and we just tried to replace it with /news/{blogpost.slug}/edit (this edit URL has piecesAfter == 1)
				return;
			}

			if (Pages.Count != 0 && piecesAfter < Pages[0].PiecesAfter)
			{
				// This page that we're adding is a better candidate. /news/{blogpost.slug}/edit was winning, but we just found /news/{blogpost.slug}/
				Pages.Clear();
			}

			Pages.Add(new UrlGenerator(urlPieces)
			{
				PageId = pageId,
				PiecesAfter = piecesAfter
			});
		}

		/// <summary>
		/// Generates a URL for the given piece of content which MUST be of the primary type.
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public string Generate(object content)
		{
			if (Pages == null || Pages.Count == 0)
			{
				// No page for this content type
				return null;
			}

			// Todo: if there is more than one, trigger an event to ask which page to use.
			// This is expected to be rare though - it'd be where there is e.g. forums in 2 completely different parts of the site - a particular piece of content needs a site specific way of choosing which url is its canonical one.
			// (Except realistically these different forums should in the same place for better taxonomy).
			return Pages[0].Generate(content);
		}

	}

	/// <summary>
	/// Url generation info for a particular URL pattern.
	/// </summary>
	public partial class UrlGenerator
	{
		/// <summary>
		/// The page ID
		/// </summary>
		public uint PageId;

		/// <summary>
		/// The # of url pieces after the primary content type is referenced. 
		/// In the url /news/{blogpost.slug}/edit, {blogpost.slug} is the primary content type reference. It has one piece after it.
		/// /news/{blogpost.slug}/edit/permissions has 2 pieces after it and is therefore more specific and not the main page for the content type.
		/// </summary>
		public int PiecesAfter;

		/// <summary>
		/// The raw set of url/pieces (the url split by /).
		/// </summary>
		public readonly string[] UrlPieces;

		/// <summary>
		/// Url pieces that have been fully parsed, such as converting {contentType.field} into an actual field that can be read from a given object.
		/// </summary>
		private UrlGenerationUrlFragment[] LoadedUrlPieces;


		/// <summary>
		/// Creates a URL generator from the given URL fragments.
		/// </summary>
		/// <param name="urlPieces"></param>
		public UrlGenerator(string[] urlPieces)
		{
			UrlPieces = urlPieces;
		}
		
		/// <summary>
		/// Creates a URL generator from the given URL.
		/// </summary>
		/// <param name="url"></param>
		public UrlGenerator(string url)
		{
			if (url == null)
			{
				UrlPieces = Array.Empty<string>();
			}
			else
			{
				UrlPieces = url.Split('/');
			}
		}

		/// <summary>
		/// Generates a URL for the given piece of content which MUST be of the primary type.
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public string Generate(object content)
		{
			var sb = new StringBuilder();

			if (LoadedUrlPieces == null)
			{
				// Load the url pieces:
				LoadPieces();
			}

			for (var i = 0; i < LoadedUrlPieces.Length; i++)
			{
				var piece = LoadedUrlPieces[i];

				if (piece == null)
				{
					continue;
				}

				if (piece.LiteralText != null)
				{
					sb.Append(piece.LiteralText);
				}
				else
				{
					// It's not literal - it's a field spec. Resolve its value from the object:
					sb.Append(piece.Resolve(content));
				}

				sb.Append('/');
			}

			return sb.ToString();
		}

		/// <summary>
		/// Loads the url pieces, resolving via reflection fields etc.
		/// </summary>
		private void LoadPieces()
		{
			var loadingUrlPieces = new UrlGenerationUrlFragment[UrlPieces.Length];

			Type currentType = null;

			for (var i = UrlPieces.Length -1; i >= 0; i--)
			{
				var piece = UrlPieces[i].Trim();
				var fragment = new UrlGenerationUrlFragment();

				if (piece.Length > 0 && piece[0] == '{')
				{
					// It's a field ref of the form {contentType.field}
					string typeAndField;

					if (piece[^1] == '}')
					{
						typeAndField = piece[1..^1];
					}
					else
					{
						// Just in caase somebody forgot the one on the end
						typeAndField = piece[1..];
					}

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
						// Unrecognised content type - treat piece literally:
						fragment.LiteralText = piece;
					}
					else
					{
						// Get the field or property from the content type:
						var fieldName = typeAndField[(firstDot + 1)..];
						var field = contentType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

						if (field == null)
						{
							var property = contentType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

							if (property != null)
							{
								fragment.FieldReaders = new List<MemberInfo>();

								// Todo: Add any readers needed to turn currentType into contentType.

								fragment.FieldReaders.Add(property.GetGetMethod());
								currentType = contentType;
							}
							else
							{
								// Treat it literally:
								fragment.LiteralText = piece;
							}
						}
						else
						{
							fragment.FieldReaders = new List<MemberInfo>();

							// Todo: Add any readers needed to turn currentType into contentType.

							fragment.FieldReaders.Add(field);

							currentType = contentType;
						}

					}
				}
				else
				{
					// Treat it literally:
					fragment.LiteralText = piece;
				}

				loadingUrlPieces[i] = fragment;
			}

			LoadedUrlPieces = loadingUrlPieces;
		}
	}

	/// <summary>
	/// A fully parsed url fragment.
	/// </summary>
	public partial class UrlGenerationUrlFragment
	{
		/// <summary>
		/// Set if this is literal text.
		/// </summary>
		public string LiteralText;

		/// <summary>
		/// Consecutive field reads required to resolve to the final value. Either FieldInfo or Property get methods.
		/// </summary>
		public List<MemberInfo> FieldReaders;

		/// <summary>
		/// Resolves this field reference fragment - e.g. {contentType.field} - to an actual textual value.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public string Resolve(object obj)
		{
			// Resolve is only called if field or property are actually set.
			if (FieldReaders == null)
			{
				return "";
			}

			var current = obj;

			for (var i = 0; i < FieldReaders.Count; i++)
			{
				var reader = FieldReaders[i];
				if (reader is FieldInfo info)
				{
					current = info.GetValue(current);
				}
				else
				{
					current = ((MethodInfo)reader).Invoke(current, null);
				}
			}

			// current MUST be a value type, otherwise we won't permit it to be toStringed for security.
			if (current == null)
			{
				return "";
			}

			var t = current.GetType();
			if (t.IsValueType || t == typeof(string))
			{
				return current.ToString();
			}

			return "[object-not-permitted-in-url]";
		}
	}

}