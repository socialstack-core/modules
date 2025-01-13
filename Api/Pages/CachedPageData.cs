using System;
using System.Collections.Generic;

namespace Api.Pages;


/// <summary>
/// Stores cached page data for faster regeneration of pages.
/// </summary>
public class CachedPageData
{
	/// <summary>
	/// Creates a new cached page data set with the given documents in it.
	/// </summary>
	/// <param name="nodes"></param>
	public CachedPageData(List<DocumentNode> nodes)
    {
		Nodes = nodes;
	}

	/// <summary>
	/// Creates a new cached page data set with the given nodes in it.
	/// </summary>
	/// <param name="nodes"></param>
	/// <param name="anonymousCompressedPage"></param>
	/// <param name="hash"></param>
	/// <param name="cacheMaxAge"></param>
	public CachedPageData(List<DocumentNode> nodes, byte[] anonymousCompressedPage, string hash, ulong cacheMaxAge)
	{
		Nodes = nodes;
		AnonymousCompressedPage = anonymousCompressedPage;
		Hash = hash;

		var lastModifiedUtc = DateTime.UtcNow;
		LastModifiedHeader = lastModifiedUtc.ToString("r");
		ExpiresHeader = lastModifiedUtc.AddSeconds(cacheMaxAge).ToString("r");
	}

	/// <summary>
	/// A node set which is safe for use by any role.
	/// </summary>
	public List<DocumentNode> Nodes;

	/// <summary>
	/// Pre-compressed complete state response for anonymous users.
	/// </summary>
	public byte[] AnonymousCompressedState;

	/// <summary>
	/// Pre-compressed complete page for anon users.
	/// </summary>
	public byte[] AnonymousCompressedPage;

	/// <summary>
	/// Hash of the compressed page for ETag http header
	/// </summary>
	public string Hash;

	/// <summary>
	/// The ISO string for when the hash was created
	/// </summary>
	public string LastModifiedHeader;

	/// <summary>
	/// The ISO string for when the content should expire from external cache
	/// </summary>
	public string ExpiresHeader;
}