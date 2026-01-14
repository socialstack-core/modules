/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

import { ApiIncludes } from 'Api/Includes';

import { Product } from 'Api/Product';

// ENUMS

export enum ProductSearchType {
    Reductive=0,
    Expansive=1,
}

export enum SortDirection {
    ASC=0,
    DESC=1,
}

// TYPES

/**
* This type was generated to reflect {ProductSearchAppliedFacet} (Api.Payments.ProductSearchAppliedFacet)
**/
export type ProductSearchAppliedFacet = {
    mapping?: string
    ids?: ulong[]
}

/**
* This type was generated to reflect {SortOrder} (Api.Payments.SortOrder)
**/
export type SortOrder = {
    field?: string
    direction: SortDirection
}

/**
* This type was generated to reflect {ProductSearchRequest} (Api.Payments.ProductSearchRequest)
**/
export type ProductSearchRequest = {
    pageOffset: int
    query?: string
    appliedFacets?: ProductSearchAppliedFacet[]
    pageSize: uint
    searchType: ProductSearchType
    includePriceStats: boolean
    includeDynamicBoosts: boolean
    hideInactiveProducts: boolean
    isAdminPanel: boolean
    minPrice?: (double | undefined)
    maxPrice?: (double | undefined)
    inStockOnly: boolean
    sortOrder: SortOrder
    excludedIds?: uint[]
    customParameters?: Record<string,Object>
}
// NON-ENTITY CONTROLLERS

export class ProductSearchController {

   private apiUrl: string = '/v1/product/search';

     /*
     * Generated from a .NET type
     * @see {ProductSearchController::GetFaceted}
     * @url /faceted
     * @debug - method.ReturnType Api.Startup.ContentStream`2[[Api.Payments.Product, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.ContentStream`2[[Api.Payments.Product, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getFaceted = (query?: string, includes?: ApiIncludes[]): Promise<ApiList<Product>> => {
        return getList<Product>(this.apiUrl + '/faceted?query=' + query + '' + (Array.isArray(includes) ? '&includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {ProductSearchController::Faceted}
     * @url /faceted
     * @debug - method.ReturnType Api.Startup.ContentStream`2[[Api.Payments.Product, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.ContentStream`2[[Api.Payments.Product, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     faceted = (request?: ProductSearchRequest, includes?: ApiIncludes[]): Promise<ApiList<Product>> => {
        return getList<Product>(this.apiUrl + '/faceted' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', request, { method: 'POST' } )
     }


}

export default new ProductSearchController();
