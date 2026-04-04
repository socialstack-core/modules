using Api.CanvasRenderer;
using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using Api.Permissions;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Payments
{
	/// <summary>
	/// Handles products.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ProductService : AutoService<Product>
	{
		private PermalinkService _permalinks;
		private ProductConfig _config;
		private ProductCategoryService _categories;

		private bool _isPermalinkSyncRunning = false;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProductService(PermalinkService permalinks, PageService pages) : base(Events.Product)
		{
			_permalinks = permalinks;
			_config = GetConfig<ProductConfig>();

			InstallAdminPages("Products", "fa:fa-shopping-basket", ["id", "name", "minQuantity"], null, "ecommerce");

			HashSet<string> excludeFields = new HashSet<string>() { "Score", "Categories", "Tags" };
			HashSet<string> nonAdminExcludeFields = new HashSet<string>() { "RolePermits", "UserPermits" };

			// expose all category names into search index 
			EventGroup.SearchMetaData.AddEventListener(async (Context ctx, HashSet<string> metadataText, Product product) =>
			{
				var mappings = product.Mappings.Get("productcategories");

				if (mappings is null)
				{
					return metadataText;
				}

				if (metadataText == null)
				{
					metadataText = new HashSet<string>();
				}

				// couldn't add as a dep due to circular ref
				if (_categories == null)
				{
					_categories = Services.Get<ProductCategoryService>();
				}

				// contains all the uint => PCNode mappings, can be null too.
				var tree = await _categories.GetLookup(ctx);

				foreach (var catId in mappings)
				{
					if (!tree.TryGetValue((uint)catId, out var categoryNode))
					{
						continue;
					}

					if (categoryNode?.BreadcrumbCategories is null || categoryNode.BreadcrumbCategories.Count == 0)
					{
						continue;
					}

					foreach (var crumb in categoryNode.BreadcrumbCategories)
					{
						if (crumb.Id == 1)
						{
							continue;
						}

						metadataText.Add(crumb.Name.GetStringValue(ctx));
					}
				}

				return metadataText;
			}, 10);

			Events.Product.BeforeGettable.AddEventListener((Context ctx, JsonField<Product, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<Product, uint>>(field);
				}

				if (field.Name == "ProductTemplateId" || field.Name.ToLower() == "producttemplate")
				{
					field.Readable = false;
					field.Hide = true;
				}

				if (field.Name == "FeatureRef")
				{
					// Handle required state here as this field is not required on ProductTemplate
					// (a child class of Product) but is required on Product itself.
					field.Data["required"] = true;
					field.Data["validate"] = "Required";
				}

				return new ValueTask<JsonField<Product, uint>>(field);
			});

			Events.Product.BeforeSettable.AddEventListener((Context ctx, JsonField<Product, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<Product, uint>>(field);
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

				if (field.Name == "ProductComponents")
				{
					field.Data["tab"] = "components";
					field.Module = "Admin/Payments/ProductComponents/ValueEditor";

					// Set value method:
					field.OnSetValue.AddEventListener((Context ctx, object value, Product target, JToken token) =>
					{
						var productComponentArray = token as JArray;

						if (productComponentArray != null)
						{
							foreach (var productComponent in productComponentArray)
							{
								if (productComponent == null || productComponent.Type != JTokenType.Object)
								{
									continue;
								}

								// We have a product component object.
								var idField = productComponent["id"];
								var deleteComponentField = productComponent["deleteComponent"];
								var updateComponentField = productComponent["updateComponent"];

								// Load it as a partial:
								target.AddTemporaryComponentInfo(new PartialProductComponent()
								{
									ProductQuantity = productComponent as JObject,
									Id = idField == null ? 0 : idField.Value<uint>(),
									DeleteComponent = deleteComponentField == null ? false : deleteComponentField.Value<bool>(),
									UpdateComponent = updateComponentField == null ? false : updateComponentField.Value<bool>()
								});
							}
						}

						// The product's _tempComponents set is now loaded and gets
						// dealt with by the BeforeCreate and BeforeUpdate event handlers.
						// Before* is involved because we need to also set the productComponents mappings set.

						// Returning null blocks the default id array behaviour 
						// but allows us to do custom value loading instead.

						return new ValueTask<object>((object)null);
					});
				}


				if (field.Name == "Variants")
				{
					field.Module = "Admin/Payments/Variants/ValueEditor";

					// Set value method:
					field.OnSetValue.AddEventListener((Context ctx, object value, Product target, JToken token) =>
					{

						var variantArray = token as JArray;

						if (variantArray != null)
						{
							foreach (var variant in variantArray)
							{
								if (variant == null || variant.Type != JTokenType.Object)
								{
									continue;
								}

								// We have a variant object.
								var idField = variant["id"];
								var deleteVariantField = variant["deleteVariant"];

								// Load it as a partial:
								target.AddTemporaryVariantInfo(new PartialProductVariant()
								{
									Product = variant as JObject,
									Id = idField == null ? 0 : idField.Value<uint>(),
									DeleteVariant = deleteVariantField == null ? false : deleteVariantField.Value<bool>(),
								});
							}
						}

						// The product's _tempVariants set is now loaded and gets
						// dealt with by the BeforeCreate, AfterCreate and BeforeUpdate event handlers.
						// AfterCreate is involved because a variant needs the VariantOfId.
						// Before* is involved because we need to also set the variants mappings set.

						// Returning null blocks the default id array behaviour 
						// but allows us to do custom value loading instead.

						return new ValueTask<object>((object)null);
					});
				}

				return new ValueTask<JsonField<Product, uint>>(field);
			});

			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
			{
				if (builder.ContentType == typeof(Product) &&
					(builder.PageType == CommonPageType.AdminEdit || builder.PageType == CommonPageType.AdminAdd)
				)
				{
					builder.AddAdminTab(new AdminTab("Components", "components"));
					builder.AddAdminTab(new AdminTab("Link to parent", "linkToParent"));
				}

				return new ValueTask<PageBuilder>(builder);
			});

			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
			{
				if (builder.ContentType == typeof(Product) && builder.PageType == CommonPageType.AdminList)
				{
					builder.GetContentRoot()
						.Empty()
						.AppendChild(new CanvasNode("Admin/Payments/ProductCategoryTree"));
				}
				else if (
					builder.ContentType == typeof(Product) &&
					(builder.PageType == CommonPageType.AdminEdit || builder.PageType == CommonPageType.AdminAdd)
				)
				{
					// Use wrapper around AutoForm (same props).
					// This editor simply ensures that variants are hidden when the product type is not a variant one.
					var formNode = builder.GetContentRoot().Find("Admin/AutoForm");
					if (formNode != null)
					{
						formNode.Module = "Admin/Payments/ProductEditor";
					}

					builder.PrimaryContentIncludes += ",productComponents.product,productCategories.requiredAttributes,productTemplate.requiredAttributes,variants.additionalAttributes";
				}

				return new ValueTask<PageBuilder>(builder);
			}, 30); // After all other autoform tabs etc have been added

			pages.Install(
				// Install a default primary product category page.
				// Note that this does not define a URL, because we want nice readable slug based URLs.
				// Because slugs can change, the URL is therefore not necessarily constant and thus
				// must be handled at the permalink level, which the event handler further down does.
				new PageBuilder()
				{
					Key = "primary:product",
					PrimaryContentIncludes = "coshhDocuments,productImages,productDownloads,productCategories,attributes,attributes.attribute,calculatedPrice,variants,variants.calculatedPrice,variants.additionalAttributes,variants.additionalAttributes.attribute,variants.attributes.attribute,variants.calculatedPrice,breadcrumb,suggestions,suggestions.primaryUrl,suggestions.calculatedPrice",
					Title = "${product.name}",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/Product/View").WithPrimaryLink("product")
						);
					}
				},
				new PageBuilder()
				{
					Key = "product_search",
					Url = "product/search",
					Title = "Search for products",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("UI/Product/Search").With("showPromotions", true)
						);
					}
				}
			);

			Events.Product.BeforeCreate.AddEventListener(async (Context context, Product product) =>
			{
				if (product == null)
				{
					return null;
				}

				// Ensure a slug is generated and is unique.
				if (string.IsNullOrEmpty(product.Slug))
				{
					product.Slug = await SlugGenerator.GenerateUniqueSlug(this, context, product.Name.Get(context));
				}

				await ValidateProduct(context, product);
				await UpdateProductCategoryMapping(context, product);

				return product;
			});

			Events.Product.AfterCreate.AddEventListener(async (Context context, Product product) =>
			{
				// Permalink target which will be for whichever page wants to handle a product as its primary content.
				// If a specific page for this product exists, it will ultimately pick that.
				var linkTarget = permalinks.CreatePrimaryTargetLocator(this, product);

				Product parentProduct = null;

				if (product.VariantOfId.HasValue && product.VariantOfId.Value != 0)
				{
					parentProduct = await Get(context, product.VariantOfId.Value);

					if (parentProduct == null)
					{
						throw new PublicException(
							"Product is a variant of #" + product.VariantOfId + " but that product does not exist.",
							"product/parent_not_found"
						);
					}
				}

				await permalinks.Create(
					context,
					new Permalink()
					{
						Url = GetInitialProductUrl(product, parentProduct),
						Target = linkTarget
					},
					DataOptions.IgnorePermissions
				);

				// If there are any temp variants, trigger an immediate update on this product.
				// This causes BeforeUpdate to run and ensures that the products are
				// created with the correct VariantOfId striaght away.
				var tempVariants = product.GetTemporaryVariants();

				if (tempVariants != null)
				{
					product = await Update(context, product, (Context ctx, Product toUpdate, Product orig) =>
					{
						// Need to pass the temp fields to the object to update:
						toUpdate.SetTemporaryVariants(tempVariants);
					}, DataOptions.IgnorePermissions);
				}

				return product;
			});

			Events.Product.BeforeUpdate.AddEventListener(async (Context context, Product toUpdate, Product original) =>
			{

				if (toUpdate == null)
				{
					return null;
				}

				// Validate:
				await ValidateProduct(context, toUpdate, false);

				Product parentProduct = null;

				if (toUpdate.VariantOfId.GetValueOrDefault() != 0)
				{
					parentProduct = await Get(context, toUpdate.VariantOfId.Value);

					if (parentProduct == null)
					{
						throw new PublicException(
							"Product is a variant of #" + toUpdate.VariantOfId.GetValueOrDefault() + " but that product does not exist.",
							"product/parent_not_found"
						);
					}
				}

				// Did the slug, sku or parent state change?
				if (
					toUpdate.VariantOfId != original.VariantOfId || // Variant of changed (usually to/from 0)
					(toUpdate.Slug != original.Slug && toUpdate.VariantOfId.GetValueOrDefault() == 0) || // Slug changed and not a variant
					(toUpdate.Sku != original.Sku && toUpdate.VariantOfId.GetValueOrDefault() != 0) // Sku changed and is a variant
				)
				{

					// Update the permalink. It's possible that this will attempt to create a duplicate
					// in which case it functionally acts like the requested one is the new canonical link.
					var permalinkInfo = new PermalinkUrlTarget()
					{
						Url = GetInitialProductUrl(toUpdate, parentProduct),
						Target = _permalinks.CreatePrimaryTargetLocator(this, toUpdate)
					};

				await _permalinks.BulkCreate(context, [permalinkInfo]);
				}

				// Handle VariantOfId change - update the variants mappings on the parent products
				if (toUpdate.VariantOfId != original.VariantOfId)
				{
					// If was previously a variant, remove from old parent's variants mapping
					if (original.VariantOfId.HasValue && original.VariantOfId.Value != 0)
					{
						var oldParent = await Get(context, original.VariantOfId.Value);
						if (oldParent != null)
						{
							await Update(context, oldParent, (ctx, parent, orig) =>
							{
								parent.Mappings.Remove("variants", toUpdate.Id);
							}, DataOptions.IgnorePermissions);
						}
					}

					// If is now a variant, add to new parent's variants mapping
					if (toUpdate.VariantOfId.HasValue && toUpdate.VariantOfId.Value != 0)
					{
						var newParent = await Get(context, toUpdate.VariantOfId.Value);
						if (newParent != null)
						{
							await Update(context, newParent, (ctx, parent, orig) =>
							{
								parent.Mappings.Add("variants", toUpdate.Id);
							}, DataOptions.IgnorePermissions);
						}
					}
				}

				//Update prices
				toUpdate.UpdatePricesJson();

				if (toUpdate.Mappings.Changed("productcategories", original.Mappings))
				{
					await UpdateProductCategoryMapping(context, toUpdate);
				}

				var variantSet = toUpdate.GetTemporaryVariants();

				if (variantSet != null)
				{
					// 1. Create any that are needed
					// 2. Update "variants" Mappings
					// 3. Delete any that have been marked as such
					List<ulong> mappings = new List<ulong>();

					foreach (var variantInfo in variantSet)
					{
						if (variantInfo.DeleteVariant)
						{
							if (variantInfo.Id == 0)
							{
								// Ignore this one - it never existed anyway.
								continue;
							}

							// Delete it now:
							await Delete(context, variantInfo.Id);

							continue;
						}

						if (variantInfo.Id == 0)
						{
							// Creating a new product. Following the same pattern as AutoController.

							// Using InstanceType such that it supports any dynamically added fields as well:
							var variantProduct = (Product)Activator.CreateInstance(InstanceType);
							await SetFieldsOnObject(variantProduct, context, variantInfo.Product);
							variantProduct.VariantOfId = toUpdate.Id;
							variantProduct.UserId = context.UserId;

							// Not permitted to create with a specified ID via the API. Ensure it's 0:
							variantProduct.SetId(default);

							// And the create call itself:
							variantProduct = await Create(context, variantProduct);

							if (variantProduct != null)
							{
								mappings.Add(variantProduct.Id);
							}
						}
						else
						{
							// Update. Load the product such that we can set the changed fields on it, and again like above 
							// we're just following what AutoController does.
							var originalEntity = await Get(context, variantInfo.Id);

							if (originalEntity == null)
							{
								continue;
							}

							var entityToUpdate = StartUpdate(context, originalEntity);
							await SetFieldsOnObject(entityToUpdate, context, variantInfo.Product);

							// Make sure it's still the original ID:
							entityToUpdate.SetId(variantInfo.Id);
							entityToUpdate.VariantOfId = toUpdate.Id;

							entityToUpdate = await FinishUpdate(context, entityToUpdate, originalEntity);

							if (entityToUpdate != null)
							{
								mappings.Add(entityToUpdate.Id);
							}
						}
					}

					// Finally update the mapping set.
					toUpdate.Mappings.Set("variants", mappings);
				}

				return toUpdate;
			});

			// Added to make sure the ContinueSellingWithNoStock,Hidden and category Mappings of the parent
			// is mirrored to variants. This also doesn't execute when a variant
			// is updated. Only parent products.

			Events.Product.AfterUpdate.AddEventListener(async (Context ctx, Product product, ChangedFields diff) =>
			{
				if (!diff.HasChanged("ContinueSellingWithNoStock") && !diff.HasChanged("Mappings") && !diff.HasChanged("Hidden"))
				{
					return product;
				}

				// first, lets check if this is not a variant. 
				// if it is, lets exit early.
				if (product.ProductType != 2 || (product.VariantOfId.HasValue && product.VariantOfId.Value != 0))
				{
					return product;
				}

				// here we know it's not a product, lets get all variants
				var variants = await Where("VariantOfId = ?", DataOptions.IgnorePermissions)
					.Bind(product.Id)
					.ListAll(ctx);

				// iterate all the variants, if the variant
				// shares the same value than the parent
				// we skip the unnecessary update call. 
				// else we update to keep it in sync. 
				foreach (var variant in variants)
				{
					if (variant.Id == product.Id)
					{
						continue;
					}

					var needsUpdate = false;

					if (variant.ContinueSellingWithNoStock != product.ContinueSellingWithNoStock)
					{
						needsUpdate = true;
					}

					if (variant.Mappings.Changed("productcategories", product.Mappings))
					{
						needsUpdate = true;
					}

					if (variant.Hidden != product.Hidden)
					{
						needsUpdate = true;
					}

					if (!needsUpdate)
					{
						continue;
					}

					await Update(ctx, variant, (_, updateVariant, _) =>
					{
						updateVariant.Mappings.Set("productcategories", product.Mappings.Get("productcategories"));
						updateVariant.ContinueSellingWithNoStock = product.ContinueSellingWithNoStock;
						updateVariant.Hidden = product.Hidden;
					});
				}

				return product;
			});

