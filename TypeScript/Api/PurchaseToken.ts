/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { PurchaseTokenIncludes } from 'Api/Includes';

import { Purchase } from 'Api/Purchase';

// TYPES

/**
* This type was generated to reflect {PurchaseToken} (Api.Payments.PurchaseToken)
**/
export type PurchaseToken = Content<uint> & {
    token?: string
    purchaseId: uint
    // HasVirtualField() fields (1 in total)
    purchase?: Purchase;
}
// ENTITY CONTROLLER

export class PurchaseTokenApi extends AutoController<PurchaseToken,uint, PurchaseTokenIncludes>{

    constructor(){
        super('/v1/purchasetoken', new PurchaseTokenIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {PurchaseTokenController::CheckTokenExists}
     * @url /token/{token}
     * @debug - method.ReturnType System.Object
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Object, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     checkTokenExists = (token?: string): Promise<Object> => {
        return getJson<Object>(this.apiUrl + '/token/' + token +'', undefined, { method: 'GET' } )
     }


}

export default new PurchaseTokenApi();
