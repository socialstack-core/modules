using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Api.Pages
{
    /// <summary>Handles permalink endpoints.</summary>
    [Route("v1/permalink")]
	public partial class PermalinkController : AutoController<Permalink>
    {

		/// <summary>
		/// Gets the links for a given target
		/// </summary>
		/// <returns></returns>
		[HttpPost("links-for-target")]
		public async ValueTask<ContentStream<Permalink, uint>?> GetPermalinks(Context context, [FromBody] PermalinkRequest req)
		{
			var permas = (_service as PermalinkService);
			var links = await permas.GetPermalinksForTarget(context, req.ContentType, req.ContentId);

			if (links == null)
			{
				return null;
			}

			return new ContentStream<Permalink, uint>(links, permas);
		}

		/// <summary>
		/// Convenience mechanism to make a particular permalink the canonical one.
		/// </summary>
		/// <returns></returns>
		[HttpGet("{id}/make-canonical")]
		public async ValueTask<Permalink> MakeCanonical(Context context, [FromRoute] uint id)
		{
			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw new PublicException("Unavailable", "no_access");
			}

			var permas = (_service as PermalinkService);
			var link = await permas.Get(context, id);

			if (link == null)
			{
				return null;
			}

			// Updating causes router rebuild.
			return await permas.Update(context, link, (Context ctx, Permalink toUpdate, Permalink orig) => {
				toUpdate.CreatedUtc = DateTime.UtcNow;
			}, DataOptions.IgnorePermissions);
		}

	}

	/// <summary>
	/// A request to GetPermalinks
	/// </summary>
	public struct PermalinkRequest
	{
		/// <summary>
		/// The content type e.g. "User"
		/// </summary>
		public string ContentType;
		/// <summary>
		/// The content ID
		/// </summary>
		public ulong ContentId;
	}
}