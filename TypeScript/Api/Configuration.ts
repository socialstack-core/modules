/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { ConfigurationIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Configuration} (Api.Configuration.Configuration)
**/
export type Configuration = VersionedContent<uint> & {
    name?: string
    environments?: string
    key?: string
    configJson?: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class ConfigurationApi extends AutoController<Configuration,uint, ConfigurationIncludes>{

    constructor(){
        super('/v1/configuration', new ConfigurationIncludes());
    }

}

export default new ConfigurationApi();
