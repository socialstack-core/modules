using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;

namespace Api.Payments
{
	/// <summary>
	/// Handles productAttributeGroups.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ProductAttributeGroupService : AutoService<ProductAttributeGroup>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProductAttributeGroupService() : base(Events.ProductAttributeGroup)
        {
			// (Don't inject productAttributeService, it already uses this one)

			// Groups need an edit and basic list page, but not a nav menu entry as their 'home' is via the products link.
			InstallAdminPages("Product Attribute Groups", "fa:fa-folder", [ "id", "name" ], null, "ecommerce");
			
			Events.ProductAttributeGroup.BeforeCreate.AddEventListener(async (ctx, prodAttrGrp) => await ValidateGroup(ctx, prodAttrGrp));
			Events.ProductAttributeGroup.BeforeUpdate.AddEventListener(ValidateGroup);

			Cache();
		}

		private ValueTask<ProductAttributeGroup> ValidateGroup(Context ctx, ProductAttributeGroup prodAttrGrp,
			ProductAttributeGroup original = null)
		{
		
			if (string.IsNullOrEmpty(prodAttrGrp.Name.Get(ctx)))
			{
				throw new PublicException("The attribute group name cannot be empty.", "attribute-group-validation/no-name");
			}

			if (string.IsNullOrEmpty(prodAttrGrp.Key))
			{
				throw new PublicException("The attribute group key cannot be empty.", "attribute-group-validation/no-key");
			}

			return ValueTask.FromResult(prodAttrGrp);
		}

	}
    
}
