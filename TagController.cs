using Microsoft.AspNetCore.Mvc;

namespace Api.Tags
{
    /// <summary>
    /// Handles tag endpoints.
    /// </summary>
    [Route("v1/tag")]
	public partial class TagController : AutoController<Tag, TagAutoForm>
    {
    }
}