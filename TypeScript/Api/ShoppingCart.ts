/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { ShoppingCartIncludes } from 'Api/Includes';

import { Purchase } from 'Api/Purchase';

import { Address } from 'Api/Address';

import { Coupon } from 'Api/Coupon';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {ShoppingCart} (Api.Payments.ShoppingCart)
**/
export type ShoppingCart = VersionedContent<uint> & {
    checkedOut: boolean
    anonymousCartKey?: string
    couponId: uint
    reference?: string
    anonymousAddressKey?: string
    guestUserId?: (uint | undefined)
    guestDetailsJson: string
    // HasVirtualField() fields (2 in total)
    coupon?: Coupon;
    creatorUser?: User;
}

/**
* This type was generated to reflect {CartCoupon} (Api.Payments.CartCoupon)
**/
export type CartCoupon = {
    code?: string
    anonymousCartKey?: string
    shoppingCartId: uint
}

/**
* This type was generated to reflect {RemoveCoupon} (Api.Payments.RemoveCoupon)
**/
export type RemoveCoupon = {
    anonymousCartKey?: string
    shoppingCartId: uint
}

/**
* This type was generated to reflect {CartItemChange} (Api.Payments.CartItemChange)
**/
export type CartItemChange = {
    productId: uint
    deltaQuantity?: (int | undefined)
    quantity?: (uint | undefined)
}

/**
* This type was generated to reflect {CartItemChanges} (Api.Payments.CartItemChanges)
**/
export type CartItemChanges = {
    shoppingCartId: uint
    anonymousCartKey?: string
    items?: CartItemChange[]
}

/**
* This type was generated to reflect {ChallengeMetaData} (Api.Payments.ChallengeMetaData)
**/
export type ChallengeMetaData = {
    challengeRequest?: string
    challengeUrl?: string
    sessionToken?: string
    token?: string
}

/**
* This type was generated to reflect {PurchaseAndAction} (Api.Payments.PurchaseAndAction)
**/
export type PurchaseAndAction = {
    purchase?: Purchase
    action?: string
    metaData: ChallengeMetaData
}

/**
* This type was generated to reflect {CheckoutInfo} (Api.Payments.CheckoutInfo)
**/
export type CheckoutInfo = {
    guestUserId?: (uint | undefined)
    shoppingCartId: uint
    anonymousCartKey?: string
    deliveryAddressKey?: string
    billingAddressKey?: string
    deliveryOptionId: uint
    ipAddress?: string
    reference?: string
    paymentMethod?: JToken
}

/**
* This type was generated to reflect {GuestDetails} (Api.GuestUsers.GuestDetails)
**/
export type GuestDetails = {
    email?: string
    firstName?: string
    lastName?: string
    addresses?: Address[]
}
// ENTITY CONTROLLER

export class ShoppingCartApi extends AutoController<ShoppingCart,uint, ShoppingCartIncludes>{

    constructor(){
        super('/v1/shoppingcart', new ShoppingCartIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {ShoppingCartController::ApplyCoupon}
     * @url /apply_coupon
     * @debug - method.ReturnType Api.Payments.ShoppingCart
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.ShoppingCart, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     applyCoupon = (couponInfo: CartCoupon, includes?: ApiIncludes[]): Promise<ShoppingCart> => {
        return getOne<ShoppingCart>(this.apiUrl + '/apply_coupon' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', couponInfo, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {ShoppingCartController::RemoveCoupon}
     * @url /remove_coupon
     * @debug - method.ReturnType Api.Payments.ShoppingCart
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.ShoppingCart, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     removeCoupon = (couponInfo: RemoveCoupon, includes?: ApiIncludes[]): Promise<ShoppingCart> => {
        return getOne<ShoppingCart>(this.apiUrl + '/remove_coupon' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', couponInfo, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {ShoppingCartController::LoadAnon}
     * @url /by-key/{cartid}/{anonkey}
     * @debug - method.ReturnType Api.Payments.ShoppingCart
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.ShoppingCart, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     loadAnon = (cartId: uint, anonKey?: string, includes?: ApiIncludes[]): Promise<ShoppingCart> => {
        return getOne<ShoppingCart>(this.apiUrl + '/by-key/' + cartId +'/' + anonKey  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {ShoppingCartController::ChangeItems}
     * @url /change_items
     * @debug - method.ReturnType Api.Payments.ShoppingCart
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.ShoppingCart, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     changeItems = (itemChanges: CartItemChanges, includes?: ApiIncludes[]): Promise<ShoppingCart> => {
        return getOne<ShoppingCart>(this.apiUrl + '/change_items' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', itemChanges, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {ShoppingCartController::Checkout}
     * @url /checkout
     * @debug - method.ReturnType Api.Payments.PurchaseAndAction
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Payments.PurchaseAndAction, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     checkout = (checkout: CheckoutInfo): Promise<PurchaseAndAction> => {
        return getJson<PurchaseAndAction>(this.apiUrl + '/checkout', checkout, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {ShoppingCartController::UpdateGuest}
     * @url /updateguest/{cartid}/{anonkey}
     * @debug - method.ReturnType Api.Payments.ShoppingCart
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.ShoppingCart, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     updateGuest = (cartId: uint, anonKey?: string, guestDetails?: GuestDetails, includes?: ApiIncludes[]): Promise<ShoppingCart> => {
        return getOne<ShoppingCart>(this.apiUrl + '/updateguest/' + cartId +'/' + anonKey  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', guestDetails, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {ShoppingCartController::CheckoutGuestCart}
     * @url /checkout-guest-cart
     * @debug - method.ReturnType Api.Payments.PurchaseAndAction
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Payments.PurchaseAndAction, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     checkoutGuestCart = (checkoutInfo: CheckoutInfo): Promise<PurchaseAndAction> => {
        return getJson<PurchaseAndAction>(this.apiUrl + '/checkout-guest-cart', checkoutInfo, { method: 'POST' } )
     }


}

export default new ShoppingCartApi();
