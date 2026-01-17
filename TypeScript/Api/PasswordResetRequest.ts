/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { PasswordResetRequestIncludes } from 'Api/Includes';

// TYPES

/**
* This type was generated to reflect {PasswordResetRequest} (Api.PasswordResetRequests.PasswordResetRequest)
**/
export type PasswordResetRequest = Content<uint> & {
    token?: string
    isUsed: boolean
    email?: string
    emailRecoveryKey?: string
}

/**
* This type was generated to reflect {NewPassword} (Api.PasswordResetRequests.NewPassword)
**/
export type NewPassword = {
    password?: string
}

/**
* This type was generated to reflect {ResetToken} (Api.PasswordResetRequests.ResetToken)
**/
export type ResetToken = {
    token?: string
    url?: string
}
// ENTITY CONTROLLER

export class PasswordResetRequestApi extends AutoController<PasswordResetRequest,uint, PasswordResetRequestIncludes>{

    constructor(){
        super('/v1/passwordresetrequest', new PasswordResetRequestIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {PasswordResetRequestController::CheckTokenExists}
     * @url /token/{token}
     * @debug - method.ReturnType System.Object
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Object, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     checkTokenExists = (token?: string): Promise<Object> => {
        return getJson<Object>(this.apiUrl + '/token/' + token +'', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {PasswordResetRequestController::LoginWithToken}
     * @url /login/{token}
     * @debug - method.ReturnType Api.Contexts.Context
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Contexts.Context, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     loginWithToken = (setSession: (s: SessionResponse) => Session, token?: string, newPassword?: NewPassword): Promise<Session> => {
        return getJson<SessionResponse>(this.apiUrl + '/login/' + token +'', newPassword, { method: 'POST' } )
            .then(setSession)
     }


     /*
     * Generated from a .NET type
     * @see {PasswordResetRequestController::Generate}
     * @url /{id}/generate
     * @debug - method.ReturnType Api.PasswordResetRequests.ResetToken
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.PasswordResetRequests.ResetToken, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     generate = (id: uint): Promise<ResetToken> => {
        return getJson<ResetToken>(this.apiUrl + '/' + id +'/generate', undefined, { method: 'GET' } )
     }


}

export default new PasswordResetRequestApi();
