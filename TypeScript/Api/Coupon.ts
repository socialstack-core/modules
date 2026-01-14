/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { CouponIncludes } from 'Api/Includes';

import { Price } from 'Api/Price';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Coupon} (Api.Payments.Coupon)
**/
export type Coupon = VersionedContent<uint> & {
    token?: string
    description?: string
    maxNumberOfPeople: uint
    disabled: boolean
    expiryDateUtc?: (Date | string | number | undefined)
    subscriptionDelayDays: uint
    discountPercent: uint
    discountFixedAmount: uint
    freeDelivery: boolean
    minimumSpendAmount: uint
    // HasVirtualField() fields (3 in total)
    discountAmount?: Price;
    minSpendPrice?: Price;
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class CouponApi extends AutoController<Coupon,uint, CouponIncludes>{

    constructor(){
        super('/v1/coupon', new CouponIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {CouponController::CheckCoupon}
     * @url /check/{couponcode}
     * @debug - method.ReturnType Api.Payments.Coupon
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.Coupon, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     checkCoupon = (couponCode?: string, includes?: ApiIncludes[]): Promise<Coupon> => {
        return getOne<Coupon>(this.apiUrl + '/check/' + couponCode  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


}

export default new CouponApi();
