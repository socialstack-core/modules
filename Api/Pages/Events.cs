using Api.CanvasRenderer;
using Api.Pages;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup.Routing;
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

		/// <summary>
		/// Set of events for a permalink.
		/// </summary>
		public static PermalinkEventGroup Permalink;
	}

	/// <summary>
	/// Page entity specific extensions to events.
	/// </summary>
	public class PermalinkEventGroup : EventGroup<Permalink>
	{
		/// <summary>
		/// Runs just before a page terminal is about to be added as a custom behaviour.
		/// You may manipulate the terminal as much as you need here.
		/// </summary>
		public EventHandler<PageTerminalBehaviour> BeforeAddTerminal;
	}

	/// <summary>
	/// Page entity specific extensions to events.
	/// </summary>
	public class PageEventGroup : EventGroup<Page>
	{
		/// <summary>
		/// Canvas node transformation.
		/// </summary>
		public EventHandler<CanvasNode> TransformCanvasNode;
		
		/// <summary>
		/// Called when the HTML head is being generated, just before the closing head tag.
		/// </summary>
		public EventHandler<Writer, PageWithTokens> OnWriteHeadEnd;
		
		/// <summary>
		/// Before a user is about to navigate to a page (the server is generating either just the state or the html for them).
		/// </summary>
		public EventHandler<PageWithTokens> BeforeNavigate;

		/// <summary>
		/// On page install.
		/// </summary>
		public EventHandler<PageBuilder> BeforePageInstall;
		
	}
}