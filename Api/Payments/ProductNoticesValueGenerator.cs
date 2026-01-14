using Api.Database;
using Api.Startup;
using System.Threading.Tasks;
using System;
using Api.SocketServerLibrary;
using Api.Contexts;
using System.Collections.Generic;
using Api.Eventing;
namespace Api.Payments;


/// <summary>
/// A notice on a product, displayed in a set of one or more.
/// </summary>
public struct ProductNotice
{
	/// <summary>
	/// The type of notice this is. "warning", "error", "info". If unsure leave it on the default.
	/// </summary>
	public string Type = "info";
	
	/// <summary>
	/// The message itself.
	/// </summary>
	public string Message;
	
	/// <summary>
	/// Creates a new info message.
	/// </summary>
	public ProductNotice(string message) {
		Message = message;
	}

	/// <summary>
	/// Creates a new message.
	/// </summary>
	public ProductNotice(string message, string type)
	{
		Type = type;
		Message = message;
	}
}

/// <summary>
/// A product notice set which does not hold the set in 
/// memory but instead writes it directly to the given writer.
/// </summary>
public class ProductNoticeWriter : ProductNoticeSet
{
	private readonly Writer _writer;

	/// <summary>
	/// Creates a new set writer for the given writer.
	/// </summary>
	/// <param name="writer"></param>
	public ProductNoticeWriter(Writer writer) {
		_writer = writer;
	}


	/// <summary>
	/// Adds the given notice to the set.
	/// </summary>
	public override void Add(ProductNotice notice)
	{
		if (_count == 0)
		{
			_writer.Write((byte)'[');
		}
		else
		{
			_writer.Write((byte)',');
		}

		_writer.WriteASCII("{\"type\":");
		_writer.WriteEscaped(notice.Type);
		_writer.WriteASCII(",\"message\":");
		_writer.WriteEscaped(notice.Message);
		_writer.Write((byte)'}');

		_count++;
	}

	/// <summary>
	/// Completes writing to the JSON. Call after all Adds have occurred.
	/// </summary>
	public void Complete() {
		if (_count == 0) {
			_writer.WriteASCII("null");
			return;
		}

		// There was at least one in the set.
		_writer.WriteASCII("]");
	}
}

/// <summary>
/// A set of product notices.
/// </summary>
public class ProductNoticeSet
{
	/// <summary>
	/// The number of notices in the set.
	/// </summary>
	protected uint _count;

	/// <summary>
	/// The number of notices in the set.
	/// </summary>
	public uint Count => _count;

	/// <summary>
	/// Adds the given notice to the set.
	/// </summary>
	/// <param name="notice"></param>
	public virtual void Add(ProductNotice notice)
	{
	}
}

/// <summary>
/// A virtual field value generator for a field called "productNotices". It can only be used on a ProductQuantity or a Product.
/// Automatically instanced and the include field name is derived from the class name by the includes system. See VirtualFieldValueGenerator for more info.
/// </summary>
public partial class ProductNoticesValueGenerator<T, ID> : VirtualFieldValueGenerator<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	private ProductService _productService;
	
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
		if(typeof(T) == typeof(Product))
		{
			var product = (Product)((object)forObject);

			if (product == null)
			{
				writer.WriteASCII("null");
				return;
			}

			var set = new ProductNoticeWriter(writer);

			// Dispatch an event for general purpose event sets:
			await Events.Product.CollectNotices.Dispatch(context, set, null, product);

			set.Complete();
			return;
		}
		else if (typeof(T) == typeof(ProductQuantity))
		{
			var productQuantity = (ProductQuantity)((object)forObject);

			if (productQuantity == null)
			{
				writer.WriteASCII("null");
				return;
			}

			_productService ??= Services.Get<ProductService>();

			// Products are cached - this is really just a dictionary lookup:
			var product = await _productService.Get(context, productQuantity.ProductId, DataOptions.IgnorePermissions);

			if (product == null)
			{
				writer.WriteASCII("null");
				return;
			}

			var set = new ProductNoticeWriter(writer);

			// Dispatch an event for general purpose event sets:
			await Events.Product.CollectNotices.Dispatch(context, set, productQuantity, product);

			set.Complete();
			return;
		}

		writer.WriteASCII("null");
	}
	
	/// <summary>
	/// The type, if any, associated with the value being outputted.
	/// For example, if GetValue outputs only strings, this is typeof(string).
	/// </summary>
	/// <returns></returns>
	public override Type OutputType => typeof(List<ProductNotice>);
}