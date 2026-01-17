/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { AddressIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Address} (Api.Addresses.Address)
**/
export type Address = VersionedContent<uint> & {
    name?: string
    addressType: uint
    anonKey?: string
    uprn?: (ulong | undefined)
    latitude?: (double | undefined)
    longitude?: (double | undefined)
    line1?: string
    line2?: string
    line3?: string
    city?: string
    county?: string
    postcode?: string
    countryCode?: string
    featureRef?: string
    contact?: string
    telNo?: string
    isDefaultDeliveryAddress: boolean
    isDefaultBillingAddress: boolean
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class AddressApi extends AutoController<Address,uint, AddressIncludes>{

    constructor(){
        super('/v1/address', new AddressIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {AddressController::GetCartAddresses}
     * @url /cart_addresses
     * @debug - method.ReturnType Api.Startup.ContentStream`2[[Api.Addresses.Address, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.ContentStream`2[[Api.Addresses.Address, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getCartAddresses = (includes?: ApiIncludes[]): Promise<ApiList<Address>> => {
        return getList<Address>(this.apiUrl + '/cart_addresses' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


}

export default new AddressApi();
