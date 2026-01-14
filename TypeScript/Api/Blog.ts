/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { BlogIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Blog} (Api.Blogs.Blog)
**/
export type Blog = VersionedContent<uint> & {
    name?: string
    featureRef?: string
    iconRef?: string
    description?: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class BlogApi extends AutoController<Blog,uint, BlogIncludes>{

    constructor(){
        super('/v1/blog', new BlogIncludes());
    }

}

export default new BlogApi();
