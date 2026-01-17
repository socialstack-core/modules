/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

// TYPES

/**
* This type was generated to reflect {UIReloadResult} (Api.CanvasRenderer.UIReloadResult)
**/
export type UIReloadResult = {
    version: long
}

/**
* This type was generated to reflect {StaticFileInfo} (Api.CanvasRenderer.StaticFileInfo)
**/
export type StaticFileInfo = {
    size: long
    modifiedUtc: ulong
    ref?: string
}

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

export class FrontendCodeController {

   private apiUrl: string = '/';

     /*
     * Generated from a .NET type
     * @see {FrontendCodeController::Reload}
     * @url /v1/monitoring/ui-reload
     * @debug - method.ReturnType Api.CanvasRenderer.UIReloadResult
     * @debug - method.TrueReturnType Api.CanvasRenderer.UIReloadResult
      */
     reload = (): Promise<UIReloadResult> => {
        return getJson<UIReloadResult>(this.apiUrl + '/v1/monitoring/ui-reload', undefined, { method: 'ANY' } )
     }


     /*
     * Generated from a .NET type
     * @see {FrontendCodeController::GetStaticFileList}
     * @url /pack/static-assets/list.json
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.CanvasRenderer.StaticFileInfo, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.CanvasRenderer.StaticFileInfo, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getStaticFileList = (): Promise<StaticFileInfo[]> => {
        return getJson<StaticFileInfo[]>(this.apiUrl + '/pack/static-assets/list.json', undefined, { method: 'ANY' } )
     }


     /*
     * Generated from a .NET type
     * @see {FrontendCodeController::GetTypeMeta}
     * @url /pack/type-meta.json
     * @debug - method.ReturnType Api.Startup.Routing.FileContent
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.Routing.FileContent, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getTypeMeta = (): Promise<string> => {
        return getText(this.apiUrl + '/pack/type-meta.json', undefined, { method: 'ANY' } )
     }


     /*
     * Generated from a .NET type
     * @see {FrontendCodeController::GetEmailMainJs}
     * @url /pack/email-static/main.js
     * @debug - method.ReturnType Api.Startup.Routing.FileContent
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.Routing.FileContent, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getEmailMainJs = (localeId: uint): Promise<string> => {
        return getText(this.apiUrl + '/pack/email-static/main.js?localeId=' + localeId + '', undefined, { method: 'ANY' } )
     }


     /*
     * Generated from a .NET type
     * @see {FrontendCodeController::GetGlobalScss}
     * @url /pack/scss/global
     * @debug - method.ReturnType Api.Startup.Routing.FileContent
     * @debug - method.TrueReturnType Api.Startup.Routing.FileContent
      */
     getGlobalScss = (): Promise<string> => {
        return getText(this.apiUrl + '/pack/scss/global', undefined, { method: 'ANY' } )
     }


     /*
     * Generated from a .NET type
     * @see {FrontendCodeController::GetMainJs}
     * @url /pack/main.js
     * @debug - method.ReturnType Api.Startup.Routing.FileContent
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.Routing.FileContent, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getMainJs = (localeId: uint): Promise<string> => {
        return getText(this.apiUrl + '/pack/main.js?localeId=' + localeId + '', undefined, { method: 'ANY' } )
     }


     /*
     * Generated from a .NET type
     * @see {FrontendCodeController::GetAdminMainJs}
     * @url /en-admin/pack/main.js
     * @debug - method.ReturnType Api.Startup.Routing.FileContent
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.Routing.FileContent, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getAdminMainJs = (localeId: uint): Promise<string> => {
        return getText(this.apiUrl + '/en-admin/pack/main.js?localeId=' + localeId + '', undefined, { method: 'ANY' } )
     }


     /*
     * Generated from a .NET type
     * @see {FrontendCodeController::GetMainCss}
     * @url /pack/main.css
     * @debug - method.ReturnType Api.Startup.Routing.FileContent
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.Routing.FileContent, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getMainCss = (): Promise<string> => {
        return getText(this.apiUrl + '/pack/main.css', undefined, { method: 'ANY' } )
     }


     /*
     * Generated from a .NET type
     * @see {FrontendCodeController::GetAdminMainCss}
     * @url /en-admin/pack/main.css
     * @debug - method.ReturnType Api.Startup.Routing.FileContent
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.Routing.FileContent, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     getAdminMainCss = (): Promise<string> => {
        return getText(this.apiUrl + '/en-admin/pack/main.css', undefined, { method: 'ANY' } )
     }


}

export default new FrontendCodeController();
