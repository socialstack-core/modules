using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Pages;
using Api.Permissions;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Payments
{
	/// <summary>
	/// Handles productTemplates.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ProductTemplateService : AutoService<ProductTemplate>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProductTemplateService() : base(Events.ProductTemplate)
        {
			// Example admin page install:
			InstallAdminPages("Product Templates", "fa:fa-box", new string[] { "id", "name" }, null, "ecommerce");

			HashSet<string> excludeFields = new HashSet<string>() { "Categories", "Tags", "VariantOfId", "Sku", "Slug", "Variants", "PriceTiers", "PriceTiersJson" };
			HashSet<string> nonAdminExcludeFields = new HashSet<string>() { "RolePermits", "UserPermits" };

			Events.ProductTemplate.BeforeSettable.AddEventListener((Context ctx, JsonField<ProductTemplate, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<ProductTemplate, uint>>(field);
				}

				// hide the core taxonomy fields as we have product specific ones
				if (excludeFields.Contains(field.Name))
				{
					field.Writeable = false;
					field.Hide = true;
				}

				// only admin can amend the critical fields
				// todo move this into a seperate service for all entites? 
				if (field.ForRole != Roles.Developer && field.ForRole != Roles.Admin && nonAdminExcludeFields.Contains(field.Name))
				{
					field.Writeable = false;
					field.Hide = true;
				}

				if (field.Name == "Attributes" || field.Name == "AdditionalAttributes")
				{
					field.Module = "Admin/Payments/AttributeSelect";
				}

				if (field.Name == "ProductCategories")
				{
					field.Module = "Admin/Payments/ProductCategorySelect";
				}

				return new ValueTask<JsonField<ProductTemplate, uint>>(field);
			});

			Events.ProductTemplate.BeforeCreate.AddEventListener(async (Context context, ProductTemplate productTemplate) =>
			{
				if (productTemplate == null)
				{
					return null;
				}

				// Ensure a slug is generated and is unique.
				if (string.IsNullOrEmpty(productTemplate.Slug))
				{
					productTemplate.Slug = await SlugGenerator.GenerateUniqueSlug(this, context, productTemplate.Name.Get(context));
				}

				await ValidateTemplate(context, productTemplate);

				return productTemplate;
			});

			Events.ProductTemplate.BeforeUpdate.AddEventListener(async (Context context, ProductTemplate toUpdate, ProductTemplate original) =>
			{
				if (toUpdate == null)
				{
					return null;
				}

				// Validate:
				await ValidateTemplate(context, toUpdate);

				return toUpdate;
			});


			// Cache this type:
			Cache();
		}

		private ValueTask<ProductTemplate> ValidateTemplate(Context context, ProductTemplate template)
		{
			if (string.IsNullOrEmpty(template.Name.Get(context)))
			{
				throw new PublicException("The product template name cannot be empty.", "product-validation/no-name");
			}

			if (string.IsNullOrEmpty(template.Slug))
			{
				throw new PublicException("The product template slug cannot be empty.", "product-validation/no-slug");
			}

			return ValueTask.FromResult(template);
		}
	}
    
}
