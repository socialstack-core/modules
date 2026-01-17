/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { ProductIncludes } from 'Api/Includes';

import { User } from 'Api/User';

import { ProductCategory } from 'Api/ProductCategory';

import { ProductTemplate } from 'Api/ProductTemplate';

// TYPES

/**
* This type was generated to reflect {ProductBase} (Api.Payments.ProductBase)
**/
export type ProductBase = VersionedContent<uint> & {
    productType: uint
    name: string
    slug?: string
    freeSampleNominalValue?: (uint | undefined)
    isBilledByUsage: boolean
    isFeatured: boolean
    billingFrequency: uint
    taxExempt: uint
    availability: uint
    descriptionHtml: string
    sellUnitDescription?: string
    featureRef?: string
    priceStrategy: uint
    stock?: (uint | undefined)
    variantOfId?: (uint | undefined)
    continueSellingWithNoStock: boolean
    priceTiersJson: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {Product} (Api.Payments.Product)
**/
export type Product = ProductBase & {
    score: double
    sku?: string
    popularity: double
    productTemplateId: uint
    // HasVirtualField() fields (3 in total)
    primaryCategory?: ProductCategory;
    productTemplate?: ProductTemplate;
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class ProductApi extends AutoController<Product,uint, ProductIncludes>{

    constructor(){
        super('/v1/product', new ProductIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {ProductController::AdminTriggerPermalinkSync}
     * @url /permalink/sync
     * @debug - method.ReturnType System.String
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.String, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     adminTriggerPermalinkSync = (): Promise<string> => {
        return getText(this.apiUrl + '/permalink/sync', undefined, { method: 'GET' } )
     }


}

export default new ProductApi();
