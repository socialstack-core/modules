using Microsoft.AspNetCore.Mvc;

namespace Api.Presence
{
    /// <summary>Handles pagePresenceRecord endpoints.</summary>
    [Route("v1/pagePresenceRecord")]
	public partial class PagePresenceRecordController : AutoController<PagePresenceRecord, ulong>
    {
    }
}