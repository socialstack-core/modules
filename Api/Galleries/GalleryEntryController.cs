using Microsoft.AspNetCore.Mvc;

namespace Api.GalleryEntries
{
    /// <summary>
    /// Handles gallery entry endpoints.
    /// </summary>
    [Route("v1/galleryentry")]
	public partial class GalleryEntryController : AutoController<GalleryEntry>
    {
	}
}