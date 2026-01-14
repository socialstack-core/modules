/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { BlogPostIncludes } from 'Api/Includes';

import { Blog } from 'Api/Blog';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {BlogPost} (Api.Blogs.BlogPost)
**/
export type BlogPost = VersionedContent<uint> & {
    blogId: uint
    title?: string
    bodyHtml?: string
    slug?: string
    featureRef?: string
    iconRef?: string
    description?: string
    synopsis?: string
    readTime: int
    // HasVirtualField() fields (2 in total)
    blog?: Blog;
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class BlogPostApi extends AutoController<BlogPost,uint, BlogPostIncludes>{

    constructor(){
        super('/v1/blogpost', new BlogPostIncludes());
    }

}

export default new BlogPostApi();
