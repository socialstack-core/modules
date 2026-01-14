using Microsoft.AspNetCore.Mvc;

namespace Api.Pages
{
    /// <summary>Handles permalink endpoints.</summary>
    [Route("v1/permalink")]
	public partial class PermalinkController : AutoController<Permalink>
    {
    }
}