using Microsoft.AspNetCore.Mvc;

namespace Api.Permissions
{
    /// <summary>Handles permittedContent endpoints.</summary>
    [Route("v1/permittedContent")]
	public partial class PermittedContentController : AutoController<PermittedContent>
    {
    }
}