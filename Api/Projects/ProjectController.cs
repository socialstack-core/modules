using Microsoft.AspNetCore.Mvc;

namespace Api.Projects
{
    /// <summary>Handles project endpoints.</summary>
    [Route("v1/project")]
	public partial class ProjectController : AutoController<Project>
    {
    }
}