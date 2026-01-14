/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { PriceIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Price} (Api.Payments.Price)
**/
export type Price = VersionedContent<uint> & {
    amount: uint
    minimumQuantity: uint
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class PriceApi extends AutoController<Price,uint, PriceIncludes>{

    constructor(){
        super('/v1/price', new PriceIncludes());
    }

}

export default new PriceApi();
