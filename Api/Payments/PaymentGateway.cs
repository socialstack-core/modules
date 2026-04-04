using Api.Contexts;
using System;
using System.Threading.Tasks;

namespace Api.Payments;

/// <summary>
/// A generic payment gateway.
/// </summary>
public class PaymentGateway
{
	/// <summary>
	/// The ID of this gateway. Stripe is gateway = 1.
	/// </summary>
	public uint Id;

	/// <summary>
	/// Identifier for the payment gateway passed to 3rd parties etc
	/// </summary>
	public string ShortCode;

	/// <summary>
	/// Process an incoming challenge response from the gateway
	/// </summary>
	/// <param name="purchase"></param>
	/// <param name="challengeResponse"></param>
	/// <returns></returns>
	public virtual ValueTask<PurchaseAndAction> ValidateChallenge(Purchase purchase, ChallengeResponse challengeResponse)
	{
		return new ValueTask<PurchaseAndAction>(new PurchaseAndAction() { });
	}

	/// <summary>
	/// Process an incoming hosted page payment response from the gateway
	/// </summary>
	/// <param name="context"></param>
	/// <param name="purchase"></param>
	/// <param name="hostedPageResponse"></param>
	/// <returns></returns>
	public virtual ValueTask<Purchase> ValidateHostedPageTransaction(Context context, Purchase purchase, HostedPageResponse hostedPageResponse)
	{
		throw new NotImplementedException();
	}


	/// <summary>
	/// Request a payment to occur.
	/// </summary>
	/// <param name="purchase"></param>
	/// <param name="totalCost"></param>
	/// <param name="paymentMethod"></param>
	/// <returns></returns>
	public virtual ValueTask<PurchaseAndAction> ExecutePurchase(Purchase purchase, ProductCost totalCost, PaymentMethod paymentMethod)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Request a card authorisation for the given purchase. The purchase itself will enter the success state if the authorisation occurs.
	/// As a result this should only be used on purchases which are either free or discounted to free with a coupon.
	/// </summary>
	/// <param name="purchase"></param>
	/// <param name="totalCost"></param>
	/// <param name="paymentMethod"></param>
	/// <returns></returns>
	public virtual ValueTask<PurchaseAndAction> AuthorisePurchase(Purchase purchase, ProductCost totalCost, PaymentMethod paymentMethod)
	{
		return new ValueTask<PurchaseAndAction>(new PurchaseAndAction() { });
	}

	/// <summary>
	/// Checks if a token is valid and if so, returns its info. *Do not* send this full information to the frontend. It is API only.
	/// You *may* send the Name and ExpiryUtc fields only.
	/// </summary>
	/// <param name="gatewayToken"></param>
	/// <returns></returns>
	public virtual ValueTask<TokenInformation> GetTokenDetails(string gatewayToken)
	{
		return new ValueTask<TokenInformation>(new TokenInformation() { Valid = false });
	}

	/// <summary>
	/// Prepares the given token. This can be used to, for example, convert a single use token into a multi-use one depending on the gateway.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="gatewayToken"></param>
	/// <returns></returns>
	public virtual ValueTask<string> PrepareToken(Context context, string gatewayToken)
	{
		return new ValueTask<string>(gatewayToken);
	}
}