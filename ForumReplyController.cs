using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;

namespace Api.Forums
{
    /// <summary>
    /// Handles forum reply endpoints.
    /// </summary>

    [Route("v1/forumreply")]
	public partial class ForumReplyController : AutoController<ForumReply>
    {
	}

}
