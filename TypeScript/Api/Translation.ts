/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { TranslationIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Translation} (Api.Translate.Translation)
**/
export type Translation = VersionedContent<uint> & {
    module?: string
    original?: string
    translated: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class TranslationApi extends AutoController<Translation,uint, TranslationIncludes>{

    constructor(){
        super('/v1/translation', new TranslationIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {TranslationController::PrePopulate}
     * @url /prepopulate
     * @debug - method.ReturnType System.Object
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     prePopulate = (): Promise<Object> => {
        return getJson<Object>(this.apiUrl + '/prepopulate', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {TranslationController::LoadPotFiles}
     * @url /potfiles
     * @debug - method.ReturnType System.Object
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     loadPotFiles = (): Promise<Object> => {
        return getJson<Object>(this.apiUrl + '/potfiles', undefined, { method: 'GET' } )
     }


}

export default new TranslationApi();
