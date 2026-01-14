using Api.Contexts;
using Api.Eventing;
using Api.Addresses;
using Api.Pages;
using Api.Startup;
using System.Net.Mail;
using System;
using Api.Permissions;
using Api.Payments;
using Api.CanvasRenderer;
using System.Threading.Tasks;
using Api.Emails;
using Newtonsoft.Json;

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

			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
			{
				if (builder.ContentType == typeof(GuestUser) && builder.PageType == CommonPageType.AdminList)
				{
					builder.GetContentRoot()
						.Empty()
						.AppendChild(new CanvasNode("UI/Guest/Details"));
				}

				return new ValueTask<PageBuilder>(builder);
			});

			Events.GuestUser.BeforeCreate.AddEventListener((Context ctx, GuestUser guestUser) =>
			{
				if (!_config.IsEnabled)
				{
					throw new PublicException("Guest users are unavailable on this site.", "guest/disabled");
				}

				return new ValueTask<GuestUser>(guestUser);
			}, 15);

		}
	}
}
