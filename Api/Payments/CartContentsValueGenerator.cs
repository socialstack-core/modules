using Api.Database;
using Api.Startup;
using System.Threading.Tasks;
using System;
using Api.SocketServerLibrary;
using Api.Contexts;
using Api.Eventing;
namespace Api.Payments;


/// <summary>
/// Pairing of price and currency.
/// </summary>
public struct PriceCurrency
{
	/// <summary>
	/// E.g. "GBP".
	/// </summary>
	public string CurrencyCode;
	
	/// <summary>
	/// The total in the currency smallest atomic unit (pence/ cents etc)
	/// </summary>
	public uint Amount;

	/// <summary>
	/// The amount excluding tax. 
	/// This value is only different from amount if tax is configured on the site in the PriceService.
	/// </summary>
	public uint AmountLessTax;
}

/// <summary>
/// A virtual field value generator for a field called "cartContents". It can only be used on a ShoppingCart.
/// 
/// Automatically instanced and the include field name is derived from the class name by the includes system. See VirtualFieldValueGenerator for more info.
/// </summary>
public partial class CartContentsValueGenerator<T, ID> : VirtualFieldValueGenerator<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	private ShoppingCartService _cartService;
	
	/// <summary>
	/// Generate the value.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="forObject"></param>
	/// <param name="writer"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public override async ValueTask GetValue(Context context, T forObject, Writer writer, ContextFlags flags)
	{
		if(typeof(T) == typeof(ShoppingCart))
		{
			var cart = (ShoppingCart)((object)forObject);
			var contents = await GetValueForCart(context, cart, writer);
			
			// Falls through to the null otherwise.
			if(contents != null){
				writer.WriteASCII("{\"currencyCode\":");
				writer.WriteEscaped(contents.CurrencyCode);
				writer.WriteASCII(",\"total\":");
				writer.WriteS(contents.Total);
				writer.WriteASCII(",\"totalLessTax\":");
				writer.WriteS(contents.TotalLessTax);
				writer.WriteASCII(",\"errorCode\":");
				writer.WriteEscaped(contents.ErrorCode);
				writer.WriteASCII(",\"errorMessage\":");
				writer.WriteEscaped(contents.ErrorMessage);

				if (Events.ShoppingCart.OnWriteCartContents.HasListeners())
				{
					await Events.ShoppingCart.OnWriteCartContents.Dispatch(context, writer, cart, contents);
				}

				writer.WriteASCII(",\"contents\":[");
				var lines = contents.Contents;
				if (lines != null)
				{
					for (var i = 0; i < lines.Count; i++)
					{
						if (i != 0)
						{
							writer.Write((byte)',');
						}

						var line = lines[i];

						writer.WriteASCII("{\"productQuantityId\":");
						writer.WriteS(line.ProductQuantityId);
						writer.WriteASCII(",\"total\":");
						writer.WriteS(line.Total);
						writer.WriteASCII(",\"totalLessTax\":");
						writer.WriteS(line.TotalLessTax);
						writer.WriteASCII(",\"errorCode\":");
						writer.WriteEscaped(line.ErrorCode);
						writer.WriteASCII(",\"errorMessage\":");
						writer.WriteEscaped(line.ErrorMessage);
						writer.WriteASCII(",\"quantity\":");
						writer.WriteS(line.Quantity);
						writer.WriteASCII(",\"productId\":");
						writer.WriteS(line.ProductId);

						writer.Write((byte)'}');
					}
				}

				writer.WriteASCII("]}");
				return;
			}
		}
		
		writer.WriteASCII("null");
	}

	/// <summary>
	/// Gets the content info for a given cart.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="cart"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public async ValueTask<ProductQuantityPricing> GetValueForCart(Context context, ShoppingCart cart, Writer writer)
	{
		// Get the product:
		if(_cartService == null)
		{
			_cartService = Services.Get<ShoppingCartService>();
		}
		
		if(cart == null)
		{
			return null;
		}
		
		var contents = await _cartService.GetContentPrices(context, cart);

		return contents;
	}
	
	/// <summary>
	/// The type, if any, associated with the value being outputted.
	/// For example, if GetValue outputs only strings, this is typeof(string).
	/// </summary>
	/// <returns></returns>
	public override Type OutputType => typeof(ProductQuantityPricing);
}