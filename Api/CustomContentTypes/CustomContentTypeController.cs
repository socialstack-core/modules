using Microsoft.AspNetCore.Mvc;

namespace Api.CustomContentTypes
{
    /// <summary>Handles customContentType endpoints.</summary>
    [Route("v1/customContentType")]
	public partial class CustomContentTypeController : AutoController<CustomContentType>
    {
    }
}