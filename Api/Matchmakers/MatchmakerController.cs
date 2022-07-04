using Microsoft.AspNetCore.Mvc;

namespace Api.Matchmakers
{
    /// <summary>Handles matchmaker endpoints.</summary>
    [Route("v1/matchmaker")]
	public partial class MatchmakerController : AutoController<Matchmaker>
    {
    }
}