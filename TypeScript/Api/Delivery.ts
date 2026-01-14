/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { DeliveryIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {Delivery} (Api.Payments.Delivery)
**/
export type Delivery = VersionedContent<uint> & {
    expectedSlotUtc: Date | string | number
    timeWindowLength: uint
    actualUtc?: (Date | string | number | undefined)
    deliveryName?: string
    deliveryNotes?: string
    deliveryCost: uint
    deliveryCostLessTax: uint
    totalCost: uint
    totalCostLessTax: uint
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class DeliveryApi extends AutoController<Delivery,uint, DeliveryIncludes>{

    constructor(){
        super('/v1/delivery', new DeliveryIncludes());
    }

}

export default new DeliveryApi();
