using Api.Database;
using Api.Startup;
using System.Threading.Tasks;
using System;
using Api.SocketServerLibrary;
using Api.Contexts;
using System.Collections.Generic;
namespace Api.Payments;


/// <summary>
/// A virtual field value generator for a field called "breadcrumb". It can only be used on a Product and returns its category breadcrumb set.
/// Automatically instanced and the include field name is derived from the class name by the includes system. See VirtualFieldValueGenerator for more info.
/// </summary>
public partial class BreadcrumbValueGenerator<T, ID> : VirtualFieldValueGenerator<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	private ProductCategoryService _productCategoryService;
	
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
		if (typeof(T) == typeof(Product) || typeof(T) == typeof(ProductCategory))
		{

			uint primaryCategoryId = 0;

			if (typeof(T) == typeof(Product))
			{
				var product = (Product)((object)forObject);
				primaryCategoryId = product.PrimaryCategoryId;
			}
			else
			{
				var category = (ProductCategory)((object)forObject);
				primaryCategoryId = category.Id;
			}

			if (primaryCategoryId == 0)
			{
				// No category set - no breadcrumbs.
				writer.WriteASCII("null");
				return;
			}
			if (_productCategoryService == null)
			{
				_productCategoryService = Services.Get<ProductCategoryService>();
			}

			var catTree = await _productCategoryService.GetTree(context);

			if (!catTree.IdLookup.TryGetValue(primaryCategoryId, out ProductCategoryNode node))
			{
				writer.WriteASCII("null");
				return;
			}

			var cats = node.BreadcrumbCategories;

			if (cats == null || cats.Count == 0)
			{
				writer.WriteASCII("null");
				return;
			}

			writer.Write((byte)'[');

			for (var i = 0; i < cats.Count; i++)
			{
				if (i != 0)
				{
					writer.Write((byte)',');
				}

				var category = cats[i];

				if (category == null)
				{
					// ??
					writer.WriteASCII("null");
					continue;
				}

				writer.WriteASCII("{\"id\":");
				writer.WriteS(category.Id);
				writer.WriteASCII(",\"name\":");
				writer.WriteEscaped(category.Name.Get(context));
				writer.WriteASCII(",\"primaryUrl\":\"/category/");
				writer.WriteS(category.Slug);
				writer.WriteASCII("\"}");
			}

			writer.Write((byte)']');

			return;
		}
		writer.WriteASCII("null");
	}

	/// <summary>
	/// The type, if any, associated with the value being outputted.
	/// For example, if GetValue outputs only strings, this is typeof(string).
	/// </summary>
	/// <returns></returns>
	public override Type OutputType => typeof(List<CategoryBreadcrumb>);
}

/// <summary>
/// A ProductCategory in the breadcrumb generated include set.
/// </summary>
public struct CategoryBreadcrumb
{
	/// <summary>
	/// Category ID.
	/// </summary>
	public uint Id;

	/// <summary>
	/// The category name.
	/// </summary>
	public string Name;

	/// <summary>
	/// The primary URL.
	/// </summary>
	public string PrimaryUrl;
}