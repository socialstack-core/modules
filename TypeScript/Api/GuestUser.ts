/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { GuestUserIncludes } from 'Api/Includes';

import { Address } from 'Api/Address';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {GuestUser} (Api.GuestUsers.GuestUser)
**/
export type GuestUser = VersionedContent<uint> & {
    email?: string
    firstName?: string
    lastName?: string
    deliveryAddressId: uint
    billingAddressId?: (uint | undefined)
    // HasVirtualField() fields (3 in total)
    deliveryAddress?: Address;
    billingAddress?: Address;
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class GuestUserApi extends AutoController<GuestUser,uint, GuestUserIncludes>{

    constructor(){
        super('/v1/guestuser', new GuestUserIncludes());
    }

}

export default new GuestUserApi();
