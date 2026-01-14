/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { ProductCategoryIncludes } from 'Api/Includes';

import { RouterNodeMetadata } from 'Api/Page';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {BaseCategory} (Api.Payments.BaseCategory)
**/
export type BaseCategory = VersionedContent<uint> & {
    name: string
    slug?: string
    descriptionHtml: string
    featureRef?: string
    productImageRef?: string
    replaceWhiteWithTransparency: boolean
    productImageHorizontalOffset?: (int | undefined)
    productImageVerticalOffset?: (int | undefined)
    iconRef?: string
    parentId?: (uint | undefined)
    isPrimary: boolean
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {ProductCategory} (Api.Payments.ProductCategory)
**/
export type ProductCategory = BaseCategory & {
    // HasVirtualField() fields (2 in total)
    productCategory?: ProductCategory;
    creatorUser?: User;
}

/**
* This type was generated to reflect {ProductCategoryNode} (Api.Payments.ProductCategoryNode)
**/
export type ProductCategoryNode = CategoryNode<ProductCategory, ProductCategoryNode> & {
}

/**
* This type was generated to reflect {TreeNodeDetail} (Api.Pages.PageController+TreeNodeDetail)
**/
export type TreeNodeDetail = {
    children?: RouterNodeMetadata[]
    self?: (RouterNodeMetadata | undefined)
}

/**
* This type was generated to reflect {CategoryTreeLocation} (Api.Payments.CategoryTreeLocation)
**/
export type CategoryTreeLocation = {
    path?: string
}
// ENTITY CONTROLLER

export class ProductCategoryApi extends AutoController<ProductCategory,uint, ProductCategoryIncludes>{

    constructor(){
        super('/v1/productcategory', new ProductCategoryIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {BaseCategoryController<ProductCategory, ProductCategoryNode, ProductCategoryService>::Structure}
     * @url /structure
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.Payments.ProductCategoryNode, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.Payments.ProductCategoryNode, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     structure = (includeProducts: boolean): Promise<ProductCategoryNode[]> => {
        return getJson<ProductCategoryNode[]>(this.apiUrl + '/structure?includeProducts=' + includeProducts + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {BaseCategoryController<ProductCategory, ProductCategoryNode, ProductCategoryService>::PermalinkSync}
     * @url /permalink/sync
     * @debug - method.ReturnType System.String
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     permalinkSync = (): Promise<string> => {
        return getText(this.apiUrl + '/permalink/sync', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {BaseCategoryController<ProductCategory, ProductCategoryNode, ProductCategoryService>::GetTreeNode}
     * @url /tree
     * @debug - method.ReturnType Api.Pages.PageController+TreeNodeDetail
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Pages.PageController+TreeNodeDetail, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getTreeNode = (location: CategoryTreeLocation): Promise<TreeNodeDetail> => {
        return getJson<TreeNodeDetail>(this.apiUrl + '/tree', location, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {BaseCategoryController<ProductCategory, ProductCategoryNode, ProductCategoryService>::GetTreeNodePath}
     * @url /tree
     * @debug - method.ReturnType Api.Pages.PageController+TreeNodeDetail
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Pages.PageController+TreeNodeDetail, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getTreeNodePath = (path?: string): Promise<TreeNodeDetail> => {
        return getJson<TreeNodeDetail>(this.apiUrl + '/tree?path=' + path + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {BaseCategoryController<ProductCategory, ProductCategoryNode, ProductCategoryService>::GetProducts}
     * @url /{id}/products
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.Payments.Product, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.Payments.Product, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getProducts = (id: uint): Promise<Product[]> => {
        return getJson<Product[]>(this.apiUrl + '/' + id +'/products', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {BaseCategoryController<ProductCategory, ProductCategoryNode, ProductCategoryService>::GetProductCategories}
     * @url /product/{id}
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.Payments.ProductCategory, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.Payments.ProductCategory, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getProductCategories = (id: uint): Promise<ProductCategory[]> => {
        return getJson<ProductCategory[]>(this.apiUrl + '/product/' + id +'', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {BaseCategoryController<ProductCategory, ProductCategoryNode, ProductCategoryService>::GetChildren}
     * @url /{id}/children
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.Payments.ProductCategoryNode, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.Payments.ProductCategoryNode, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getChildren = (id: uint): Promise<ProductCategoryNode[]> => {
        return getJson<ProductCategoryNode[]>(this.apiUrl + '/' + id +'/children', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {BaseCategoryController<ProductCategory, ProductCategoryNode, ProductCategoryService>::GetParents}
     * @url /{id}/parents
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.Payments.ProductCategoryNode, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.Payments.ProductCategoryNode, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getParents = (id: uint): Promise<ProductCategoryNode[]> => {
        return getJson<ProductCategoryNode[]>(this.apiUrl + '/' + id +'/parents', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {BaseCategoryController<ProductCategory, ProductCategoryNode, ProductCategoryService>::GetDescendants}
     * @url /{id}/descendants
     * @debug - method.ReturnType Api.Startup.ContentStream`2[[Api.Payments.ProductCategory, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Startup.ContentStream`2[[Api.Payments.ProductCategory, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     getDescendants = (id: uint, includes?: ApiIncludes[]): Promise<ApiList<ProductCategory>> => {
        return getList<ProductCategory>(this.apiUrl + '/' + id +'/descendants' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


}

export default new ProductCategoryApi();
