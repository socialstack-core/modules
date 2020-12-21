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
        [HttpPost]
        public override async ValueTask<object> Create([FromBody] JObject body)
        {
            var entity = new Follower();

            var context = Request.GetContext();

            await SetFieldsOnObject(entity, context, body, JsonFieldGroup.Default);

            var follower = await _service.List(context, new Filter<Follower>().Equals("UserId", entity.UserId).And().Equals("SubscribedToId", entity.SubscribedToId));

            if (follower.Any())
            {
                return follower.First();
            }

            return base.Create(body);
        }
    }
}