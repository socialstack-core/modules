﻿using Api.Startup;
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
				
				// Allow viewing of owned things:
				Roles.Guest.If("IsSelf()").ThenGrant(
					"productquantity_load", "productquantity_list",
					"purchase_load", "purchase_list",
					"shoppingcart_load", "shoppingcart_list",
					"subscription_load", "subscription_list",
					"paymentmethod_load", "paymentmethod_list"
				);
				
				Roles.Member.If("IsSelf()").ThenGrant(
					"productquantity_load", "productquantity_list",
					"purchase_load", "purchase_list",
					"shoppingcart_load", "shoppingcart_list",
					"subscription_load", "subscription_list",
					"paymentmethod_load", "paymentmethod_list"
				);
				
				return new ValueTask<object>(source);
			}, 20);
		}
	}
}