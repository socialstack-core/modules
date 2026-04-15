using Microsoft.AspNetCore.Mvc;

namespace Api.AutomationTasks
{
    /// <summary>Handles automationTask endpoints.</summary>
    [Route("v1/automationTask")]
	public partial class AutomationTaskController : AutoController<AutomationTask>
    {
    }
}