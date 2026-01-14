/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { ProductAttributeGroupIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {ProductAttributeGroup} (Api.Payments.ProductAttributeGroup)
**/
export type ProductAttributeGroup = VersionedContent<uint> & {
    key?: string
    parentGroupId: uint
    name: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class ProductAttributeGroupApi extends AutoController<ProductAttributeGroup,uint, ProductAttributeGroupIncludes>{

    constructor(){
        super('/v1/productattributegroup', new ProductAttributeGroupIncludes());
    }

}

export default new ProductAttributeGroupApi();
