using Microsoft.AspNetCore.Mvc;

namespace Api.Components
{
    /// <summary>Handles componentGroup endpoints.</summary>
    [Route("v1/componentgroup")]
	public partial class ComponentGroupController : AutoController<ComponentGroup>
    {
    }
}