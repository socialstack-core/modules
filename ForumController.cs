using Microsoft.AspNetCore.Mvc;

namespace Api.Forums
{
    /// <summary>
    /// Handles forum endpoints.
    /// </summary>

    [Route("v1/forum")]
	public partial class ForumController : AutoController<Forum, ForumAutoForm>
    {
    }
}