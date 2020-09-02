using Microsoft.AspNetCore.Mvc;

namespace Api.Rewards
{
    /// <summary>
    /// Handles reward content endpoints.
    /// </summary>
    [Route("v1/rewardcontent")]
	public partial class RewardContentController : AutoController<RewardContent>
    {
    }
}