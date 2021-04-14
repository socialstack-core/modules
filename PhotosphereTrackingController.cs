using Microsoft.AspNetCore.Mvc;


namespace Api.PhotosphereTracking
{
    /// <summary>Handles video endpoints.</summary>
    [Route("v1/photospheretracking")]
    public partial class PhotosphereTrackingController : AutoController<PhotosphereTrack, uint>
    {
    }
}