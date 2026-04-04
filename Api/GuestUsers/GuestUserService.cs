using Api.Addresses;
using Api.CanvasRenderer;
using Api.Contexts;
using Api.Emails;
using Api.Eventing;
using Api.Pages;
using Api.Payments;
using Api.Permissions;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Api.GuestUsers
{
	/// <summary>
	/// Handles guestUsers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class GuestUserService : AutoService<GuestUser>
	{
		private AddressService _addressService;
		private PurchaseTokenService _purchaseTokens;
		private GuestUsersConfig _config;
		private EmailTemplateService _emails;
		private DeliveryOptionService _deliveryOptions;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public GuestUserService(PageService pages, AddressService addressService, PurchaseTokenService purchaseTokenService, EmailTemplateService emailTemplateService, DeliveryOptionService deliveryOptions) : base(Events.GuestUser)
		{
			_addressService = addressService;
			_purchaseTokens = purchaseTokenService;
			_emails = emailTemplateService;
			_deliveryOptions = deliveryOptions;

			_config = GetConfig<GuestUsersConfig>();

			// anon users can create purchases but the only 'access' would be via the reference value
			if (_config.IsEnabled)
			{
				Roles.Public.Grant("purchase_execute", "purchase_create", "productquantity_create");
			}

			InstallAdminPages("Guest Orders", "fa:fa-shopping-basket", ["id", "name", "minQuantity"], null, "ecommerce");

			pages.Install(
				new PageBuilder()
				{
					Url = "guest/register",
					Key = "guest_register",
					Title = "Guest Details",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasRenderer.CanvasNode("UI/Guest/Registration")
							.With("guest", true)
						);
					}
				},
				new PageBuilder()
				{
					Url = "guest/checkout",
					Key = "guest_checkout",
					Title = "Guest Checkout",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasRenderer.CanvasNode("UI/Payments/Checkout")
							.With("guest", true)
						);
					}
				},
				new PageBuilder()
				{
					Url = "/en-admin/guest/purchase/${purchase.id}",
					Key = "admin_guest_purchase_view",
					Title = "Guest purchase",
					PrimaryContentType = "Purchase",
					PrimaryContentIncludes = "creatorUser,billingAddress,deliveryAddress,productQuantities.orderStatus,productQuantities.product.businessProductConfig,requestedProductQuantities.product.businessProductConfig,productQuantities.product.productDownloads, productQuantities.product.coshhDocuments",
					BuildBody = (PageBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasRenderer.CanvasNode("UI/Business/Orders/View").WithPrimaryLink("purchase")
						);
					}
				}
			);

			HashSet<string> hiddenFields = new HashSet<string>() { "FeatureRef", "Feature", "Categories", "Tags" };

			Events.GuestUser.BeforeSettable.AddEventListener((Context ctx, JsonField<GuestUser, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<GuestUser, uint>>(field);
				}

				if (hiddenFields.Contains(field.Name))
				{
					field.Writeable = false;
					field.Hide = true;
				}

				return new ValueTask<JsonField<GuestUser, uint>>(field);
			});

			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
			{
				if (builder.ContentType == typeof(GuestUser) && builder.PageType == CommonPageType.AdminList)
				{
					builder.GetContentRoot()
						.Empty()
						.AppendChild(new CanvasNode("Admin/Guest/Details"));
				}

				return new ValueTask<PageBuilder>(builder);
			});


			Events.GuestUser.BeforeCreate.AddEventListener(async (Context ctx, GuestUser guestUser) =>
			{
				if (!_config.IsEnabled)
				{
					throw new PublicException("The guest system is not active on this website.", "guest/disabled");
				}

				return guestUser;
			});

			Events.GuestUser.BeforeCreate.AddEventListener(async (Context ctx, GuestUser guestUser) =>
			{
				// guest has already been processed/ignored 
				if (guestUser == null)
				{
					return guestUser;
				}

				MailAddress email;
				try
				{
					email = new MailAddress(guestUser.Email);
				}
				catch (Exception)
				{
					throw new PublicException("The email domain cannot be verified. Please contact us for assistance", "email_format");

				}

				return guestUser;
			}, 50);
		}
	}
}
