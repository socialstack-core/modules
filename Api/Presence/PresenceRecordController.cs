using Microsoft.AspNetCore.Mvc;

namespace Api.Presence
{
    /// <summary>Handles presenceRecord endpoints.</summary>
    [Route("v1/presenceRecord")]
	public partial class PresenceRecordController : AutoController<PresenceRecord>
    {
    }
}