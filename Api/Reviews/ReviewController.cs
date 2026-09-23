using Microsoft.AspNetCore.Mvc;

namespace Api.Reviews
{
    /// <summary>Handles review endpoints.</summary>
    [Route("v1/review")]
	public partial class ReviewController : AutoController<Review>
    {
    }
}