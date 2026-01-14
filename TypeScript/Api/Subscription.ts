/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { SubscriptionIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Subscription} (Api.Payments.Subscription)
**/
export type Subscription = VersionedContent<uint> & {
    lastChargeUtc: Date | string | number
    nextChargeUtc: Date | string | number
    willCancel: boolean
    couponId: uint
    timeslotFrequency: uint
    status: uint
    paymentMethodId: uint
    deliveryOptionId: uint
    deliveryAddressId: uint
    billingAddressId: uint
    localeId: uint
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {CardUpdateStatus} (Api.Payments.CardUpdateStatus)
**/
export type CardUpdateStatus = {
    status: uint
}
// ENTITY CONTROLLER

export class SubscriptionApi extends AutoController<Subscription,uint, SubscriptionIncludes>{

    constructor(){
        super('/v1/subscription', new SubscriptionIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {SubscriptionController::UpdateCard}
     * @url /{id}/update-card
     * @debug - method.ReturnType Api.Payments.CardUpdateStatus
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.CardUpdateStatus, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     updateCard = (id: uint, cardUpdate?: Partial<T>): Promise<CardUpdateStatus> => {
        return getJson<CardUpdateStatus>(this.apiUrl + '/' + id +'/update-card', cardUpdate, { method: 'POST' } )
     }


}

export default new SubscriptionApi();
