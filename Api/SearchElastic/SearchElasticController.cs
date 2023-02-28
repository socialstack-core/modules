using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Api.SearchElastic
{
    /// <summary>Handles site search endpoints.</summary>
    [Route("v1/sitesearch")]
    public partial class SearchElasticController : Controller
    {
        /// <summary>
        /// Exposes the site search
        /// </summary>
        [HttpPost("query")]
        public virtual async ValueTask<DocumentsResult> Query([FromBody] JObject filters)
        {
            var queryString = filters["queryString"] != null ? filters["queryString"].ToString() : "";
            var tags = filters["tags"] != null ? filters["tags"].ToString() : "";
            var contentTypes = filters["contentTypes"] != null ? filters["contentTypes"].ToString() : "";
            int pageSize = filters["pageSize"] != null ? filters["pageSize"].ToObject<int>() : 10;
            int startFrom = filters["startFrom"] != null ? filters["startFrom"].ToObject<int>() : 0;

            var context = await Request.GetContext();

            var documentsResult = await Services.Get<SearchElasticService>().Query(context, queryString, tags, contentTypes, startFrom, pageSize);

            return documentsResult;
        }

        [HttpGet("reset")]
        public virtual async ValueTask<bool> Reset()
        {
            return await Services.Get<SearchElasticService>().Reset();
        }

    }
}