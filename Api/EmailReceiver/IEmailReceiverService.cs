using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.EmailReceiver
{
    /// <summary>
	/// Receives emails via SMTP, optionally with TLS.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
    public partial interface IEmailReceiverService
    {
    }
}