using Microsoft.AspNetCore.Mvc;

namespace Api.Configuration
{
    /// <summary>Handles configuration endpoints.</summary>
    [Route("v1/configuration")]
	public partial class ConfigurationController : AutoController<Configuration>
    {
    }
}