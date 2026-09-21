using Api.CanvasRenderer;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System.Threading.Tasks;

namespace Api.Pages
{

	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class PermalinkInit
	{

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public PermalinkInit()
		{

			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
			{
				if (builder.PageType == CommonPageType.AdminEdit && builder.ContentType != typeof(Page))
				{
					builder.AddAdminTab(new AdminTab("Links and display", "permalinks")
					{
						Content = new CanvasNode("Admin/Permalink/List").With("contentType", builder.ContentType?.Name).WithPrimaryLink("content")
					});
				}
				return new ValueTask<PageBuilder>(builder);
			}, 15);

		}
	}
}