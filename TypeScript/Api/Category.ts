/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { CategoryIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

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
// ENTITY CONTROLLER

export class CategoryApi extends AutoController<Category,uint, CategoryIncludes>{

    constructor(){
        super('/v1/category', new CategoryIncludes());
    }

}

export default new CategoryApi();
