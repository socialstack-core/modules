/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { PurchaseIncludes } from 'Api/Includes';

import { Address } from 'Api/Address';

import { DeliveryOption } from 'Api/DeliveryOption';

import { GuestUser } from 'Api/GuestUser';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Purchase} (Api.Payments.Purchase)
**/
export type Purchase = VersionedContent<uint> & {
    status: uint
    authorise: boolean
    multiExecute: boolean
    localeId: uint
    paymentGatewayInternalId?: string
    paymentGatewayId: uint
    paymentMethodId: uint
    currencyCode?: string
    productsCostLessTax: ulong
    productsCost: ulong
    totalCostLessTax: ulong
    totalCost: ulong
    deliveryCostLessTax: ulong
    deliveryCost: ulong
    deliveryApportionment?: (double | undefined)
    contentType?: string
    contentId: uint
    deliveryAddressId: uint
    billingAddressId: uint
    deliveryOptionId: uint
    ipAddress?: string
    gatewayResponseJson: string
    reference?: string
    guestUserId?: (uint | undefined)
    // HasVirtualField() fields (5 in total)
    deliveryAddress?: Address;
    billingAddress?: Address;
    deliveryOption?: DeliveryOption;
    guestUser?: GuestUser;
    creatorUser?: User;
}

/**
* This type was generated to reflect {PurchaseStatus} (Api.Payments.PurchaseStatus)
**/
export type PurchaseStatus = {
    status: uint
    nextAction?: string
}
// ENTITY CONTROLLER

export class PurchaseApi extends AutoController<Purchase,uint, PurchaseIncludes>{

    constructor(){
        super('/v1/purchase', new PurchaseIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {PurchaseController::ValidateChallenge}
     * @url /opayo/challenge/callback
     * @debug - method.ReturnType Api.Payments.PurchaseStatus
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.PurchaseStatus, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     validateChallenge = (): Promise<PurchaseStatus> => {
        return getJson<PurchaseStatus>(this.apiUrl + '/opayo/challenge/callback', undefined, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {PurchaseController::ApprovalStatus}
     * @url /approval/status/{token}
     * @debug - method.ReturnType Api.Payments.PurchaseStatus
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.PurchaseStatus, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     approvalStatus = (token?: string): Promise<PurchaseStatus> => {
        return getJson<PurchaseStatus>(this.apiUrl + '/approval/status/' + token +'', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {PurchaseController::GetByToken}
     * @url /get/token/{token}
     * @debug - method.ReturnType Api.Payments.Purchase
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.Purchase, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     getByToken = (token?: string, includes?: ApiIncludes[]): Promise<Purchase> => {
        return getOne<Purchase>(this.apiUrl + '/get/token/' + token  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {PurchaseController::ResendEmail}
     * @url /resend/email/{id}
     * @debug - method.ReturnType System.Boolean
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Boolean, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     resendEmail = (id: uint, key?: string): Promise<boolean> => {
        return getJson<boolean>(this.apiUrl + '/resend/email/' + id +'?key=' + key + '', undefined, { method: 'GET' } )
     }


}

export default new PurchaseApi();
