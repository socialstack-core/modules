using Microsoft.AspNetCore.Mvc;

namespace Api.CustomContentTypes
{
    /// <summary>Handles customContentTypeField endpoints.</summary>
    [Route("v1/customContentTypeField")]
	public partial class CustomContentTypeFieldController : AutoController<CustomContentTypeField>
    {
    }
}