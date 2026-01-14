/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { DomainCertificateIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// TYPES

/**
* This type was generated to reflect {DomainCertificate} (Api.CloudHosts.DomainCertificate)
**/
export type DomainCertificate = VersionedContent<uint> & {
    domain?: string
    ready: boolean
    fileKey?: string
    expiryUtc?: (Date | string | number | undefined)
    serverId: uint
    orderUrl?: string
    status: uint
    lastPingUtc: Date | string | number
    // HasVirtualField() fields (1 in total)
    creatorUser?: User;
}
// ENTITY CONTROLLER

export class DomainCertificateApi extends AutoController<DomainCertificate,uint, DomainCertificateIncludes>{

    constructor(){
        super('/v1/domaincertificate', new DomainCertificateIncludes());
    }

}

export default new DomainCertificateApi();
