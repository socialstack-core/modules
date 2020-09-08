using Microsoft.AspNetCore.Mvc;

namespace Api.Matchmaking
{
    /// <summary>Handles matchServer endpoints.</summary>
    [Route("v1/matchServer")]
	public partial class MatchServerController : AutoController<MatchServer>
    {
    }
}