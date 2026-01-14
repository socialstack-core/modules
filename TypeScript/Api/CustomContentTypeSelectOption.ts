/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { CustomContentTypeSelectOptionIncludes } from 'Api/Includes';

import { CustomContentTypeField } from 'Api/CustomContentTypeField';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {CustomContentTypeSelectOption} (Api.CustomContentTypes.CustomContentTypeSelectOption)
**/
export type CustomContentTypeSelectOption = VersionedContent<uint> & {
    customContentTypeFieldId: uint
    value: string
    order: uint
    // HasVirtualField() fields (2 in total)
    customContentTypeField?: CustomContentTypeField;
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class CustomContentTypeSelectOptionApi extends AutoController<CustomContentTypeSelectOption,uint, CustomContentTypeSelectOptionIncludes>{

    constructor(){
        super('/v1/customcontenttypeselectoption', new CustomContentTypeSelectOptionIncludes());
    }

}

export default new CustomContentTypeSelectOptionApi();
