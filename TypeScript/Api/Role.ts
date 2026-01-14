/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { RoleIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Role} (Api.Permissions.Role)
**/
export type Role = VersionedContent<uint> & {
    name?: string
    key?: string
    canViewAdmin: boolean
    isComposite: boolean
    adminDashboardJson?: string
    grantRuleJson?: string
    inheritedRoleId: uint
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class RoleApi extends AutoController<Role,uint, RoleIncludes>{

    constructor(){
        super('/v1/role', new RoleIncludes());
    }

}

export default new RoleApi();
