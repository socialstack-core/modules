using Microsoft.AspNetCore.Mvc;

namespace Api.Matchmaking
{
    /// <summary>Handles matchmaker endpoints.</summary>
    [Route("v1/matchmaker")]
	public partial class MatchmakerController : AutoController<Matchmaker>
    {
    }
}