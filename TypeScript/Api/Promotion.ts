/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { PromotionIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Promotion} (Api.Payments.Promotion)
**/
export type Promotion = VersionedContent<uint> & {
    name?: string
    bodyJson: string
    featureRef?: string
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class PromotionApi extends AutoController<Promotion,uint, PromotionIncludes>{

    constructor(){
        super('/v1/promotion', new PromotionIncludes());
    }

}

export default new PromotionApi();
