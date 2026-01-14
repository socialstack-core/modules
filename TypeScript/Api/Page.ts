/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { PageIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Page} (Api.Pages.Page)
**/
export type Page = VersionedContent<uint> & {
    url?: string
    openGraphImageRef?: string
    openGraphType?: string
    openGraphDescription?: string
    title: string
    key?: string
    primaryContentType?: string
    bodyJson: string
    description: string
    canIndex: boolean
    noFollow: boolean
    preferIfLoggedIn: boolean
    primaryContentIncludes?: string
    primaryContentIsRevisions: boolean
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {RouterNodeMetadata} (Api.Startup.Routing.RouterNodeMetadata)
**/
export type RouterNodeMetadata = {
    type?: string
    hasChildren: boolean
    fullRoute?: string
    childKey?: string
    name?: string
    contentId: ulong
    editUrl?: string
    createUrl?: string
}

/**
* This type was generated to reflect {TreeNodeDetail} (Api.Pages.PageController+TreeNodeDetail)
**/
export type TreeNodeDetail = {
    children?: RouterNodeMetadata[]
    self?: (RouterNodeMetadata | undefined)
}

/**
* This type was generated to reflect {RouterTreeLocation} (Api.Pages.PageController+RouterTreeLocation)
**/
export type RouterTreeLocation = {
    url?: string
}

/**
* This type was generated to reflect {PageStateResult} (Api.Pages.PageStateResult)
**/
export type PageStateResult = {
    oldVersion: boolean
    redirect?: string
    config?: Record<string,Config>
    page?: Page
    description?: string
    title?: string
    po?: Object
    tokenNames?: string[]
    tokens?: string[]
}

/**
* This type was generated to reflect {PageDetails} (Api.Pages.PageController+PageDetails)
**/
export type PageDetails = {
    url?: string
    version: long
}
// ENTITY CONTROLLER

export class PageApi extends AutoController<Page,uint, PageIncludes>{

    constructor(){
        super('/v1/page', new PageIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {PageController::GetRouterTreeNode}
     * @url /tree
     * @debug - method.ReturnType Api.Pages.PageController+TreeNodeDetail
     * @debug - method.TrueReturnType System.Nullable`1[[Api.Pages.PageController+TreeNodeDetail, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     getRouterTreeNode = (location: RouterTreeLocation): Promise<TreeNodeDetail> => {
        return getJson<TreeNodeDetail>(this.apiUrl + '/tree', location, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {PageController::GetRouterTreeNodePath}
     * @url /tree
     * @debug - method.ReturnType Api.Pages.PageController+TreeNodeDetail
     * @debug - method.TrueReturnType System.Nullable`1[[Api.Pages.PageController+TreeNodeDetail, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     getRouterTreeNodePath = (url?: string): Promise<TreeNodeDetail> => {
        return getJson<TreeNodeDetail>(this.apiUrl + '/tree?url=' + url + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {PageController::PageState}
     * @url /state
     * @debug - method.ReturnType Api.Pages.PageStateResult
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     pageState = (pageDetails?: PageDetails): Promise<PageStateResult> => {
        return getJson<PageStateResult>(this.apiUrl + '/state', pageDetails, { method: 'POST' } )
     }


}

export default new PageApi();
