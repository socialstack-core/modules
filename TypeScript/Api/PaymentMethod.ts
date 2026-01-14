/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { PaymentMethodIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {BrowserInfo} (Api.Payments.BrowserInfo)
**/
export type BrowserInfo = {
    browserJavascriptEnabled: boolean
    browserUserAgent?: string
    browserLanguage?: string
    browserJavaEnabled: boolean
    browserColorDepth?: string
    browserScreenHeight?: string
    browserScreenWidth?: string
    browserTZ?: string
}

/**
* This type was generated to reflect {PaymentMethod} (Api.Payments.PaymentMethod)
**/
export type PaymentMethod = VersionedContent<uint> & {
    browserInfo?: BrowserInfo
    paymentGatewayId: uint
    issuer?: string
    expiryUtc: Date | string | number
    lastUsedUtc: Date | string | number
    paymentMethodTypeId: uint
    name?: string
    oneMonthExpiryNotice: boolean
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class PaymentMethodApi extends AutoController<PaymentMethod,uint, PaymentMethodIncludes>{

    constructor(){
        super('/v1/paymentmethod', new PaymentMethodIncludes());
    }

}

export default new PaymentMethodApi();
