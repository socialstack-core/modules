using Microsoft.AspNetCore.Mvc;

namespace Api.Matchmakers
{
    /// <summary>Handles matchServer endpoints.</summary>
    [Route("v1/matchServer")]
	public partial class MatchServerController : AutoController<MatchServer>
    {
    }
}