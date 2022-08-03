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
	/// Request a payment to occur.
	/// </summary>
	/// <returns></returns>
	public virtual ValueTask<Purchase> ExecutePurchase(Purchase purchase, ProductCost totalCost)
	{
		return new ValueTask<Purchase>((Purchase)null);
	}

}