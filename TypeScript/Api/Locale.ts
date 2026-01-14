/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { LocaleIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Locale} (Api.Translate.Locale)
**/
export type Locale = VersionedContent<uint> & {
    defaultTaxJurisdiction?: string
    currencyCode?: string
    name: string
    code?: string
    flagIconRef?: string
    aliases?: string
    isDisabled: boolean
    isRedirected: boolean
    permanentRedirect: boolean
    rightToLeft: boolean
    pagePath?: string
    domains?: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class LocaleApi extends AutoController<Locale,uint, LocaleIncludes>{

    constructor(){
        super('/v1/locale', new LocaleIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {LocaleController::Set}
     * @url /set/{id}
     * @debug - method.ReturnType Api.Contexts.Context
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Contexts.Context, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     set = (setSession: (s: SessionResponse) => Session, id: uint): Promise<Session> => {
        return getJson<SessionResponse>(this.apiUrl + '/set/' + id +'', undefined, { method: 'GET' } )
            .then(setSession)
     }


}

export default new LocaleApi();
