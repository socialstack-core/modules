using Microsoft.AspNetCore.Mvc;

namespace Api.Rewards
{
    /// <summary>
    /// Handles reward endpoints.
    /// </summary>
    [Route("v1/reward")]
	public partial class RewardController : AutoController<Reward>
    {
    }
}