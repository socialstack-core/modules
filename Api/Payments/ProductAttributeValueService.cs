using System.Threading.Tasks;
using Api.CanvasRenderer;
using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using Api.Startup;

namespace Api.Payments
{
	/// <summary>
	/// Handles productAttributeValues.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ProductAttributeValueService : AutoService<ProductAttributeValue>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProductAttributeValueService(PageService pages) : base(Events.ProductAttributeValue)
		{

			pages.Install(
				new PageBuilder()
				{
					Url = "/en-admin/productattribute/${productattribute.id}/values",
					Key = "admin_editor:productattributevalue",
					Title = "Edit product attribute values",
					PrimaryContentType = "ProductAttribute",
					PrimaryContentIncludes = "",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("Admin/Payments/ProductAttribute/ValueEditor")
								// The ProductAttribute referenced by the id in the URL will be passed as a prop called 'attribute'.
								// If it doesn't exist, the page itself 404s.
								.WithPrimaryLink("attribute")
						);
					}
				}
			);
			
			Events.ProductAttributeValue.BeforeCreate.AddEventListener(async (ctx, attrValue) => await ValidateAttributeValue(ctx, attrValue));
			Events.ProductAttributeValue.BeforeUpdate.AddEventListener(ValidateAttributeValue);
			
			// added to prevent orphaned values from showing and being searchable
			// in the Admin Panel, this doesn't delete attribute values from the products themselves,
			// but 40m is useless without knowing its attribute. 
			Events.ProductAttribute.BeforeDelete.AddEventListener(async (ctx, attribute) =>
			{
				// This deletes all attribute values for a specified
				// attribute, this doesn't clean up existing orphans
				// that will happen in a separate function.
				await DeleteAttributeValues(ctx, attribute);
				return attribute;
			});
			
			Cache();
		}
		
		/// <summary>
		/// Removes a product attribute's values when an
		/// attribute is deleted. 
		/// </summary>
		/// <param name="context"></param>
		/// <param name="attribute"></param>
		/// <returns></returns>
		public async ValueTask DeleteAttributeValues(Context context, ProductAttribute attribute)
		{
			var values = await Where("ProductAttributeId = ?", DataOptions.IgnorePermissions)
				.Bind(attribute.Id)
				.ListAll(context);

			foreach (var value in values)
			{
				await Delete(context, value);
			}
		}
		
		private ValueTask<ProductAttributeValue> ValidateAttributeValue(Context context, ProductAttributeValue attrValue, ProductAttributeValue original = null)
		{

			if (string.IsNullOrEmpty(attrValue.Value))
			{
				throw new PublicException("The attribute value cannot be empty.", "attribute-value-validation/no-value");
			}

			return ValueTask.FromResult(attrValue);
		}

	}
    
}
