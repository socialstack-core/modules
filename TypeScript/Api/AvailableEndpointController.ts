/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

// TYPES

/**
* This type was generated to reflect {UptimeSince} (Api.AvailableEndpoints.UptimeSince)
**/
export type UptimeSince = {
    utcTicks: long
    utc?: string
}

/**
* This type was generated to reflect {UptimeResult} (Api.AvailableEndpoints.UptimeResult)
**/
export type UptimeResult = {
    since: UptimeSince
}

/**
* This type was generated to reflect {Endpoint} (Api.AvailableEndpoints.Endpoint)
**/
export type Endpoint = {
    url?: string
    summary?: string
    urlFields?: Record<string,Object>
    bodyFields?: Record<string,Object>
    httpMethod?: string
}

/**
* This type was generated to reflect {ContentType} (Api.AvailableEndpoints.ContentType)
**/
export type ContentType = {
    id: int
    name?: string
}

/**
* This type was generated to reflect {ApiStructure} (Api.AvailableEndpoints.ApiStructure)
**/
export type ApiStructure = {
    endpoints?: Endpoint[]
    contentTypes?: ContentType[]
}
// NON-ENTITY CONTROLLERS

export class AvailableEndpointController {

   private apiUrl: string = '/v1';

     /*
     * Generated from a .NET type
     * @see {AvailableEndpointController::Uptime}
     * @url /uptime
     * @debug - method.ReturnType Api.AvailableEndpoints.UptimeResult
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     uptime = (): Promise<UptimeResult> => {
        return getJson<UptimeResult>(this.apiUrl + '/uptime', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AvailableEndpointController::Get}
     * @url /
     * @debug - method.ReturnType Api.AvailableEndpoints.ApiStructure
     * @debug - method.TrueReturnType Api.AvailableEndpoints.ApiStructure
      */
     get = (): Promise<ApiStructure> => {
        return getJson<ApiStructure>(this.apiUrl + '/', undefined, { method: 'GET' } )
     }


}

export default new AvailableEndpointController();
