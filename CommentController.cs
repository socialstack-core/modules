using Microsoft.AspNetCore.Mvc;


namespace Api.Comments
{
    /// <summary>
    /// Handles comment endpoints.
    /// </summary>

    [Route("v1/comment")]
	public partial class CommentController : AutoController<Comment>
    {}
}