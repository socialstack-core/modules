/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { TagIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

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
// ENTITY CONTROLLER

export class TagApi extends AutoController<Tag,uint, TagIncludes>{

    constructor(){
        super('/v1/tag', new TagIncludes());
    }

}

export default new TagApi();
