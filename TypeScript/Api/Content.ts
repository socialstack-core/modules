/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

import { ApiIncludes } from 'Api/Includes';

import { User } from 'Api/User';

import { ProductAttributeGroup } from 'Api/ProductAttributeGroup';

import { ProductAttribute } from 'Api/ProductAttribute';

import { ProductCategory } from 'Api/ProductCategory';

import { Product } from 'Api/Product';

import { ProductTemplate } from 'Api/ProductTemplate';

import { Role } from 'Api/Role';

import { CustomContentType } from 'Api/CustomContentType';

// OPEN GENERICS

export type Content<ID> = {
    type?: string
    id: ID
    // adding (32) global virtual fields.
    recentDraft: ulong
    primaryUrl?: string
    breadcrumb?: CategoryBreadcrumb[]
    calculatedPrice?: PriceCurrency[]
    cartContents?: ProductQuantityPricing
    productNotices?: ProductNotice[]
    emailAddress?: string
    signedRef128?: string
    signedRef256?: string
    signedRefOriginal?: string
    rolePermits?: Role[]
    roleExclusions?: Role[]
    composition?: Role[]
    tags?: Tag[]
    deliveries?: Delivery[]
    requiredAttributes?: ProductAttribute[]
    attributes?: ProductAttributeValue[]
    additionalAttributes?: ProductAttributeValue[]
    productCategories?: ProductCategory[]
    productQuantities?: ProductQuantity[]
    requestedProductQuantities?: ProductQuantity[]
    optionalExtras?: Product[]
    accessories?: Product[]
    suggestions?: Product[]
    variants?: Product[]
    subscriptions?: Subscription[]
    userPermits?: User[]
    customContentTypeFields?: CustomContentTypeField[]
    categories?: Category[]
    productImages?: Upload[]
    productDownloads?: Upload[]
    uploads?: Upload[]
}



export type UserCreatedContent<T> = Content<T> & {
    userId: uint
    createdUtc: Date | string | number
    editedUtc: Date | string | number
}



export type VersionedContent<T> = UserCreatedContent<T> & {
    revision: int
}



export type Revision<T, ID> = UserCreatedContent<ID> & {
    contentJson?: string
    contentId: ID
    isDraft: boolean
    publishDraftDate?: (Date | string | number | undefined)
    actionType: uint
    impersonatorUserId: uint
}



// TYPES

/**
* This type was generated to reflect {CategoryBreadcrumb} (Api.Payments.CategoryBreadcrumb)
**/
export type CategoryBreadcrumb = {
    id: uint
    name?: string
    primaryUrl?: string
}

/**
* This type was generated to reflect {PriceCurrency} (Api.Payments.PriceCurrency)
**/
export type PriceCurrency = {
    currencyCode?: string
    amount: uint
    amountLessTax: uint
}

/**
* This type was generated to reflect {LineItem} (Api.Payments.LineItem)
**/
export type LineItem = {
    productQuantityId: uint
    errorMessage?: string
    errorCode?: string
    productId: uint
    quantity: ulong
    total: ulong
    totalLessTax: ulong
}

/**
* This type was generated to reflect {ProductQuantityPricing} (Api.Payments.ProductQuantityPricing)
**/
export type ProductQuantityPricing = {
    total: ulong
    totalLessTax: ulong
    totalPD: ulong
    totalPDLessTax: ulong
    taxJurisdiction?: string
    couponId: uint
    currencyCode?: string
    contents?: LineItem[]
    errorMessage?: string
    errorCode?: string
    hasSubscriptionProducts: boolean
}

/**
* This type was generated to reflect {ProductNotice} (Api.Payments.ProductNotice)
**/
export type ProductNotice = {
    type?: string
    message?: string
}

