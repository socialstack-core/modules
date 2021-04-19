using Microsoft.AspNetCore.Mvc;

namespace Api.Interests
{
    /// <summary>Handles interest endpoints.</summary>
    [Route("v1/interest")]
	public partial class InterestController : AutoController<Interest>
    {
    }
}