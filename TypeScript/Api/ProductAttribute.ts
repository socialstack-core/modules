/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { ProductAttributeIncludes } from 'Api/Includes';

import { RouterNodeMetadata } from 'Api/Page';

import { ProductAttributeGroup } from 'Api/ProductAttributeGroup';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {ProductAttribute} (Api.Payments.ProductAttribute)
**/
export type ProductAttribute = VersionedContent<uint> & {
    name: string
    key?: string
    productAttributeGroupId: uint
    productAttributeType: int
    rangeType: int
    multiple: boolean
    units?: string
    featureRef?: string
    // HasVirtualField() fields (2 in total)
    attributeGroup?: ProductAttributeGroup;
    creatorUser?: User;
}

/**
* This type was generated to reflect {TreeNodeDetail} (Api.Pages.PageController+TreeNodeDetail)
**/
export type TreeNodeDetail = {
    children?: RouterNodeMetadata[]
    self?: (RouterNodeMetadata | undefined)
}

/**
* This type was generated to reflect {AttributeTreeLocation} (Api.Payments.AttributeTreeLocation)
**/
export type AttributeTreeLocation = {
    path?: string
}
// ENTITY CONTROLLER

export class ProductAttributeApi extends AutoController<ProductAttribute,uint, ProductAttributeIncludes>{

    constructor(){
        super('/v1/productattribute', new ProductAttributeIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {ProductAttributeController::GetTreeNode}
     * @url /tree
     * @debug - method.ReturnType Api.Pages.PageController+TreeNodeDetail
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Pages.PageController+TreeNodeDetail, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getTreeNode = (location: AttributeTreeLocation): Promise<TreeNodeDetail> => {
        return getJson<TreeNodeDetail>(this.apiUrl + '/tree', location, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {ProductAttributeController::GetTreeNodePath}
     * @url /tree
     * @debug - method.ReturnType Api.Pages.PageController+TreeNodeDetail
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Pages.PageController+TreeNodeDetail, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getTreeNodePath = (path?: string): Promise<TreeNodeDetail> => {
        return getJson<TreeNodeDetail>(this.apiUrl + '/tree?path=' + path + '', undefined, { method: 'GET' } )
     }


}

export default new ProductAttributeApi();
