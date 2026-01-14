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

export class HtmlController {

   private apiUrl: string = '/';

     /*
     * Generated from a .NET type
     * @see {HtmlController::GetRteConfigPage}
     * @url /pack/rte.html
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     getRteConfigPage = (): Promise<string> => {
        return getText(this.apiUrl + '/pack/rte.html', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {HtmlController::Robots}
     * @url /robots.txt
     * @debug - method.ReturnType Api.Startup.Routing.FileContent
     * @debug - method.TrueReturnType Api.Startup.Routing.FileContent
      */
     robots = (): Promise<string> => {
        return getText(this.apiUrl + '/robots.txt', undefined, { method: 'ANY' } )
     }


}

export default new HtmlController();
