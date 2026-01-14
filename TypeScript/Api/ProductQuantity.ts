/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { ProductQuantityIncludes } from 'Api/Includes';

import { Product } from 'Api/Product';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {ProductQuantity} (Api.Payments.ProductQuantity)
**/
export type ProductQuantity = VersionedContent<uint> & {
    productId: uint
    quantity: ulong
    orderedTotal: ulong
    orderedTotalLessTax: ulong
    orderedCurrencyCode?: string
    // HasVirtualField() fields (2 in total)
    product?: Product;
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class ProductQuantityApi extends AutoController<ProductQuantity,uint, ProductQuantityIncludes>{

    constructor(){
        super('/v1/productusage', new ProductQuantityIncludes());
    }

}

export default new ProductQuantityApi();
