using Microsoft.AspNetCore.Mvc;

namespace Api.Connections
{
    /// <summary>
    /// Handles connection endpoints.
    /// </summary>
    [Route("v1/connection")]
	public partial class ConnectionController : AutoController<Connection>
    {
	}
}