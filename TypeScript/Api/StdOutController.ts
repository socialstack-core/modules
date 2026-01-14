/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

// TYPES

/**
* This type was generated to reflect {PublicMessage} (Api.Startup.PublicMessage)
**/
export type PublicMessage = {
    message?: string
    code?: string
}

/**
* This type was generated to reflect {ServerIdentification} (Api.Startup.ServerIdentification)
**/
export type ServerIdentification = {
    id: uint
}

/**
* This type was generated to reflect {LogFilteringModel} (Api.Startup.LogFilteringModel)
**/
export type LogFilteringModel = {
    newerThan: long
    olderThan: long
    offset: uint
    pageSize: uint
    localOnly: boolean
    tag?: string
    levels?: string[]
    disableReactWarnings: boolean
    queryFilter?: string
    exceptionsOnly: boolean
    disableTypeScriptInfo: boolean
}

/**
* This type was generated to reflect {BufferPoolStatus} (Api.Startup.BufferPoolStatus)
**/
export type BufferPoolStatus = {
    writerCount: int
    bufferCount: int
    byteSize: int
}

/**
* This type was generated to reflect {MonitoringExecModel} (Api.Startup.MonitoringExecModel)
**/
export type MonitoringExecModel = {
    command?: string
}

/**
* This type was generated to reflect {WebsocketClientInfo} (Api.Startup.WebsocketClientInfo)
**/
export type WebsocketClientInfo = {
    clients: int
}
// NON-ENTITY CONTROLLERS

export class StdOutController {

   private apiUrl: string = '/v1/monitoring';

     /*
     * Generated from a .NET type
     * @see {StdOutController::V8Clear}
     * @url /v8/clear
     * @debug - method.ReturnType System.Void
     * @debug - method.TrueReturnType System.Void
      */
     v8Clear = (): Promise<string> => {
        return getText(this.apiUrl + '/v8/clear', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::UpdateCerts}
     * @url /certs/update
     * @debug - method.ReturnType Api.Startup.PublicMessage
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Startup.PublicMessage, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     updateCerts = (): Promise<PublicMessage> => {
        return getJson<PublicMessage>(this.apiUrl + '/certs/update', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::UpdateWebserverConfig}
     * @url /webserver/apply
     * @debug - method.ReturnType Api.Startup.PublicMessage
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Startup.PublicMessage, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     updateWebserverConfig = (): Promise<PublicMessage> => {
        return getJson<PublicMessage>(this.apiUrl + '/webserver/apply', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::WhoAmI}
     * @url /whoami
     * @debug - method.ReturnType Api.Startup.ServerIdentification
     * @debug - method.TrueReturnType System.Nullable`1[[Api.Startup.ServerIdentification, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     whoAmI = (): Promise<ServerIdentification> => {
        return getJson<ServerIdentification>(this.apiUrl + '/whoami', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::GetLog}
     * @url /log
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     getLog = (filtering?: LogFilteringModel): Promise<string> => {
        return getText(this.apiUrl + '/log', filtering, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::PlainTextBenchmark}
     * @url /helloworld
     * @debug - method.ReturnType Microsoft.AspNetCore.Mvc.IActionResult
     * @debug - method.TrueReturnType Microsoft.AspNetCore.Mvc.IActionResult
      */
     plainTextBenchmark = (): Promise<IActionResult> => {
        return getJson<IActionResult>(this.apiUrl + '/helloworld', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::BufferPoolStatus}
     * @url /bufferpool/status
     * @debug - method.ReturnType Api.Startup.BufferPoolStatus
     * @debug - method.TrueReturnType Api.Startup.BufferPoolStatus
      */
     bufferPoolStatus = (): Promise<BufferPoolStatus> => {
        return getJson<BufferPoolStatus>(this.apiUrl + '/bufferpool/status', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::BufferPoolClear}
     * @url /bufferpool/clear
     * @debug - method.ReturnType System.Void
     * @debug - method.TrueReturnType System.Void
      */
     bufferPoolClear = (): Promise<string> => {
        return getText(this.apiUrl + '/bufferpool/clear', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::GC}
     * @url /gc
     * @debug - method.ReturnType System.Void
     * @debug - method.TrueReturnType System.Void
      */
     gC = (): Promise<string> => {
        return getText(this.apiUrl + '/gc', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::Execute}
     * @url /exec
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     execute = (body?: MonitoringExecModel): Promise<string> => {
        return getText(this.apiUrl + '/exec', body, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::Halt}
     * @url /halt
     * @debug - method.ReturnType System.Void
     * @debug - method.TrueReturnType System.Void
      */
     halt = (): Promise<string> => {
        return getText(this.apiUrl + '/halt', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {StdOutController::GetWsClientCount}
     * @url /clients
     * @debug - method.ReturnType Api.Startup.WebsocketClientInfo
     * @debug - method.TrueReturnType Api.Startup.WebsocketClientInfo
      */
     getWsClientCount = (): Promise<WebsocketClientInfo> => {
        return getJson<WebsocketClientInfo>(this.apiUrl + '/clients', undefined, { method: 'GET' } )
     }


}

export default new StdOutController();
