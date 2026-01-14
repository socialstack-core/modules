/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

// TYPES

/**
* This type was generated to reflect {NewDetails} (Api.UserSensitiveField.NewDetails)
**/
export type NewDetails = {
    email?: string
    password?: string
    emailRecovery?: string
}
// NON-ENTITY CONTROLLERS

export class UserSensitiveFieldController {

   private apiUrl: string = '/v1/userSensitiveField';

     /*
     * Generated from a .NET type
     * @see {UserSensitiveFieldController::LoginWithToken}
     * @url /login/{token}
     * @debug - method.ReturnType Api.Contexts.Context
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Contexts.Context, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     loginWithToken = (setSession: (s: SessionResponse) => Session, token?: string, newDetails?: NewDetails): Promise<Session> => {
        return getJson<SessionResponse>(this.apiUrl + '/login/' + token +'', newDetails, { method: 'POST' } )
            .then(setSession)
     }


}

export default new UserSensitiveFieldController();
