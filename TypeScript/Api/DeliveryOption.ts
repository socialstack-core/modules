/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { DeliveryOptionIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {DeliveryOption} (Api.Payments.DeliveryOption)
**/
export type DeliveryOption = VersionedContent<uint> & {
    informationJson?: string
    addressId: uint
    shoppingCartId: uint
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {CartEstimation} (Api.Payments.CartEstimation)
**/
export type CartEstimation = {
    deliveryAddressKey?: string
}
// ENTITY CONTROLLER

export class DeliveryOptionApi extends AutoController<DeliveryOption,uint, DeliveryOptionIncludes>{

    constructor(){
        super('/v1/deliveryoption', new DeliveryOptionIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {DeliveryOptionController::Estimate}
     * @url /estimate/cart/{shoppingcartid}/by-key/{anonkey}
     * @debug - method.ReturnType Api.Startup.ContentStream`2[[Api.Payments.DeliveryOption, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.ContentStream`2[[Api.Payments.DeliveryOption, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     estimate = (shoppingCartId: uint, anonKey?: string, options: CartEstimation, includes?: ApiIncludes[]): Promise<ApiList<DeliveryOption>> => {
        return getList<DeliveryOption>(this.apiUrl + '/estimate/cart/' + shoppingCartId +'/by-key/' + anonKey  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', options, { method: 'POST' } )
     }


}

export default new DeliveryOptionApi();
