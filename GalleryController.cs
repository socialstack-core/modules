using Microsoft.AspNetCore.Mvc;

namespace Api.Galleries
{
    /// <summary>
    /// Handles gallery endpoints.
    /// </summary>
    [Route("v1/gallery")]
	public partial class GalleryController : AutoController<Gallery>
    {
    }
}