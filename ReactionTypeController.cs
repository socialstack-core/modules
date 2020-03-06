using Microsoft.AspNetCore.Mvc;

namespace Api.Reactions
{
    /// <summary>
    /// Handles reaction types (creating a new type of like/ upvote etc).
    /// </summary>
    [Route("v1/reactiontype")]
	public partial class ReactionTypeController : AutoController<ReactionType, ReactionTypeAutoForm>
    {
    }
}