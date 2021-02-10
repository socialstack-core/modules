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
		/// During page generation.
		/// </summary>
		public EventHandler<Document> Generated;

		/// <summary>
		/// On admin page install.
		/// </summary>
		public EventHandler<Page, CanvasRenderer.CanvasNode, System.Type, AdminPageType> BeforeAdminPageInstall;
		
	}
}