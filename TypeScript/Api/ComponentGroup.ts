/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { ComponentGroupIncludes } from 'Api/Includes';

import { Role } from 'Api/Role';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {ComponentGroup} (Api.Components.ComponentGroup)
**/
export type ComponentGroup = VersionedContent<uint> & {
    name: string
    allowedComponents?: string
    roleId: uint
    // HasVirtualField() fields (2 in total)
    role?: Role;
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class ComponentGroupApi extends AutoController<ComponentGroup,uint, ComponentGroupIncludes>{

    constructor(){
        super('/v1/componentgroup', new ComponentGroupIncludes());
    }

}

export default new ComponentGroupApi();
