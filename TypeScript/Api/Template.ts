/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { TemplateIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Template} (Api.Templates.Template)
**/
export type Template = VersionedContent<uint> & {
    key?: string
    title?: string
    description?: string
    templateType: uint
    moduleGroups?: string
    bodyJson: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class TemplateApi extends AutoController<Template,uint, TemplateIncludes>{

    constructor(){
        super('/v1/template', new TemplateIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {TemplateController::GetByKey}
     * @url /by-key/{key}
     * @debug - method.ReturnType Api.Templates.Template
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Templates.Template, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     getByKey = (key?: string, includes?: ApiIncludes[]): Promise<Template> => {
        return getOne<Template>(this.apiUrl + '/by-key/' + key  + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'GET' } )
     }


}

export default new TemplateApi();