#if !DEBUG
			Cache();
#endif
		}

		private async ValueTask UpdateProductCategoryMapping(Context context, Product product)
		{
			var mappings = product.Mappings.Get("productcategories");

			if (mappings is null)
			{
				product.Mappings.Remove("childOfCategories");
				return;
			}

			// couldn't add as a dep due to circular ref
			if (_categories == null)
			{
				_categories = Services.Get<ProductCategoryService>();
			}

			// contains all the uint => PCNode mappings, can be null too.
			var tree = await _categories.GetLookup(context);

			var allCats = new List<ulong>();

			foreach (var catId in mappings)
			{
				if (!tree.TryGetValue((uint)catId, out var categoryNode))
				{
					continue;
				}
				if (categoryNode?.BreadcrumbCategories is null || categoryNode.BreadcrumbCategories.Count == 0)
				{
					continue;
				}

				allCats.AddRange(categoryNode.BreadcrumbCategories.Select(category => (ulong)category.Id));
			}

			product.Mappings.Set("childOfCategories", allCats.Distinct().ToList());

		}

		/// <summary>
		/// Adds a validation layer to <c>Product</c> only,
		/// this checks fields strictly on the product.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="product"></param>
		/// <param name="creating"></param>
		/// <returns></returns>
		/// <exception cref="PublicException"></exception>
		private async ValueTask<Product> ValidateProduct(Context context, Product product, bool creating = true)
		{
			if (string.IsNullOrEmpty(product.Name.Get(context)))
			{
				throw new PublicException("The product name cannot be empty.", "product-validation/no-name");
			}

			if (string.IsNullOrEmpty(product.Slug))
			{
				throw new PublicException("The product slug cannot be empty.", "product-validation/no-slug");
			}

			if (creating && !string.IsNullOrEmpty(product.Sku))
			{
				var existingProduct = await Where("Sku=?", DataOptions.IgnorePermissions)
					.Bind(product.Sku)
					.First(context);

				if (existingProduct != null)
				{
					throw new PublicException("The product sku already exists.", "product-validation/duplicate-sku");
				}
			}

			return product;
		}

		/// <summary>
		/// Use the primaryUrl system instead of calling this directly.
		/// </summary>
		/// <param name="product"></param>
		/// <param name="parentProduct">Present if the product is a variant.</param>
		/// <returns></returns>
		private string GetInitialProductUrl(Product product, Product parentProduct)
		{
			if (parentProduct != null)
			{
				// Product is a variant of parentProduct.
				return "/product/" + parentProduct.Slug + "?sku=" + product.Sku;
			}

			return "/product/" + product.Slug;
		}

		/// <summary>
		/// Checks whether the sync is running
		/// </summary>
		public bool IsSyncRunning
		{
			get
			{
				return _isPermalinkSyncRunning;
			}
		}

		/// <summary>
		/// A convenience brute-force method for ensuring that all required permalinks exist.
		/// Best used after a major database edit (such as importing outside of SS).
		/// </summary>
		/// <returns></returns>
		public async ValueTask SyncPermalinks(Context context)
		{
			// This method is about to be exposed to an endpoint, in order to stop this from 
			// firing multiple times, let's add a blocker.
			if (_isPermalinkSyncRunning)
			{
				return;
			}

			_isPermalinkSyncRunning = true;
			Log.Warn("product", "Sync product permalinks");

			try
			{
				var allProducts = await Where("", DataOptions.IgnorePermissions).ListAll(context);
				var links = new List<PermalinkUrlTarget>();

				// Created if any variants exist
				Dictionary<uint, Product> lookup = null;

				foreach (var product in allProducts)
				{
					// Permalink target which will be for whichever page wants to handle a product as its primary content.
					// If a specific page for this product exists, it will ultimately pick that.
					var linkTarget = _permalinks.CreatePrimaryTargetLocator(this, product);

					await Update(context, product, async (ctx, product, original) =>
					{
						// noop. This triggers the UpdateProductCategoryMapping method. 
						await UpdateProductCategoryMapping(ctx, product);
					});

					Product parentProduct = null;
					if (product.VariantOfId.HasValue && product.VariantOfId.Value != 0)
					{
						lookup ??= allProducts.ToDictionary(p => p.Id);

						if (!lookup.TryGetValue(product.VariantOfId.Value, out parentProduct))
						{
							// Invalid variant!
							Log.Warn(LogTag, $"Invalid variant: {product.Id} is a variant of #{product.VariantOfId.Value} but that parent product doesn't exist.");
							continue;
						}
					}

					var permalinkInfo = new PermalinkUrlTarget()
					{
						Url = GetInitialProductUrl(product, parentProduct),
						Target = linkTarget
					};

					links.Add(permalinkInfo);
				}

				await _permalinks.BulkCreate(context, links);
			}
			finally
			{
				_isPermalinkSyncRunning = false;
			}
		}


		/// <summary>
		/// True if it should error if an order for less than the min is placed.
		/// Otherwise it will be rounded up.
		/// </summary>
		public bool ErrorIfBelowMinimum => _config.ErrorIfBelowMinimum;

		/// <summary>
		/// Gets the product tiers for a given product. The result is null if there are none.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<PriceComparison> GetPriceTiers(Context context, Product product)
		{
			if (product == null)
			{
				return null;
			}

			// System generated contextual pricing if necessary:
			var prices = new PriceComparison();

			//Get the default and custom prices
			prices.Original = product.PriceTiers;
			prices.Discounted = await Events.Product.Pricing.Dispatch(context, prices.Discounted, product);

			return prices;
		}
	}

	/// <summary>
	/// A class that contains the default price tiers of a product and the discounted price tiers, if they exist
	/// </summary>
	public class PriceComparison
	{
		/// <summary>
		/// The default price tiers
		/// </summary>
		public List<Price> Original;

		/// <summary>
		/// The discounted price tiers
		/// </summary>
		public List<Price> Discounted;

		private List<Price> _tiersToUse;

		/// <summary>
		/// Find the tiers to be used
		/// </summary>
		public List<Price> TiersToUse
		{
			get
			{
				if (_tiersToUse != null)
				{
					return _tiersToUse;
				}

				//Check what is populated
				if (Discounted != null && Discounted.Count > 0)
				{
					_tiersToUse = Discounted;
				}
				else
				{
					_tiersToUse = Original;
				}

				return _tiersToUse;
			}
			set => _tiersToUse = value;
		}
	}

}
