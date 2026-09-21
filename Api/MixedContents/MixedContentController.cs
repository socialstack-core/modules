using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.MixedContents
{
    /// <summary>Handles mixedContent endpoints.</summary>
    [Route("v1/mixedcontent")]
	public partial class MixedContentController : AutoController
    {
		private MixedContentService _service;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		/// <param name="service"></param>
		public MixedContentController(MixedContentService service)
		{
			_service = service;
		}

		/// <summary>
		/// Gets mixed content.
		/// Generally avoid this endpoint unless necessary - use specialised endpoints whenever possible.
		/// </summary>
		[HttpPost("mixed-list")]
		public async ValueTask<ContentStream<MixedContent, ulong>?> GetContentList(Context context, [FromBody] ContentListRequest request)
		{
			// This endpoint is admin only to avoid any potential IsIncluded() based exploits.
			// For it to become general use, you would need to ensure inclusion of dynamic objects (such as what this relies on)
			// does not trigger IsIncluded to become true. That would only happen at the tiers beyond dynamic objects.
			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw new PublicException("Unavailable", "no_access");
			}

			if (request.Items == null)
			{
				return null;
			}

			return new ContentStream<MixedContent, ulong>(request.Items, _service);
		}
	}
	
	/// <summary>
	/// Gets mixed content.
	/// Generally avoid this endpoint unless necessary - use specialised endpoints.
	/// </summary>
	public struct ContentListRequest
	{
		/// <summary>
		/// The items to get.
		/// Will only return results visible to the contextual user.
		/// </summary>
		public List<MixedContent> Items;
	}
}