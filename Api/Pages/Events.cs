using Api.Pages;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// All page entity events.
		/// </summary>
		public static PageEventGroup Page;
	}

	/// <summary>
	/// Page entity specific extensions to events.
	/// </summary>
	public class PageEventGroup : EventGroup<Page>
	{
		/// <summary>
		/// Called just before a page is about to resolve from the given URL and search query string.
		/// This can be used for e.g. enforcing universal access requirements.
		/// </summary>
		public EventHandler<PageWithTokens, string, Microsoft.AspNetCore.Http.QueryString> BeforeResolveUrl;

		/// <summary>
		/// A URL is parsed, and then it is resolved. This happens just before the parse phase and is essentially the very first thing that happens.
		/// </summary>
		public EventHandler<UrlInfo, Microsoft.AspNetCore.Http.QueryString> BeforeParseUrl;

		/// <summary>
		/// During page generation.
		/// </summary>
		public EventHandler<Document> Generated;
		
		/// <summary>
		/// Before a user is about to navigate to a page (the server is generating either just the state or the html for them).
		/// </summary>
		public EventHandler<Page, string> BeforeNavigate;

		/// <summary>
		/// Url lookup is ready. Use to add e.g. custom redirects to the lookup tree.
		/// </summary>
		public EventHandler<UrlLookupCache> AfterLookupReady;

		/// <summary>
		/// Just before adding a particular terminal.
		/// </summary>
		public EventHandler<UrlLookupTerminal> BeforeAddTerminal;

		/// <summary>
		/// On admin page install.
		/// </summary>
		public EventHandler<Page, CanvasRenderer.CanvasNode, System.Type, AdminPageType> BeforeAdminPageInstall;
		
	}
}