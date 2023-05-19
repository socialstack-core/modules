using Microsoft.AspNetCore.Mvc;

namespace Api.Redirects
{
    /// <summary>Handles redirect endpoints.</summary>
    [Route("v1/redirect")]
	public partial class RedirectController : AutoController<Redirect>
    {
    }
}