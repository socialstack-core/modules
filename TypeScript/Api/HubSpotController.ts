/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

// TYPES

/**
* This type was generated to reflect {HubSpotEntity} (Api.HubSpot.HubSpotEntity)
**/
export type HubSpotEntity = {
    id?: string
    createdAt?: (DateTimeOffset | undefined)
    updatedAt?: (DateTimeOffset | undefined)
    archived: boolean
    properties?: Record<string,JToken>
    associations?: Record<string,JToken>
}

/**
* This type was generated to reflect {HubSpotNextPage} (Api.HubSpot.HubSpotNextPage)
**/
export type HubSpotNextPage = {
    after?: string
    link?: string
}

/**
* This type was generated to reflect {HubSpotPaging} (Api.HubSpot.HubSpotPaging)
**/
export type HubSpotPaging = {
    next?: HubSpotNextPage
}

/**
* This type was generated to reflect {HubSpotListResponse`1} (Api.HubSpot.HubSpotListResponse`1[[Api.HubSpot.HubSpotEntity, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]])
**/
export type HubSpotListResponse<HubSpotEntity> = {
    results?: HubSpotEntity[]
    paging?: HubSpotPaging
}
// NON-ENTITY CONTROLLERS

export class HubSpotController {

   private apiUrl: string = '/v1/hubspot';

     /*
     * Generated from a .NET type
     * @see {HubSpotController::Contacts}
     * @url /contacts
     * @debug - method.ReturnType Api.HubSpot.HubSpotListResponse`1[[Api.HubSpot.HubSpotEntity, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.HubSpot.HubSpotListResponse`1[[Api.HubSpot.HubSpotEntity, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     contacts = (): Promise<HubSpotListResponse<HubSpotEntity>> => {
        return getJson<HubSpotListResponse<HubSpotEntity>>(this.apiUrl + '/contacts', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {HubSpotController::Companies}
     * @url /companies
     * @debug - method.ReturnType Api.HubSpot.HubSpotListResponse`1[[Api.HubSpot.HubSpotEntity, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.HubSpot.HubSpotListResponse`1[[Api.HubSpot.HubSpotEntity, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     companies = (): Promise<HubSpotListResponse<HubSpotEntity>> => {
        return getJson<HubSpotListResponse<HubSpotEntity>>(this.apiUrl + '/companies', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {HubSpotController::ContactByEmail}
     * @url /contact/{email}
     * @debug - method.ReturnType Api.HubSpot.HubSpotEntity
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.HubSpot.HubSpotEntity, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     contactByEmail = (email?: string): Promise<HubSpotEntity> => {
        return getJson<HubSpotEntity>(this.apiUrl + '/contact/' + email +'', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {HubSpotController::CreateContact}
     * @url /contact/create/{userid}
     * @debug - method.ReturnType Api.HubSpot.HubSpotEntity
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.HubSpot.HubSpotEntity, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     createContact = (userId: uint): Promise<HubSpotEntity> => {
        return getJson<HubSpotEntity>(this.apiUrl + '/contact/create/' + userId +'', undefined, { method: 'GET' } )
     }


}

export default new HubSpotController();
