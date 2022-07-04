using Microsoft.AspNetCore.Mvc;

namespace Api.UserAgendaEntries
{
    /// <summary>Handles userAgendaEntry endpoints.</summary>
    [Route("v1/userAgendaEntry")]
	public partial class UserAgendaEntryController : AutoController<UserAgendaEntry>
    {
    }
}