using Microsoft.AspNetCore.Mvc;

namespace Api.QrCodes
{
    /// <summary>Handles qrCode endpoints.</summary>
    [Route("v1/qrCode")]
	public partial class QrCodeController : AutoController<QrCode>
    {
    }
}