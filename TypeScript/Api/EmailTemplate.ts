/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { EmailTemplateIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {EmailTemplate} (Api.Emails.EmailTemplate)
**/
export type EmailTemplate = VersionedContent<uint> & {
    key?: string
    name?: string
    subject: string
    bodyJson: string
    primaryContentType?: string
    primaryContentIncludes?: string
    notes?: string
    sendFrom?: string
    emailType: int
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {EmailTestResponse} (Api.Emails.EmailTestResponse)
**/
export type EmailTestResponse = {
    sent: boolean
}

/**
* This type was generated to reflect {EmailTestRequest} (Api.Emails.EmailTestRequest)
**/
export type EmailTestRequest = {
    templateKey?: string
    customData?: string
}
// ENTITY CONTROLLER

export class EmailTemplateApi extends AutoController<EmailTemplate,uint, EmailTemplateIncludes>{

    constructor(){
        super('/v1/emailtemplate', new EmailTemplateIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {EmailTemplateController::TestEmail}
     * @url /test
     * @debug - method.ReturnType Api.Emails.EmailTestResponse
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Emails.EmailTestResponse, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     testEmail = (mailTest?: EmailTestRequest): Promise<EmailTestResponse> => {
        return getJson<EmailTestResponse>(this.apiUrl + '/test', mailTest, { method: 'POST' } )
     }


}

export default new EmailTemplateApi();
