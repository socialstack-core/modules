/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { UploadIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Upload} (Api.Uploader.Upload)
**/
export type Upload = VersionedContent<uint> & {
    ref?: string
    originalName?: string
    fileType?: string
    variants?: string
    blurhash?: string
    width?: (int | undefined)
    height?: (int | undefined)
    focalX?: (int | undefined)
    focalY?: (int | undefined)
    alt?: string
    author?: string
    usageCount?: (int | undefined)
    isImage: boolean
    isPrivate: boolean
    isVideo: boolean
    isAudio: boolean
    transcodeState: int
    coverImageRef?: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}

/**
* This type was generated to reflect {MediaRef} (Api.Uploader.MediaRef)
**/
export type MediaRef = {
    type?: string
    id: uint
    name?: string
    description?: string
    field?: string
    url?: string
    existingRef?: string
    updatedRef?: string
    status?: string
    localeId: uint
}
// ENTITY CONTROLLER

export class UploadApi extends AutoController<Upload,uint, UploadIncludes>{

    constructor(){
        super('/v1/upload', new UploadIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {UploadController::Upload}
     * @url /create
     * @debug - method.ReturnType Api.Uploader.Upload
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[Api.Uploader.Upload, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
      */
     upload = (includes?: ApiIncludes[]): Promise<Upload> => {
        return getOne<Upload>(this.apiUrl + '/create' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', undefined, { method: 'PUT' } )
     }


     /*
     * Generated from a .NET type
     * @see {UploadController::TranscodedTar}
     * @url /transcoded/{id}
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     transcodedTar = (id: uint, token?: string): Promise<string> => {
        return getText(this.apiUrl + '/transcoded/' + id +'?token=' + token + '', undefined, { method: 'PUT' } )
     }


     /*
     * Generated from a .NET type
     * @see {UploadController::Active}
     * @url /active
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.Uploader.Upload, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.Uploader.Upload, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     active = (): Promise<Upload[]> => {
        return getJson<Upload[]>(this.apiUrl + '/active', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {UploadController::ActivePost}
     * @url /active
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     activePost = (): Promise<string> => {
        return getText(this.apiUrl + '/active', undefined, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {UploadController::FileConsistency}
     * @url /file-consistency
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     fileConsistency = (regenBefore?: string, idRange?: string): Promise<string> => {
        return getText(this.apiUrl + '/file-consistency?regenBefore=' + regenBefore + '&idRange=' + idRange + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {UploadController::Replace}
     * @url /replace
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.Uploader.MediaRef, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.Uploader.MediaRef, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     replace = (sourceRef?: string, targetRef?: string): Promise<MediaRef[]> => {
        return getJson<MediaRef[]>(this.apiUrl + '/replace?sourceRef=' + sourceRef + '&targetRef=' + targetRef + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {UploadController::UpdateAlts}
     * @url /update-alts
     * @debug - method.ReturnType System.Void
     * @debug - method.TrueReturnType System.Void
      */
     updateAlts = (): Promise<string> => {
        return getText(this.apiUrl + '/update-alts', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {UploadController::UpdateRefs}
     * @url /update-refs
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.Uploader.MediaRef, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.Uploader.MediaRef, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     updateRefs = (update: boolean): Promise<MediaRef[]> => {
        return getJson<MediaRef[]>(this.apiUrl + '/update-refs?update=' + update + '', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {UploadController::Preview}
     * @url /replace/preview
     * @debug - method.ReturnType System.Collections.Generic.List`1[[Api.Uploader.MediaRef, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Collections.Generic.List`1[[Api.Uploader.MediaRef, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     preview = (uploadRef?: string): Promise<MediaRef[]> => {
        return getJson<MediaRef[]>(this.apiUrl + '/replace/preview?uploadRef=' + uploadRef + '', undefined, { method: 'GET' } )
     }


}

export default new UploadApi();
