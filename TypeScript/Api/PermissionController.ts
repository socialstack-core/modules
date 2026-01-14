/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

import { Role } from 'Api/Role';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {GrantMeta} (Api.Permissions.GrantMeta)
**/
export type GrantMeta = {
    ruleDescription?: string
    role?: Role
}

/**
* This type was generated to reflect {PermissionMeta} (Api.Permissions.PermissionMeta)
**/
export type PermissionMeta = {
    key?: string
    description?: string
    grants?: GrantMeta[]
}

/**
* This type was generated to reflect {PermissionInformation} (Api.Permissions.PermissionInformation)
**/
export type PermissionInformation = {
    capabilities?: PermissionMeta[]
    roles?: Role[]
}
// NON-ENTITY CONTROLLERS

export class PermissionController {

   private apiUrl: string = '/v1/permission';

     /*
     * Generated from a .NET type
     * @see {PermissionController::List}
     * @url /list
     * @debug - method.ReturnType Api.Permissions.PermissionInformation
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Permissions.PermissionInformation, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     list = (): Promise<PermissionInformation> => {
        return getJson<PermissionInformation>(this.apiUrl + '/list', undefined, { method: 'GET' } )
     }


}

export default new PermissionController();
