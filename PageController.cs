using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Api.CanvasRenderer;

namespace Api.Pages
{
    /// <summary>
    /// Handles page endpoints.
    /// </summary>

    [Route("v1/page")]
    public partial class PageController : AutoController<Page>
    {
        private static HtmlService _htmlService;
        private static FrontendCodeService _frontendService;
        private static byte[] _oldVersion = System.Text.Encoding.UTF8.GetBytes("{\"oldVersion\":1}");
        private PageService _pageService;

        /// <summary>
        /// 
        /// </summary>
        public PageController(PageService ps)
        {
            _pageService = ps;
        }

		/// <summary>
		/// Attempts to get the page state of a page given the url and the version.
		/// </summary>
		/// <param name="pageDetails"></param>
		/// <returns></returns>
		[HttpPost("state")]
		public async ValueTask PageState([FromBody] PageDetails pageDetails)
        {
            if (_htmlService == null)
            {
                _htmlService = Services.Get<HtmlService>();
                _frontendService = Services.Get<FrontendCodeService>();
            }

            // Version check - are they out of date?
            if (pageDetails.version < _frontendService.Version)
            {
                await Response.Body.WriteAsync(_oldVersion);
                return;
            }

			var context = await Request.GetContext();

			// we first need to get the pageAndTokens
			var pageAndTokens = await _pageService.GetPage(context, pageDetails.Url, Microsoft.AspNetCore.Http.QueryString.Empty);

            Response.ContentType = "application/json";
			await _htmlService.RenderState(context, pageAndTokens, pageDetails.Url, Response.Body);
		}

        /// <summary>
        /// Used when getting the page state.
        /// </summary>
        public class PageDetails
        {
            /// <summary>
            /// The url of the page we are getting the state for.
            /// </summary>
            public string Url;

            /// <summary>
            /// The version
            /// </summary>
            public long version;
        }
    }
}