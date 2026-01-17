/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

// TYPES

/**
* This type was generated to reflect {StripeIntentResponse} (Api.Payments.StripeController+StripeIntentResponse)
**/
export type StripeIntentResponse = {
    clientSecret?: string
}

/**
* This type was generated to reflect {PublicMessage} (Api.Startup.PublicMessage)
**/
export type PublicMessage = {
    message?: string
    code?: string
}
// NON-ENTITY CONTROLLERS

export class StripeController {

   private apiUrl: string = '/v1/stripe-gateway';

     /*
     * Generated from a .NET type
     * @see {StripeController::SetupIntent}
     * @url /setup
     * @debug - method.ReturnType Api.Payments.StripeController+StripeIntentResponse
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Payments.StripeController+StripeIntentResponse, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     setupIntent = (): Promise<StripeIntentResponse> => {
        return getJson<StripeIntentResponse>(this.apiUrl + '/setup', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {StripeController::Webhook}
     * @url /webhook
     * @debug - method.ReturnType Api.Startup.PublicMessage
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.PublicMessage, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     webhook = (): Promise<PublicMessage> => {
        return getJson<PublicMessage>(this.apiUrl + '/webhook', undefined, { method: 'POST' } )
     }


}

export default new StripeController();
