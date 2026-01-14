/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { ProductTemplateIncludes } from 'Api/Includes';

import { User } from 'Api/User';

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
* This type was generated to reflect {ProductTemplate} (Api.Payments.ProductTemplate)
**/
export type ProductTemplate = ProductBase & {
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class ProductTemplateApi extends AutoController<ProductTemplate,uint, ProductTemplateIncludes>{

    constructor(){
        super('/v1/producttemplate', new ProductTemplateIncludes());
    }

}

export default new ProductTemplateApi();
