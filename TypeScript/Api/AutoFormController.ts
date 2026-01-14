/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

import { ContentType } from 'Api/AvailableEndpointController';

// TYPES

/**
* This type was generated to reflect {AutoFormField} (Api.AutoForms.AutoFormField)
**/
export type AutoFormField = {
    includable: boolean
    valueType?: string
    module?: string
    order: uint
    data?: Record<string,Object>
    tokeniseable: boolean
    fieldName?: string
}

/**
* This type was generated to reflect {AutoFormInfo} (Api.AutoForms.AutoFormInfo)
**/
export type AutoFormInfo = {
    supportsRevisions: boolean
    contentType?: string
    endpoint?: string
    fields?: AutoFormField[]
}

/**
* This type was generated to reflect {AutoFormStructure} (Api.AutoForms.AutoFormStructure)
**/
export type AutoFormStructure = {
    forms?: AutoFormInfo[]
    contentTypes?: ContentType[]
}
// NON-ENTITY CONTROLLERS

export class AutoFormController {

   private apiUrl: string = '/v1/autoform';

     /*
     * Generated from a .NET type
     * @see {AutoFormController::Get}
     * @url /{type}/{name}
     * @debug - method.ReturnType Api.AutoForms.AutoFormInfo
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.AutoForms.AutoFormInfo, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     get = (type?: string, name?: string): Promise<AutoFormInfo> => {
        return getJson<AutoFormInfo>(this.apiUrl + '/' + type +'/' + name +'', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutoFormController::AllContentForms}
     * @url /all
     * @debug - method.ReturnType Api.AutoForms.AutoFormStructure
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.AutoForms.AutoFormStructure, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     allContentForms = (): Promise<AutoFormStructure> => {
        return getJson<AutoFormStructure>(this.apiUrl + '/all', undefined, { method: 'GET' } )
     }


}

export default new AutoFormController();
