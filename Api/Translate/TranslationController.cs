using Microsoft.AspNetCore.Mvc;

namespace Api.Translate
{
    /// <summary>Handles translation endpoints.</summary>
    [Route("v1/translation")]
	public partial class TranslationController : AutoController<Translation>
    {
    }
}