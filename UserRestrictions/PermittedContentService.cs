using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Permissions
{
	/// <summary>
	/// Handles permittedContents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PermittedContentService : AutoService<PermittedContent>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PermittedContentService() : base(Events.PermittedContent)
        {
			// Example admin page install:
			// InstallAdminPages("PermittedContents", "fa:fa-rocket", new string[] { "id", "name" });
			
			// Caching is required by this service:
			Cache();
			
			Events.PermittedContent.AfterLoad.AddEventListener(async (Context context, PermittedContent permit) => {

				if (permit == null)
				{
					permit.Permitted = await Content.Get(context, permit.PermittedContentTypeId, permit.PermittedContentId);
				}

				return permit;
			});

			Events.PermittedContent.AfterList.AddEventListener(async (Context context, List<PermittedContent> permits) => {

				if (permits == null)
				{
					return permits;
				}

				// Can be mixed content so we'll use the Content.ApplyMixed helper:
				await Content.ApplyMixed(
					context,
					permits,
					src =>
					{
						// Never invoked with null.
						var uae = (PermittedContent)src;
						return new ContentTypeAndId(uae.PermittedContentTypeId, uae.PermittedContentId);
					},
					(object src, object content) =>
					{
						var uae = (PermittedContent)src;
						uae.Permitted = content;
					}
				);

				return permits;
			});

			// Caching these has a general performance improvement given many filters use them.
			Cache();
		}
	}
    
}
