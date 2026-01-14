/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { ProductAttributeValueIncludes } from 'Api/Includes';

import { ProductAttribute } from 'Api/ProductAttribute';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {ProductAttributeValue} (Api.Payments.ProductAttributeValue)
**/
export type ProductAttributeValue = VersionedContent<uint> & {
    productAttributeId: uint
    value?: string
    featureRef?: string
    // HasVirtualField() fields (2 in total)
    attribute?: ProductAttribute;
    creatorUser?: User;
}

/**
* This type was generated to reflect {OrphanedAttributeCleanupResponse} (Api.Payments.OrphanedAttributeCleanupResponse)
**/
export type OrphanedAttributeCleanupResponse = {
    allValuesCount: int
    allAttributesCount: int
    cleanedUpValuesCount: int
}
// ENTITY CONTROLLER

export class ProductAttributeValueApi extends AutoController<ProductAttributeValue,uint, ProductAttributeValueIncludes>{

    constructor(){
        super('/v1/productattributevalue', new ProductAttributeValueIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {ProductAttributeValueController::DeleteOrphanedAttributeValues}
     * @url /cleanup
     * @debug - method.ReturnType Api.Payments.OrphanedAttributeCleanupResponse
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Payments.OrphanedAttributeCleanupResponse, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     deleteOrphanedAttributeValues = (): Promise<OrphanedAttributeCleanupResponse> => {
        return getJson<OrphanedAttributeCleanupResponse>(this.apiUrl + '/cleanup', undefined, { method: 'GET' } )
     }


}

export default new ProductAttributeValueApi();
