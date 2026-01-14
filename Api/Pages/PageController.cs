using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Api.CanvasRenderer;
using Api.Eventing;
using Api.SocketServerLibrary;
using Microsoft.AspNetCore.Http;
using Api.Startup.Routing;
using System;
using System.Collections.Generic;
using Api.Permissions;

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
        /// Gets information about a router tree node. Exclusively explores the http 'GET' tree.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="PublicException"></exception>
        [HttpPost("tree")]
        public TreeNodeDetail? GetRouterTreeNode(Context context, [FromBody] RouterTreeLocation location)
        {
            return GetRouterTreeNodePath(context, location.Url);
		}

		/// <summary>
		/// Gets information about a router tree node. Exclusively explores the http 'GET' tree.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="url"></param>
		/// <returns></returns>
		/// <exception cref="PublicException"></exception>
		[HttpGet("tree")]
		public TreeNodeDetail? GetRouterTreeNodePath(Context context, [FromQuery] string url)
        {
            if (!context.Role.CanViewAdmin)
            {
                throw new PublicException("Admins only", "router/admin_only");
            }

            var router = Router.CurrentRouter;

			if (router == null)
			{
                return null;
			}

			var routingNode = router.AbsoluteResolve("GET", url);

			if (routingNode == null)
			{
				return null;
			}

            var children = new List<RouterNodeMetadata>();

			var kids = routingNode.GetChildren();

            if (kids != null)
            {
                for (var i = 0; i < kids.Length; i++)
                {
                    var info = kids[i].GetMetadata();

                    if (info.HasValue)
                    {
                        children.Add(info.Value);
                    }
				}
			}

            return new TreeNodeDetail
            {
                Children = children,
                Self = routingNode.GetMetadata()
			};
		}

		/// <summary>
		/// Attempts to get the page state of a page given the url and the version. Not available to the SSR or websocket APIs.
		/// </summary>
		/// <param name="httpContext"></param>
		/// <param name="context"></param>
		/// <param name="pageDetails"></param>
		/// <returns></returns>
		[HttpPost("state")]
        [Returns(typeof(PageStateResult))]
		public async ValueTask PageState(HttpContext httpContext, Context context, [FromBody] PageDetails pageDetails)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;

            if (_htmlService == null)
            {
                _htmlService = Services.Get<HtmlService>();
                _frontendService = Services.Get<FrontendCodeService>();
            }

            // Version check - are they out of date?
            if (pageDetails.version < _frontendService.Version)
            {
                await response.Body.WriteAsync(_oldVersion);
                return;
            }

			// If services have not finished starting up yet, wait.
			var svcWaiter = Services.StartupWaiter;

			if (svcWaiter != null)
			{
				await svcWaiter.Task;
			}

            // Construct the pageWithTokens via first locating the terminal in the router.
            var router = Router.CurrentRouter;

            if (router == null)
            {
                response.StatusCode = 404;
                return;
            }

            var terminalWithTokens = router.ResolveWithTokens(context, pageDetails.Url);

            if (terminalWithTokens == null)
            {
                terminalWithTokens = new TerminalWithTokens()
                {
                    TerminalNode = router.Status_404
                };
            }

            var redirectTerminal = terminalWithTokens.Value.TerminalNode as TerminalRedirectNode;

			if (redirectTerminal != null)
            {
                // Redirecting to the given url, as a 302:
                var writer = Writer.GetPooled();
                writer.Start(null);

                writer.WriteASCII("{\"redirect\":");
                writer.WriteEscaped(redirectTerminal.GetTarget());
                writer.Write((byte)'}');
                await writer.CopyToAsync(response.Body);
                writer.Release();
                return;
            }

			var pageTerminal = terminalWithTokens.Value.TerminalNode as RouterPageTerminal;

			if (pageTerminal == null)
			{
				response.StatusCode = 404;
				return;
			}

            // Build the pageAndTokens, collecting the primary content too:
            var pageWithTokens = new PageWithTokens() {
                PageTerminal = pageTerminal,
                Host = request.Host,
                TokenValues = terminalWithTokens.Value.Tokens
            };

            pageWithTokens.PrimaryService = pageTerminal.GetPrimaryService();
			pageWithTokens.PrimaryObject = await pageTerminal.GetPrimaryObject(context, pageWithTokens);

			await Events.Page.BeforeNavigate.Dispatch(context, pageWithTokens);

            response.ContentType = "application/json";
			await _htmlService.RenderState(context, response, pageWithTokens, pageDetails.Url);
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

        /// <summary>
        /// A location in the router tree.
        /// </summary>
        public struct RouterTreeLocation
        {
            /// <summary>
            /// The URL to resolve relative to. Empty string (not /) indicates site root.
            /// </summary>
            public string Url;
        }
        
        /// <summary>
        /// Details about a node in the routing tree.
        /// </summary>
        public struct TreeNodeDetail
		{
            /// <summary>
            /// The set of children in this node.
            /// </summary>
            public List<RouterNodeMetadata> Children;

            /// <summary>
            /// "This" node (if relevant: the root node does not have this).
            /// </summary>
            public RouterNodeMetadata? Self;
        }

	}
}