/**
* This type was generated to reflect {Tag} (Api.Tags.Tag)
**/
export type Tag = VersionedContent<uint> & {
    name: string
    description: string
    featureRef?: string
    iconRef?: string
    hexColor?: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {Delivery} (Api.Payments.Delivery)
**/
export type Delivery = VersionedContent<uint> & {
    expectedSlotUtc: Date | string | number
    timeWindowLength: uint
    actualUtc?: (Date | string | number | undefined)
    deliveryName?: string
    deliveryNotes?: string
    deliveryCost: uint
    deliveryCostLessTax: uint
    totalCost: uint
    totalCostLessTax: uint
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

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

/**
* This type was generated to reflect {Subscription} (Api.Payments.Subscription)
**/
export type Subscription = VersionedContent<uint> & {
    lastChargeUtc: Date | string | number
    nextChargeUtc: Date | string | number
    willCancel: boolean
    couponId: uint
    timeslotFrequency: uint
    status: uint
    paymentMethodId: uint
    deliveryOptionId: uint
    deliveryAddressId: uint
    billingAddressId: uint
    localeId: uint
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {CustomContentTypeField} (Api.CustomContentTypes.CustomContentTypeField)
**/
export type CustomContentTypeField = VersionedContent<uint> & {
    customContentTypeId: uint
    defaultValue?: string
    dataType?: string
    linkedEntity?: string
    name?: string
    nickName?: string
    localised: boolean
    urlEncoded: boolean
    isHidden: boolean
    hideSeconds: boolean
    roundMinutes: boolean
    validation?: string
    order: uint
    group?: string
    optionsArePrices: boolean
    deleted: boolean
    // HasVirtualField() fields (2 in total)
    customContentType?: CustomContentType;
    creatorUser?: User;
}

/**
* This type was generated to reflect {Category} (Api.Categories.Category)
**/
export type Category = VersionedContent<uint> & {
    name: string
    description: string
    featureRef?: string
    iconRef?: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {Upload} (Api.Uploader.Upload)
**/
export type Upload = VersionedContent<uint> & {
    ref?: string
    originalName?: string
    fileType?: string
    variants?: string
    blurhash?: string
    width?: (int | undefined)
    height?: (int | undefined)
    focalX?: (int | undefined)
    focalY?: (int | undefined)
    alt?: string
    author?: string
    usageCount?: (int | undefined)
    isImage: boolean
    isPrivate: boolean
    isVideo: boolean
    isAudio: boolean
    transcodeState: int
    coverImageRef?: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {FilterSortConfig} (Api.Startup.FilterSortConfig)
**/
export type FilterSortConfig = {
    field?: string
    direction?: string
}

/**
* This type was generated to reflect {ListFilter} (Api.Startup.ListFilter)
**/
export type ListFilter = {
    pageSize: int
    pageIndex: int
    query?: string
    sort?: (FilterSortConfig | undefined)
    includeTotal?: (boolean | undefined)
    args?: Object[]
}
// AUTO CONTROLLERS

export class AutoController<T extends Content<uint>, ID, Includes extends ApiIncludes> {

    protected apiUrl: string;

    public includes: Includes;
    constructor(baseUrl: string = '', includes: Includes) {
        this.apiUrl = baseUrl?.toLowerCase();
        this.includes = includes
    }
     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::ListCSV}
     * @url /list.csv
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     listCSV = (query?: string, args?: string, fileName?: string, includes?: ApiIncludes[]): Promise<string> => {
        return getText(this.apiUrl + '/list.csv?query=' + query + '&args=' + args + '&fileName=' + fileName + '' + (Array.isArray(includes) ? '&includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::LoadRevision}
     * @url /revision/{id}
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     loadRevision = (id: ID, includes?: ApiIncludes[]): Promise<Revision<T, ID>> => {
        return getOne<Revision<T, ID>>(this.apiUrl + '/revision/' + id  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::LoadFromRevision}
     * @url /revision/content/{id}
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     loadFromRevision = (id: ID, includes?: ApiIncludes[]): Promise<T> => {
        return getOne<T>(this.apiUrl + '/revision/content/' + id  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::DeleteRevision}
     * @url /revision/{id}
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     deleteRevision = (id: ID, includes?: ApiIncludes[]): Promise<Revision<T, ID>> => {
        return getOne<Revision<T, ID>>(this.apiUrl + '/revision/' + id  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'DELETE' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::RevisionListAll}
     * @url /revision/list
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     revisionListAll = (includes?: ApiIncludes[]): Promise<ApiList<Revision<T, ID>>> => {
        return getList<Revision<T, ID>>(this.apiUrl + '/revision/list' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::RevisionList}
     * @url /revision/list
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     revisionList = (filters?: ListFilter, includes?: ApiIncludes[]): Promise<ApiList<Revision<T, ID>>> => {
        return getList<Revision<T, ID>>(this.apiUrl + '/revision/list' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', filters, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::PublishRevision}
     * @url /publish/{id}
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     publishRevision = (id: ID, includes?: ApiIncludes[]): Promise<T> => {
        return getOne<T>(this.apiUrl + '/publish/' + id  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::CreateDraft}
     * @url /draft
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     createDraft = (body?: Partial<T>, includes?: ApiIncludes[]): Promise<Revision<T, ID>> => {
        return getOne<Revision<T, ID>>(this.apiUrl + '/draft' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', body, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::Load}
     * @url /{id}
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     load = (id: ID, includes?: ApiIncludes[]): Promise<T> => {
        return getOne<T>(this.apiUrl + '/' + id  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::Delete}
     * @url /{id}
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     delete = (id: ID, includes?: ApiIncludes[]): Promise<T> => {
        return getOne<T>(this.apiUrl + '/' + id  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'DELETE' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::InvalidateCachedItem}
     * @url /cache/invalidate/{id}
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     invalidateCachedItem = (id: ID, includes?: ApiIncludes[]): Promise<string> => {
        return getText(this.apiUrl + '/cache/invalidate/' + id  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::InvalidateCache}
     * @url /cache/invalidate
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     invalidateCache = (includes?: ApiIncludes[]): Promise<string> => {
        return getText(this.apiUrl + '/cache/invalidate' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::ListAll}
     * @url /list
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     listAll = (includes?: ApiIncludes[]): Promise<ApiList<T>> => {
        return getList<T>(this.apiUrl + '/list' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::List}
     * @url /list
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     list = (filters?: ListFilter, includes?: ApiIncludes[]): Promise<ApiList<T>> => {
        return getList<T>(this.apiUrl + '/list' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', filters, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::Create}
     * @url /
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     create = (body?: Partial<T>, includes?: ApiIncludes[]): Promise<T> => {
        return getOne<T>(this.apiUrl + '/' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', body, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::Update}
     * @url /{id}
     * @debug - method.ReturnType 
     * @debug - method.TrueReturnType 
      */
     update = (id: ID, body?: Partial<T>, includes?: ApiIncludes[]): Promise<T> => {
        return getOne<T>(this.apiUrl + '/' + id  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', body, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::ListPOTUpdate}
     * @url /list.pot
     * @debug - method.ReturnType System.Object
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     listPOTUpdate = (includes?: ApiIncludes[]): Promise<Object> => {
        return getJson<Object>(this.apiUrl + '/list.pot' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'PUT' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoController<T, ID>::ListPOT}
     * @url /list.pot
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     listPOT = (filters?: Partial<T>, ignoreFields?: string, includes?: ApiIncludes[]): Promise<string> => {
        return getText(this.apiUrl + '/list.pot?ignoreFields=' + ignoreFields + '' + (Array.isArray(includes) ? '&includes=' + includes.join(',') : '') + '', filters, { method: 'POST' } )
     }


}

