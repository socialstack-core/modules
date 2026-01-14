/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController, FilterSortConfig } from 'Api/Content';

import { AdminNavMenuItemIncludes } from 'Api/Includes';

// TYPES

/**
* This type was generated to reflect {AdminNavMenuItem} (Api.NavMenus.AdminNavMenuItem)
**/
export type AdminNavMenuItem = Content<uint> & {
    title?: string
    pageKey?: string
    url?: string
    iconRef?: string
    key?: string
    parentId: uint
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
// ENTITY CONTROLLER

export class AdminNavMenuItemApi extends AutoController<AdminNavMenuItem,uint, AdminNavMenuItemIncludes>{

    constructor(){
        super('/v1/adminnavmenuitem', new AdminNavMenuItemIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {AdminNavMenuItemController::List}
     * @url /list
     * @debug - method.ReturnType Api.Startup.ContentStream`2[[Api.NavMenus.AdminNavMenuItem, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.ContentStream`2[[Api.NavMenus.AdminNavMenuItem, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     list = (filters?: ListFilter, includes?: ApiIncludes[]): Promise<ApiList<AdminNavMenuItem>> => {
        return getList<AdminNavMenuItem>(this.apiUrl + '/list' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', filters, { method: 'POST' } )
     }


}

export default new AdminNavMenuItemApi();
