/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { CustomContentTypeIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {CustomContentType} (Api.CustomContentTypes.CustomContentType)
**/
export type CustomContentType = VersionedContent<uint> & {
    name?: string
    nickName?: string
    summary?: string
    iconRef?: string
    deleted: boolean
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {CustomTypeInfo} (Api.CustomContentTypes.CustomContentTypeController+CustomTypeInfo)
**/
export type CustomTypeInfo = {
    name?: string
    value?: string
}
// ENTITY CONTROLLER

export class CustomContentTypeApi extends AutoController<CustomContentType,uint, CustomContentTypeIncludes>{

    constructor(){
        super('/v1/customcontenttype', new CustomContentTypeIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {CustomContentTypeController::GetAllTypes}
     * @url /alltypes
     * @debug - method.ReturnType System.Collections.Generic.List`1[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
     * @debug - method.TrueReturnType System.Collections.Generic.List`1[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getAllTypes = (): Promise<string[]> => {
        return getJson<string[]>(this.apiUrl + '/alltypes', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {CustomContentTypeController::GetAllTypesPlus}
     * @url /allcustomtypesplus
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.CustomContentTypes.CustomContentTypeController+CustomTypeInfo, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.CustomContentTypes.CustomContentTypeController+CustomTypeInfo, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getAllTypesPlus = (): Promise<CustomTypeInfo[]> => {
        return getJson<CustomTypeInfo[]>(this.apiUrl + '/allcustomtypesplus', undefined, { method: 'GET' } )
     }


}

export default new CustomContentTypeApi();
