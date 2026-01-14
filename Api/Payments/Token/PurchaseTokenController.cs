using Api.Contexts;
using Api.Users;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Payments
{
	/// <summary>Handles purchaseToken endpoints.</summary>
	[Route("v1/purchaseToken")]
	public partial class PurchaseTokenController : AutoController<PurchaseToken>
	{

		/// <summary>
		/// Check if token exists and has not expired yet.
		/// </summary>
		[HttpGet("token/{token}")]
		public async ValueTask<object> CheckTokenExists(Context context, [FromRoute] string token)
		{
			var svc = (_service as PurchaseTokenService);

			var request = await svc.Get(context, token);

			if (request == null)
			{
				return null;
			}

			return new
			{
				token
			};
		}
	}
}