using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System.Text.RegularExpressions;
using Api.CanvasRenderer;
using Api.Pages;
using System;
using System.Linq;
using Api.Startup;
using Api.Startup.Routing;
using static Api.Pages.PageController;
using Api.Translate;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Api.Payments
{
	/// <summary>
	/// Handles productAttributes.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ProductAttributeService : AutoService<ProductAttribute>
    {
		private ProductAttributeGroupService _groups;
		private ProductAttributeValueService _attributeValues;
		private RoleService _roles;
		
		/// <summary>
		/// Added in as a cached list to allow all roles to be loaded
		/// and checks put against each one to decide who can edit
		/// an attribute, and who can't 
		/// </summary>
		private readonly Dictionary<Role, bool> _canEditAttribute = []; 

		/// <summary>
		/// The cached attribute tree. Use GetTree to obtain one instead of this directly.
		/// </summary>
		private AttributeGroupTree? _attributeTree;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProductAttributeService(ProductAttributeGroupService groups, ProductAttributeValueService attributeValues, RoleService roles) : base(Events.ProductAttribute)
        {
			_groups = groups;
			_attributeValues = attributeValues;
			_roles = roles;

			// Example admin page install:
			InstallAdminPages("Product Attributes", "fa:fa-tags", ["id", "name"], null, "ecommerce");
			
			Events.Role.AfterUpdate.AddEventListener(OnRoleMutate);
			Events.Role.AfterCreate.AddEventListener(OnRoleMutate);
			
			Events.Role.AfterDelete.AddEventListener((ctx, role) =>
			{
				_canEditAttribute.Remove(role);
				return ValueTask.FromResult(role);
			});
			
			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
			{
				if (builder.ContentType == typeof(ProductAttribute) && builder.PageType == CommonPageType.AdminList)
				{
					builder.GetContentRoot()
						.Empty()
						.AppendChild(new CanvasNode("Admin/Payments/ProductAttributeTree"));
				}

				return new ValueTask<PageBuilder>(builder);
			});
			
			Events.ProductAttribute.BeforeCreate.AddEventListener(async (ctx, attr) => {
				if (attr == null)
				{
					return attr;
				}

				// validate and if necessary generate key
				await ValidateAttribute(ctx, attr);
								
				return attr;
			});

			Events.ProductAttribute.BeforeUpdate.AddEventListener(async (Context ctx, ProductAttribute attr, ProductAttribute origAttr) => {
				
				if (attr == null)
				{
					return attr;
				}
			
				// validate and if necessary generate key
				await ValidateAttribute(ctx, attr);

				// todo trigger product refresh
				return attr;
			});
			
			Events.ProductAttribute.AfterCreate.AddEventListener(ClearCache);
			Events.ProductAttribute.AfterUpdate.AddEventListener(ClearCache);
			Events.ProductAttribute.AfterDelete.AddEventListener(ClearCache);

			Events.ProductAttributeGroup.AfterCreate.AddEventListener(ClearCache);
			Events.ProductAttributeGroup.AfterUpdate.AddEventListener(ClearCache);
			Events.ProductAttributeGroup.AfterDelete.AddEventListener(ClearCache);

			// Install some default content.
			Events.Service.AfterStart.AddEventListener(async (Context context, object svc) => {

				// If it doesn't exist at all, it's created.
				var groupLookup = await InstallDefaultGroups(
					context,
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Physical Attributes"),
						Key = "physical"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Dimensions"),
						Key = "dimensions",
						ParentGroupKey = "physical"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Weight"),
						Key = "weight",
						ParentGroupKey = "physical"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Material & Build"),
						Key = "material_build",
						ParentGroupKey = "physical"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Performance & Technicals"),
						Key = "performance_technicals"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Power & Energy"),
						Key = "power_energy",
						ParentGroupKey = "performance_technicals"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Computing"),
						Key = "computing",
						ParentGroupKey = "performance_technicals"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Mechanical"),
						Key = "mechanical",
						ParentGroupKey = "performance_technicals"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Load Ranges"),
						Key = "load_ranges",
						ParentGroupKey = "performance_technicals"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Chemical"),
						Key = "chemical",
						ParentGroupKey = "performance_technicals"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Usage & Compatibility"),
						Key = "usage_compatibility"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Application"),
						Key = "application",
						ParentGroupKey = "usage_compatibility"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Compatibility"),
						Key = "compatibility",
						ParentGroupKey = "usage_compatibility"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Aesthetic & Style"),
						Key = "aesthetic_style"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Design"),
						Key = "design",
						ParentGroupKey = "aesthetic_style"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Branding"),
						Key = "branding",
						ParentGroupKey = "aesthetic_style"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Packaging & Logistics"),
						Key = "packaging_logistics"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Packaging"),
						Key = "packaging",
						ParentGroupKey = "packaging_logistics"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Shipping"),
						Key = "shipping",
						ParentGroupKey = "packaging_logistics"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Regulatory & Certifications"),
						Key = "regulatory_certifications"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Compliance"),
						Key = "compliance",
						ParentGroupKey = "regulatory_certifications"
					},
					new ProductAttributeGroup()
					{
						Name = new Localized<string>("Warranty"),
						Key = "warranty",
						ParentGroupKey = "regulatory_certifications"
					}
				);

				// Next, install default attributes. 
				await InstallDefaults(context, groupLookup, 
					new ProductAttribute {
						Name = new Localized<string>("Width"),
						ProductAttributeType = 1, // long
						Units = "mm",
						ProductAttributeGroupKey = "dimensions"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Height"),
						ProductAttributeType = 2, // double
						Units = "mm",
						ProductAttributeGroupKey = "dimensions"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Length"),
						ProductAttributeType = 2, // double
						Units = "mm",
						ProductAttributeGroupKey = "dimensions"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Depth"),
						ProductAttributeType = 2, // double
						Units = "mm",
						ProductAttributeGroupKey = "dimensions"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Diameter"),
						ProductAttributeType = 2, // double
						Units = "mm",
						ProductAttributeGroupKey = "dimensions"
					},					
					new ProductAttribute
					{
						Name = new Localized<string>("Volume"),
						ProductAttributeType = 2, // double
						Units = "ml",
						ProductAttributeGroupKey = "dimensions"
					},
                    new ProductAttribute
                    {
                        Name = new Localized<string>("Shape"),
                        ProductAttributeType = 3, // text (round,square)
                        ProductAttributeGroupKey = "dimensions"
                    },					
					new ProductAttribute
					{
						Name = new Localized<string>("Net Weight"),
						ProductAttributeType = 2, // double
						Units = "kg",
						ProductAttributeGroupKey = "weight"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Gross Weight"),
						ProductAttributeType = 2, // double
						Units = "kg",
						ProductAttributeGroupKey = "weight"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Material"),
						ProductAttributeType = 3, // text (cotton, plastic, aluminium, ..)
						Multiple = true,
						ProductAttributeGroupKey = "material_build"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Finish"),
						ProductAttributeType = 3, // text (glossy, matte, ..)
						ProductAttributeGroupKey = "material_build"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Colour"),
						ProductAttributeType = 3, // text (white, red, black, ..)
						ProductAttributeGroupKey = "material_build"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Texture"),
						ProductAttributeType = 3, // text (rough, smooth, ..)
						ProductAttributeGroupKey = "material_build"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Power Consumption"),
						ProductAttributeType = 2, // double
						Units = "W",
						ProductAttributeGroupKey = "power_energy"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Voltage"),
						ProductAttributeType = 2, // double
						Units = "V",
						ProductAttributeGroupKey = "power_energy"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Current"),
						ProductAttributeType = 2, // double
						Units = "A",
						ProductAttributeGroupKey = "power_energy"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Battery Type"),
						ProductAttributeType = 3, // text (Lithium ion, ..)
						ProductAttributeGroupKey = "power_energy"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Battery Capacity"),
						ProductAttributeType = 2, // double
						Units = "Ah",
						ProductAttributeGroupKey = "power_energy"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Energy Efficiency Rating"),
						ProductAttributeType = 3, // text (A, B, C, ..)
						ProductAttributeGroupKey = "power_energy"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Processing Speed"),
						ProductAttributeType = 2, // double
						Units = "GHz",
						ProductAttributeGroupKey = "computing"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Storage Capacity"),
						ProductAttributeType = 2, // double
						Units = "GB",
						ProductAttributeGroupKey = "computing"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Acidity"),
						ProductAttributeType = 2, // double
						Units = "pH",
						ProductAttributeGroupKey = "chemical"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Rotor Speed"),
						ProductAttributeType = 2, // double
						Units = "RPM",
						RangeType = 2,
						ProductAttributeGroupKey = "mechanical"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Cylinder count"),
						ProductAttributeType = 1, // long
						ProductAttributeGroupKey = "mechanical"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Weight capacity"),
						ProductAttributeType = 2, // double
						Units = "kg",
						RangeType = 2,
						ProductAttributeGroupKey = "load_ranges"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Usable Outdoors"),
						ProductAttributeType = 7, // bool
						ProductAttributeGroupKey = "application"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Intended Use"),
						ProductAttributeType = 3, // string (bedroom, kitchen, ..)
						Multiple = true,
						ProductAttributeGroupKey = "application"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Target User"),
						ProductAttributeType = 3, // string (adults, pets, children, ..)
						Multiple = true,
						ProductAttributeGroupKey = "application"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Supported Platforms"),
						ProductAttributeType = 3, // string (PC, Nintendo Switch, Xbox One, ..)
						Multiple = true,
						ProductAttributeGroupKey = "compatibility"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Connection Types"),
						ProductAttributeType = 3, // string (USB-C, Bluetooth, ..)
						Multiple = true,
						ProductAttributeGroupKey = "compatibility"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Style"),
						ProductAttributeType = 3, // string (Modern, Rustic, ..)
						ProductAttributeGroupKey = "design"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Pattern"),
						ProductAttributeType = 3, // string (Checkered, Dotted, ..)
						ProductAttributeGroupKey = "design"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Brand Name"),
						ProductAttributeType = 3, // string (Apple, Nvidia, ..)
						ProductAttributeGroupKey = "branding"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Logo Presence"),
						ProductAttributeType = 7, // boolean (yes, no)
						ProductAttributeGroupKey = "branding"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Customizable"),
						ProductAttributeType = 7, // boolean (yes, no)
						ProductAttributeGroupKey = "branding"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Package Width"),
						ProductAttributeType = 1, // long
						Units = "mm",
						ProductAttributeGroupKey = "packaging"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Package Height"),
						ProductAttributeType = 2, // double
						Units = "mm",
						ProductAttributeGroupKey = "packaging"
					},
					new ProductAttribute
					{
						Name = new Localized<string>("Package Length"),
						ProductAttributeType = 2, // double
						Units = "mm",
						ProductAttributeGroupKey = "packaging"
					},

					new ProductAttribute
					{
						Name = new Localized<string>("Max Stack Height"),
						ProductAttributeType = 2, // double
						Units = "m",
						ProductAttributeGroupKey = "shipping"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Max Units Stacked"),
						ProductAttributeType = 1, // long
						ProductAttributeGroupKey = "shipping"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Stack Load Limit"),
						ProductAttributeType = 2, // double
						Units = "kg",
						ProductAttributeGroupKey = "shipping"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Pallet Quantity"),
						ProductAttributeType = 1, // long
						ProductAttributeGroupKey = "shipping"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Fragility"),
						ProductAttributeType = 7, // bool
						ProductAttributeGroupKey = "shipping"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Explosive"),
						ProductAttributeType = 7, // bool
						ProductAttributeGroupKey = "shipping"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Certifications"),
						ProductAttributeType = 3, // text (CE, FCC, ..)
						ProductAttributeGroupKey = "compliance"
					},
					
					new ProductAttribute
					{
						Name = new Localized<string>("Country of Origin"),
						ProductAttributeType = 3, // text (United Kingdom, China, ..)
						ProductAttributeGroupKey = "compliance"
					},
					
					new ProductAttribute
					{
						// Just in case some warranties are in days or years - they're just different attributes.
						Key = "warranty_period_months",
						Name = new Localized<string>("Warranty Period"),
						ProductAttributeType = 1, // long
						Units = "months",
						ProductAttributeGroupKey = "warranty"
					}
				);

				Events.ProductAttribute.AfterCreate.AddEventListener(async (Context context, ProductAttribute attrib) => {
					if (attrib == null)
					{
						return attrib;
					}

					// If it is a boolean (type 7) attribute, add the yes/ no values.
					if (attrib.ProductAttributeType == 7)
					{
						await _attributeValues.Create(context, new ProductAttributeValue() {
							Value = "Yes",
							ProductAttributeId = attrib.Id
						}, DataOptions.IgnorePermissions);
						
						await _attributeValues.Create(context, new ProductAttributeValue() {
							Value = "No",
							ProductAttributeId = attrib.Id
						}, DataOptions.IgnorePermissions);
					}

					return attrib;
				});

				return svc;
			});

			Cache();
		}

		private ValueTask<ProductAttribute> ClearCache(Context ctx, ProductAttribute attrib)
		{
			_attributeTree = null;
			return ValueTask.FromResult(attrib);
		}

		private ValueTask<ProductAttributeGroup> ClearCache(Context ctx, ProductAttributeGroup group)
		{
			_attributeTree = null;
			return ValueTask.FromResult(group);
		}

		private ValueTask<Role> OnRoleMutate(Context ctx, Role role)
		{
			var capability = Capabilities.GetAllCurrent().FirstOrDefault(cap => cap.Name == "productattribute_update");

			if (capability is null)
			{
				throw new PublicException("Cannot find correct capability", "product-attribute/capability");
			}

			#warning MUST revisit this, it drops viewing user context
			_canEditAttribute[role] = role.IsGranted(capability, new Context(1, 1, role.Id), new (), ContextFlags.None);
			
			return ValueTask.FromResult(role);
		}

		/// <summary>
		/// Adds a validation layer to the <c>ProductAttribute</c> entity,
		/// makes sure the name and the key aren't empty.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="attrib"></param>
		/// <param name="original"></param>
		/// <returns></returns>
		/// <exception cref="PublicException"></exception>
		private async ValueTask<ProductAttribute> ValidateAttribute(Context context, ProductAttribute attrib,
			ProductAttribute original = null)
		{

			if (string.IsNullOrEmpty(attrib.Name.Get(context)))
			{
				throw new PublicException("The attribute name cannot be empty.", "attribute-validation/no-name");
			}

			if (string.IsNullOrEmpty(attrib.Key))
			{
				attrib.Key = ToAttributeKey(attrib.Name.GetFallback());
				
				// try and match a single record. 
				var query = Where("Key = ?").Bind(attrib.Key);
				query.PageSize = 1;
				query.SortAscending = false;
				
				// if one is found, append the ID + 1 of the last one found.
				var result = await query.ListAll(context);
				if (result.Count != 0)
				{
					attrib.Key += "_" + (result.First().Id + 1);
				}
			}

			if (attrib.ProductAttributeGroupId == 0)
			{
				throw new PublicException("The attribute group cannot be empty.", "attribute-validation/no-group");
			}
			
			return attrib;
		}

		/// <summary>
		/// Get the entire attribute group tree (hopefully from cache)
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		public async Task<AttributeGroupTree> GetTree(Context ctx)
		{
			var tree = _attributeTree;

			if (tree != null)
			{
				return tree.Value;
			}

			// Build a new one:
			var newTree = await BuildAttributeGroupTree(ctx, true);

			// Cache it:
			_attributeTree = newTree;

			return newTree;
		}
		
		/// <summary>
		/// Build the flat database list into a nested group tree structure.
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="includeAttributes"></param>
		/// <returns></returns>
		private async Task<AttributeGroupTree> BuildAttributeGroupTree(Context ctx, bool includeAttributes = false)
		{
			Log.Info("productcategory", "Building attribute group structure");

			await LoadRolesPermissions(ctx);

			// get all groups 
			var groups = await _groups.Where("", DataOptions.IgnorePermissions).ListAll(ctx);

			// create lookup dictionaries
			var lookup = new Dictionary<uint, ProductAttributeGroupNode>();
			var lookupBySlug = new Dictionary<string, ProductAttributeGroupNode>();

			foreach (var group in groups)
			{
				var node = new ProductAttributeGroupNode()
				{
					Group = group
				};

				lookup[group.Id] = node;
				lookupBySlug[group.Key] = node;
			}

			List<ProductAttributeGroupNode> roots = new();

			foreach (var cat in groups)
			{
				var node = lookup[cat.Id];

				if (cat.ParentGroupId != 0 && lookup.TryGetValue(cat.ParentGroupId, out var parent))
				{
					node.Parent = parent;
					parent.Children.Add(node);
				}
				else
				{
					roots.Add(node);
				}
			}

			if (includeAttributes)
			{
				var attribs = await Where("", DataOptions.IgnorePermissions).ListAll(ctx);

				// Attach attribs to groups
				foreach (var attrib in attribs)
				{
					if (attrib.ProductAttributeGroupId != 0 && lookup.TryGetValue(attrib.ProductAttributeGroupId, out var attributeGroupNode))
					{
						var nodeProduct = new ProductAttributeNode
						{
							Attribute = attrib
						};

						attributeGroupNode.Attributes.Add(nodeProduct);
					}
				}
			}

            Log.Info("productcategory", "Finished building attribute group structure");

            return new AttributeGroupTree()
			{
				IdLookup = lookup,
				KeyLookup = lookupBySlug,
				Roots = roots
			};
		}

		private async ValueTask LoadRolesPermissions(Context ctx)
		{
			var roles = await _roles.Where("CanViewAdmin = ?", DataOptions.IgnorePermissions).Bind(true).ListAll(ctx);
			var capability = Capabilities.GetAllCurrent().FirstOrDefault(cap => cap.Name == "productattribute_update");

			if (capability is null)
			{
				throw new PublicException("Cannot find correct capability", "product-attribute/capability");
			}
			
			foreach (var role in roles)
			{
				#warning MUST revisit this, it drops viewing user context
				_canEditAttribute[role] = role.IsGranted(capability, new Context(1, 1, role.Id), new (), ContextFlags.None);
			}
		}

		/// <summary>
		/// Gets a tree node for the admin panel at a given category slug path.
		/// As category slugs are globally unique, only actually the last one is used 
		/// (unless it is blank, in which case root categories are returned).
		/// </summary>
		/// <param name="context"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public async ValueTask<TreeNodeDetail?> GetTreeNodeAtPath(Context context, string path)
		{
			var tree = await GetTree(context);

			if (string.IsNullOrEmpty(path))
			{
				// Root of the tree.
				var roots = tree.Roots;

				var rootSet = new List<RouterNodeMetadata>();

				if (roots != null)
				{
					foreach (var root in roots)
					{
						rootSet.Add(ConvertNode(context, root, root.Group.Key));
					}
				}

				return new TreeNodeDetail()
				{
					Self = null,
					Children = rootSet
				};
			}

			if (path.EndsWith('/'))
			{
				path = path.Substring(0, path.Length - 1);
			}

			var lastSlash = path.LastIndexOf('/');

			string slug;

			if (lastSlash == -1)
			{
				slug = path;
			}
			else
			{
				slug = path.Substring(lastSlash + 1);
			}

			var lookup = tree.KeyLookup;

			if (!lookup.TryGetValue(slug, out ProductAttributeGroupNode node))
			{
				// Not found.
				return null;
			}

			var kids = node.Children;

			var childSet = new List<RouterNodeMetadata>();

			if (kids != null)
			{
				foreach (var child in kids)
				{
					childSet.Add(ConvertNode(context, child, path + "/" + child.Group.Key));
				}
			}

			if (node.Attributes != null)
			{
				foreach (var attr in node.Attributes)
				{
					childSet.Add(ConvertNode(context, attr));
				}
			}

			return new TreeNodeDetail()
			{
				Self = ConvertNode(context, node, path),
				Children = childSet
			};
		}

		/// <summary>
		/// Builds an admin tree view compatible struct of metadata for the given attrib group node.
		/// </summary>
		/// <returns></returns>
		private RouterNodeMetadata ConvertNode(Context context, ProductAttributeGroupNode node, string path)
		{
			return new RouterNodeMetadata()
			{
				Type = "ProductAttributeGroup",
				EditUrl = "/en-admin/productattributegroup/" + node.Group.Id,
				ContentId = node.Group.Id,
				Name = node.Group.Name.Get(context),
				ChildKey = node.Group.Key,
				FullRoute = path,
				HasChildren = (node.Children != null && node.Children.Count > 0) || (node.Attributes != null && node.Attributes.Count > 0)
			};
		}
		
		/// <summary>
		/// Builds an admin tree view compatible struct of metadata for the given attrib node.
		/// </summary>
		/// <returns></returns>
		private RouterNodeMetadata ConvertNode(Context context, ProductAttributeNode node)
		{
			return new RouterNodeMetadata()
			{
				Type = "ProductAttribute",
				EditUrl = GetEditUrl(context, node) ,
				ContentId = node.Attribute.Id,
				Name = node.Attribute.Name.Get(context),
				ChildKey = node.Attribute.Key,
				HasChildren = false // It may have values, but they don't appear as children in the tree.
			};
		}

		private string GetEditUrl(Context ctx, ProductAttributeNode node)
		{
			var canEdit = false;
			_canEditAttribute.TryGetValue(ctx.Role, out canEdit);

			return "/en-admin/productattribute/" + node.Attribute.Id + (!canEdit ? "/values" : string.Empty);
		}

		/// <summary>
		/// Generates an attribute key from the given name.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public string ToAttributeKey(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return string.Empty;

			// Convert to lowercase
			string result = input.ToLowerInvariant();

			// Remove all non-alphanumeric characters except spaces
			result = Regex.Replace(result, @"[^a-z0-9\s]", "");

			// Replace one or more spaces with a single underscore
			result = Regex.Replace(result, @"\s+", "_");

			return result;
		}

		/// <summary>
		/// Install default attributes.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="groups"></param>
		/// <param name="attribs"></param>
		/// <returns></returns>
		public async ValueTask InstallDefaults(
			Context context,
			Dictionary<string, ProductAttributeGroup> groups,
			params ProductAttribute[] attribs)
		{
			var all = await Where("", DataOptions.IgnorePermissions).ListAll(context);

			// Build a key dict:
			var lookup = new Dictionary<string, ProductAttribute>();

			foreach (var entry in all)
			{
				if (string.IsNullOrEmpty(entry.Key))
				{
					continue;
				}

				lookup[entry.Key] = entry;
			}

			foreach (var attrib in attribs)
			{
				if (string.IsNullOrEmpty(attrib.Key))
				{
					attrib.Key = ToAttributeKey(attrib.Name.GetFallback());
				}

				if (lookup.ContainsKey(attrib.Key))
				{
					// It already exists.
					continue;
				}

				// Create it but only if the group can be located.
				if (
					string.IsNullOrEmpty(attrib.ProductAttributeGroupKey) || 
					!groups.TryGetValue(attrib.ProductAttributeGroupKey, out ProductAttributeGroup parent))
				{
					throw new System.Exception("Unable to install attribute '" + attrib.Name + "' as it has no identifiable parent group. " +
						"Check that the ProductAttributeGroupKey is set and exists.");
				}

				attrib.ProductAttributeGroupId = parent.Id;
				lookup[attrib.Key] = await Create(context, attrib, DataOptions.IgnorePermissions);
			}

		}

		/// <summary>
		/// Install default attribute groups.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="groups"></param>
		/// <returns></returns>
		public async ValueTask<Dictionary<string, ProductAttributeGroup>> InstallDefaultGroups(Context context, params ProductAttributeGroup[] groups)
		{
			// Get all current cats (fast - from the cache):
			var all = await _groups.Where("", DataOptions.IgnorePermissions).ListAll(context);

			// Build a key dict:
			var lookup = new Dictionary<string, ProductAttributeGroup>();

			foreach (var entry in all)
			{
				if (string.IsNullOrEmpty(entry.Key))
				{
					continue;
				}

				lookup[entry.Key] = entry;
			}

			foreach (var group in groups)
			{
				if (string.IsNullOrEmpty(group.Key))
				{
					group.Key = ToAttributeKey(group.Name.GetFallback());
				}

				if (lookup.ContainsKey(group.Key))
				{
					// It already exists.
					continue;
				}

				// Creating it now. Get the parent first if it has one:
				if (!string.IsNullOrEmpty(group.ParentGroupKey))
				{
					if (!lookup.TryGetValue(group.ParentGroupKey, out ProductAttributeGroup parent))
					{
						throw new System.Exception("Unable to install attribute groups: Please put parent groups in the array first. " +
							"'" + group.Key + "' group tried to use parent '" + group.ParentGroupKey +  "' but it does not exist at this point.");
					}

					group.ParentGroupId = parent.Id;
				}

				lookup[group.Key] = await _groups.Create(context, group, DataOptions.IgnorePermissions);
			}

			return lookup;
		}
	}

	/// <summary>
	/// The attribute tree.
	/// </summary>
	public struct AttributeGroupTree
	{
		/// <summary>
		/// Roots of the tree.
		/// </summary>
		public List<ProductAttributeGroupNode> Roots;

		/// <summary>
		/// A lookup to a particular node in the tree.
		/// </summary>
		public Dictionary<uint, ProductAttributeGroupNode> IdLookup;

		/// <summary>
		/// A lookup to a particular node in the tree by unique key.
		/// </summary>
		public Dictionary<string, ProductAttributeGroupNode> KeyLookup;
	}

}
