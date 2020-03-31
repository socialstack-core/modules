using Microsoft.AspNetCore.Mvc;

namespace Api.Pages
{
    /// <summary>
    /// Handles page endpoints.
    /// </summary>

    [Route("v1/page")]
	public partial class PageController : AutoController<Page>
    {
    }
}