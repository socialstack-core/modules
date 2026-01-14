/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { SubscriptionUsageIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {SubscriptionUsage} (Api.Payments.SubscriptionUsage)
**/
export type SubscriptionUsage = UserCreatedContent<uint> & {
    productId: uint
    subscriptionId: uint
    maximumUsageToday: uint
    chargedTimeslotId: uint
    dateUtc: Date | string | number
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class SubscriptionUsageApi extends AutoController<SubscriptionUsage,uint, SubscriptionUsageIncludes>{

    constructor(){
        super('/v1/subscriptionusage', new SubscriptionUsageIncludes());
    }

}

export default new SubscriptionUsageApi();
