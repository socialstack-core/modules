using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Pages;

namespace Api.QrCodes
{
	/// <summary>
	/// Handles qrCodes.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class QrCodeService : AutoService<QrCode>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public QrCodeService() : base(Events.QrCode)
        {
			// Example admin page install:
			InstallAdminPages("Qr Codes", "fa:fa-qrcode", new string[] { "id", "name" });


			// Event which triggers after the page lookup is ready.
			// Can be used to add custom redirects.
			Events.Page.AfterLookupReady.AddEventListener((Context context, UrlLookupCache cache) =>
			{
				if (cache == null)
				{
					return new ValueTask<UrlLookupCache>(cache);
				}

				cache.Add("/qr/{qrcode.id}", async (UrlInfo urlInfo, UrlLookupNode node, List<string> tokenValues) => {

					if (tokenValues == null || tokenValues.Count == 0)
					{
						// 404
						return null;
					}

					// Get the QR code:
					if (!uint.TryParse(tokenValues[0], out uint id))
					{
						return null;
					}

					// Get the QR code:
					var qr = await Get(context, id);

					if (qr == null)
					{
						return null;
					}

					return qr.Url;
				});

				return new ValueTask<UrlLookupCache>(cache);
			});
		}
	}
    
}
