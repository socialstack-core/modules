/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { NavMenuItemIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {NavMenuItem} (Api.NavMenus.NavMenuItem)
**/
export type NavMenuItem = VersionedContent<uint> & {
    navMenuId: uint
    menuKey?: string
    parentItemId?: (uint | undefined)
    bodyJson: string
    target?: string
    iconRef?: string
    order: int
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class NavMenuItemApi extends AutoController<NavMenuItem,uint, NavMenuItemIncludes>{

    constructor(){
        super('/v1/navmenuitem', new NavMenuItemIncludes());
    }

}

export default new NavMenuItemApi();
