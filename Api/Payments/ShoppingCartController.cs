using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles shoppingCart endpoints.</summary>
    [Route("v1/shoppingCart")]
	public partial class ShoppingCartController : AutoController<ShoppingCart>
    {
    }
}