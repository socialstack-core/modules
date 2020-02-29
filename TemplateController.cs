using Microsoft.AspNetCore.Mvc;

namespace Api.Templates
{
    /// <summary>
    /// Handles template endpoints.
    /// </summary>

    [Route("v1/template")]
	public partial class TemplateController : AutoController<Template, TemplateAutoForm>
    {
    }
}