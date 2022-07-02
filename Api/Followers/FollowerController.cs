using System.Linq;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Api.Followers
{
    /// <summary>
    /// Handles follower endpoints.
    /// </summary>
    [Route("v1/follower")]
	public partial class FollowerController : AutoController<Follower>
    {
    }
}