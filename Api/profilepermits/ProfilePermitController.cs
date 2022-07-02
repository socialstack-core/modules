using System.Linq;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Api.ProfilePermits
{
    /// <summary>
    /// Handles profile permit endpoints.
    /// </summary>
    [Route("v1/profilepermit")]
	public partial class ProfilePermitController : AutoController<ProfilePermit>
    {
    }
}