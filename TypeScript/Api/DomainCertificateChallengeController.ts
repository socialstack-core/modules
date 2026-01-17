/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

// TYPES

/**
* This type was generated to reflect {FileContent} (Api.Startup.Routing.FileContent)
**/
export type FileContent = {
    mimeType?: string
    rawBytes?: byte[]
    lastModifiedUtc?: string
    etag?: string
    isCompressed: boolean
    fileName?: string
}
// NON-ENTITY CONTROLLERS

export class DomainCertificateChallengeController {

   private apiUrl: string = '/.well-known/acme-challenge';

     /*
     * Generated from a .NET type
     * @see {DomainCertificateChallengeController::CatchAll}
     * @url /{token}
     * @debug - method.ReturnType Api.Startup.Routing.FileContent
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.Routing.FileContent, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     catchAll = (token?: string): Promise<string> => {
        return getText(this.apiUrl + '/' + token +'', undefined, { method: 'GET' } )
     }


}

export default new DomainCertificateChallengeController();
