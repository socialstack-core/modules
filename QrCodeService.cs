using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

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
		}
	}
    
}
