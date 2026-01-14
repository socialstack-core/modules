/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { UserIncludes } from 'Api/Includes';

import { Role } from 'Api/Role';

// TYPES

/**
* This type was generated to reflect {User} (Api.Users.User)
**/
export type User = VersionedContent<uint> & {
    email?: string
    emailOptOutFlags: uint
    hubSpotId?: string
    syncToHubSpot?: (boolean | undefined)
    firstName?: string
    lastName?: string
    fullName?: string
    lastVisitedUtc: Date | string | number
    role: uint
    featureRef?: string
    avatarRef?: string
    username?: string
    localeId?: (uint | undefined)
    // HasVirtualField() fields (2 in total)
    userRole?: Role;
    creatorUser?: User;
}

/**
* This type was generated to reflect {UserPasswordForgot} (Api.Users.UserPasswordForgot)
**/
export type UserPasswordForgot = {
    email?: string
}

/**
* This type was generated to reflect {OptionalPassword} (Api.Users.OptionalPassword)
**/
export type OptionalPassword = {
    password?: string
}

/**
* This type was generated to reflect {UserLogin} (Api.Users.UserLogin)
**/
export type UserLogin = {
    emailOrUsername?: string
    password?: string
}
// ENTITY CONTROLLER

export class UserApi extends AutoController<User,uint, UserIncludes>{

    constructor(){
        super('/v1/user', new UserIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {UserController::ResendVerificationEmail}
     * @url /sendverifyemail
     * @debug - method.ReturnType Api.Contexts.Context
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Contexts.Context, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     resendVerificationEmail = (setSession: (s: SessionResponse) => Session, body?: UserPasswordForgot): Promise<Session> => {
        return getJson<SessionResponse>(this.apiUrl + '/sendverifyemail', body, { method: 'POST' } )
            .then(setSession)
     }


     /*
     * Generated from a .NET type
     * @see {UserController::VerifyUser}
     * @url /verify/{userid}/{token}
     * @debug - method.ReturnType Api.Contexts.Context
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Contexts.Context, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     verifyUser = (setSession: (s: SessionResponse) => Session, userid: uint, token?: string, newPassword?: OptionalPassword): Promise<Session> => {
        return getJson<SessionResponse>(this.apiUrl + '/verify/' + userid +'/' + token +'', newPassword, { method: 'POST' } )
            .then(setSession)
     }


     /*
     * Generated from a .NET type
     * @see {UserController::Self}
     * @url /self
     * @debug - method.ReturnType Api.Contexts.Context
     * @debug - method.TrueReturnType Api.Contexts.Context
      */
     self = (setSession: (s: SessionResponse) => Session): Promise<Session> => {
        return getJson<SessionResponse>(this.apiUrl + '/self', undefined, { method: 'GET' } )
            .then(setSession)
     }


     /*
     * Generated from a .NET type
     * @see {UserController::Logout}
     * @url /logout
     * @debug - method.ReturnType Api.Contexts.Context
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Contexts.Context, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     logout = (setSession: (s: SessionResponse) => Session): Promise<Session> => {
        return getJson<SessionResponse>(this.apiUrl + '/logout', undefined, { method: 'GET' } )
            .then(setSession)
     }


     /*
     * Generated from a .NET type
     * @see {UserController::Login}
     * @url /login
     * @debug - method.ReturnType System.Object
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     login = (body?: UserLogin): Promise<Object> => {
        return getJson<Object>(this.apiUrl + '/login', body, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {UserController::Impersonate}
     * @url /{id}/impersonate
     * @debug - method.ReturnType Api.Contexts.Context
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Contexts.Context, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     impersonate = (setSession: (s: SessionResponse) => Session, id: uint): Promise<Session> => {
        return getJson<SessionResponse>(this.apiUrl + '/' + id +'/impersonate', undefined, { method: 'GET' } )
            .then(setSession)
     }


     /*
     * Generated from a .NET type
     * @see {UserController::Unpersonate}
     * @url /unpersonate
     * @debug - method.ReturnType Api.Contexts.Context
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Contexts.Context, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     unpersonate = (setSession: (s: SessionResponse) => Session): Promise<Session> => {
        return getJson<SessionResponse>(this.apiUrl + '/unpersonate', undefined, { method: 'GET' } )
            .then(setSession)
     }


}

export default new UserApi();
