using Microsoft.AspNetCore.Mvc;

namespace Api.Huddles
{
    /// <summary>Handles huddlePresence endpoints.</summary>
    [Route("v1/huddlepresence")]
	public partial class HuddlePresenceController : AutoController<HuddlePresence>
    {
    }
}