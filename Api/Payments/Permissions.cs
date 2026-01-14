using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Payments
{
	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class Permissions
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Permissions()
		{
			// Hook the default role setup. It's done like this so it can be removed by a plugin if wanted.
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) =>
			{
				// Remove public viewing (as it's enabled by default).
				// We also disable update/ create to avoid people using the API to edit internal fields
				// (most of these things are internal).
				Roles.Guest.Revoke("productquantity_load", "productquantity_list", "productquantity_update", "productquantity_create");
				Roles.Public.Revoke("productquantity_load", "productquantity_list", "productquantity_update", "productquantity_create");
				Roles.Member.Revoke("productquantity_load", "productquantity_list", "productquantity_update", "productquantity_create");
				
				Roles.Guest.Revoke("purchase_load", "purchase_list", "purchase_update", "purchase_create");
				Roles.Public.Revoke("purchase_load", "purchase_list", "purchase_update", "purchase_create");
				Roles.Member.Revoke("purchase_load", "purchase_list", "purchase_update", "purchase_create");
				
				Roles.Guest.Revoke("shoppingcart_load", "shoppingcart_list", "shoppingcart_update", "shoppingcart_create");
				Roles.Public.Revoke("shoppingcart_load", "shoppingcart_list", "shoppingcart_update", "shoppingcart_create");
				Roles.Member.Revoke("shoppingcart_load", "shoppingcart_list", "shoppingcart_update", "shoppingcart_create");
				
				Roles.Guest.Revoke("subscription_load", "subscription_list", "subscription_update", "subscription_create");
				Roles.Public.Revoke("subscription_load", "subscription_list", "subscription_update", "subscription_create");
				Roles.Member.Revoke("subscription_load", "subscription_list", "subscription_update", "subscription_create");
				
				Roles.Guest.Revoke("paymentmethod_load", "paymentmethod_list", "paymentmethod_update", "paymentmethod_create");
				Roles.Public.Revoke("paymentmethod_load", "paymentmethod_list", "paymentmethod_update", "paymentmethod_create");
				Roles.Member.Revoke("paymentmethod_load", "paymentmethod_list", "paymentmethod_update", "paymentmethod_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("productTemplate_load", "productTemplate_list");
				Roles.Public.Revoke("productTemplate_load", "productTemplate_list");
				Roles.Member.Revoke("productTemplate_load", "productTemplate_list");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("coupon_load", "coupon_list");
				Roles.Public.Revoke("coupon_load", "coupon_list");
				Roles.Member.Revoke("coupon_load", "coupon_list");

				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("purchaseToken_create");
				Roles.Public.Grant("purchaseToken_create");
				Roles.Guest.Grant("purchaseToken_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("purchaseToken_load", "purchaseToken_list");
				Roles.Public.Revoke("purchaseToken_load", "purchaseToken_list");
				Roles.Member.Revoke("purchaseToken_load", "purchaseToken_list");
				
				// Allow viewing of owned things:
				Roles.Guest.If("IsSelf()").ThenGrant(
					"productquantity_load", "productquantity_list",
					"purchase_load", "purchase_list",
					"purchase_execute",
					"shoppingcart_load", "shoppingcart_list",
					"subscription_load", "subscription_list",
					"paymentmethod_load", "paymentmethod_list"
				);

				// Allow anon/guest checkout to see any included content on a purchase retrieved by tokens
				Roles.Public.If("IsIncluded()").ThenGrant(
					"productquantity_load", "productquantity_list"
				);

				Roles.Member.If("IsSelf()").ThenGrant(
					"productquantity_load", "productquantity_list",
					"purchase_load", "purchase_list",
					"purchase_execute",
					"shoppingcart_load", "shoppingcart_list",
					"subscription_load", "subscription_list",
					"paymentmethod_load", "paymentmethod_list"
				);

				// We'll also be restricting the developer role on payment methods.
				Roles.Admin.Revoke("paymentmethod_load", "paymentmethod_list");
				Roles.Developer.Revoke("paymentmethod_load", "paymentmethod_list");

				Roles.Admin.If("IsSelf()").ThenGrant("paymentmethod_load", "paymentmethod_list");
				Roles.Developer.If("IsSelf()").ThenGrant("paymentmethod_load", "paymentmethod_list");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("address_load", "address_list");
				Roles.Public.Revoke("address_load", "address_list");
				Roles.Member.Revoke("address_load", "address_list");
				Roles.Member.If("IsSelf()").ThenGrant("address_load", "address_list");

				Roles.Guest.Revoke("deliveryoption_load", "deliveryoption_list", "delivery_load", "delivery_list", "subscriptionusage_load", "subscriptionusage_list");
				Roles.Public.Revoke("deliveryoption_load", "deliveryoption_list", "delivery_load", "delivery_list", "subscriptionusage_load", "subscriptionusage_list");
				Roles.Member.Revoke("deliveryoption_load", "deliveryoption_list", "delivery_load", "delivery_list", "subscriptionusage_load", "subscriptionusage_list");

				return new ValueTask<object>(source);
			}, 20);
		}
	}
}