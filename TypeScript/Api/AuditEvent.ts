/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { AuditEventIncludes } from 'Api/Includes';

import { User } from 'Api/User';

// ENUMS

export enum LogSortDirection {
    ASC=0,
    DESC=1,
}

export enum BsonType {
    EndOfDocument=0,
    Double=1,
    String=2,
    Document=3,
    Array=4,
    Binary=5,
    Undefined=6,
    ObjectId=7,
    Boolean=8,
    DateTime=9,
    Null=10,
    RegularExpression=11,
    JavaScript=13,
    Symbol=14,
    JavaScriptWithScope=15,
    Int32=16,
    Timestamp=17,
    Int64=18,
    Decimal128=19,
    MinKey=255,
    MaxKey=127,
}

export enum BsonBinarySubType {
    Binary=0,
    Function=1,
    OldBinary=2,
    UuidLegacy=3,
    UuidStandard=4,
    MD5=5,
    Encrypted=6,
    Column=7,
    Sensitive=8,
    Vector=9,
    UserDefined=128,
}

// TYPES

/**
* This type was generated to reflect {AuditEvent} (Api.AuditEvents.AuditEvent)
**/
export type AuditEvent = UserCreatedContent<uint> & {
    realUserId: uint
    contentType?: string
    contentId: ulong
    actionType: uint
    // HasVirtualField() fields (2 in total)
    realUser?: User;
    creatorUser?: User;
}

/**
* This type was generated to reflect {LogSortOrder} (Api.AuditEvents.LogSortOrder)
**/
export type LogSortOrder = {
    field?: string
    direction: LogSortDirection
}

/**
* This type was generated to reflect {AuditLogSearchRequest} (Api.AuditEvents.AuditLogSearchRequest)
**/
export type AuditLogSearchRequest = {
    contentTypes?: string[]
    userId?: (uint | undefined)
    startUtc?: (Date | string | number | undefined)
    range?: (int | undefined)
    pageIndex: int
    pageSize: int
    sortOrder: LogSortOrder
}

/**
* This type was generated to reflect {BsonArray} (MongoDB.Bson.BsonArray)
**/
export type BsonArray = BsonValue & {
    bsonType: BsonType
    capacity: int
    count: int
    isReadOnly: boolean
    values?: BsonValue[]
    item?: BsonValue
}

/**
* This type was generated to reflect {BsonBinaryData} (MongoDB.Bson.BsonBinaryData)
**/
export type BsonBinaryData = BsonValue & {
    bsonType: BsonType
    bytes?: byte[]
    subType: BsonBinarySubType
}

/**
* This type was generated to reflect {BsonDateTime} (MongoDB.Bson.BsonDateTime)
**/
export type BsonDateTime = BsonValue & {
    bsonType: BsonType
    isValidDateTime: boolean
    millisecondsSinceEpoch: long
}

/**
* This type was generated to reflect {BsonJavaScript} (MongoDB.Bson.BsonJavaScript)
**/
export type BsonJavaScript = BsonValue & {
    bsonType: BsonType
    code?: string
}

/**
* This type was generated to reflect {BsonJavaScriptWithScope} (MongoDB.Bson.BsonJavaScriptWithScope)
**/
export type BsonJavaScriptWithScope = BsonJavaScript & {
    bsonType: BsonType
    scope?: BsonDocument
}

/**
* This type was generated to reflect {BsonMaxKey} (MongoDB.Bson.BsonMaxKey)
**/
export type BsonMaxKey = BsonValue & {
    bsonType: BsonType
}

/**
* This type was generated to reflect {BsonMinKey} (MongoDB.Bson.BsonMinKey)
**/
export type BsonMinKey = BsonValue & {
    bsonType: BsonType
}

/**
* This type was generated to reflect {BsonNull} (MongoDB.Bson.BsonNull)
**/
export type BsonNull = BsonValue & {
    bsonType: BsonType
}

/**
* This type was generated to reflect {BsonRegularExpression} (MongoDB.Bson.BsonRegularExpression)
**/
export type BsonRegularExpression = BsonValue & {
    bsonType: BsonType
    pattern?: string
    options?: string
}

/**
* This type was generated to reflect {BsonSymbol} (MongoDB.Bson.BsonSymbol)
**/
export type BsonSymbol = BsonValue & {
    bsonType: BsonType
    name?: string
}

/**
* This type was generated to reflect {BsonTimestamp} (MongoDB.Bson.BsonTimestamp)
**/
export type BsonTimestamp = BsonValue & {
    bsonType: BsonType
    value: long
    increment: int
    timestamp: int
}

/**
* This type was generated to reflect {BsonUndefined} (MongoDB.Bson.BsonUndefined)
**/
export type BsonUndefined = BsonValue & {
    bsonType: BsonType
}

/**
* This type was generated to reflect {Decimal128} (MongoDB.Bson.Decimal128)
**/
export type Decimal128 = {
}

/**
* This type was generated to reflect {ObjectId} (MongoDB.Bson.ObjectId)
**/
export type ObjectId = {
    timestamp: int
    creationTime: Date | string | number
}

/**
* This type was generated to reflect {BsonValue} (MongoDB.Bson.BsonValue)
**/
export type BsonValue = {
    asBoolean: boolean
    asBsonArray?: BsonArray
    asBsonBinaryData?: BsonBinaryData
    asBsonDateTime?: BsonDateTime
    asBsonDocument?: BsonDocument
    asBsonJavaScript?: BsonJavaScript
    asBsonJavaScriptWithScope?: BsonJavaScriptWithScope
    asBsonMaxKey?: BsonMaxKey
    asBsonMinKey?: BsonMinKey
    asBsonNull?: BsonNull
    asBsonRegularExpression?: BsonRegularExpression
    asBsonSymbol?: BsonSymbol
    asBsonTimestamp?: BsonTimestamp
    asBsonUndefined?: BsonUndefined
    asBsonValue?: BsonValue
    asByteArray?: byte[]
    asDecimal: double
    asDecimal128: Decimal128
    asDouble: double
    asGuid: Guid
    asInt32: int
    asInt64: long
    asLocalTime: Date | string | number
    asNullableBoolean?: (boolean | undefined)
    asNullableDecimal?: (double | undefined)
    asNullableDecimal128?: (Decimal128 | undefined)
    asNullableDouble?: (double | undefined)
    asNullableGuid?: (Guid | undefined)
    asNullableInt32?: (int | undefined)
    asNullableInt64?: (long | undefined)
    asNullableLocalTime?: (Date | string | number | undefined)
    asNullableObjectId?: (ObjectId | undefined)
    asNullableUniversalTime?: (Date | string | number | undefined)
    asObjectId: ObjectId
    asRegex?: Regex
    asString?: string
    asUniversalTime: Date | string | number
    bsonType: BsonType
    isBoolean: boolean
    isBsonArray: boolean
    isBsonBinaryData: boolean
    isBsonDateTime: boolean
    isBsonDocument: boolean
    isBsonJavaScript: boolean
    isBsonJavaScriptWithScope: boolean
    isBsonMaxKey: boolean
    isBsonMinKey: boolean
    isBsonNull: boolean
    isBsonRegularExpression: boolean
    isBsonSymbol: boolean
    isBsonTimestamp: boolean
    isBsonUndefined: boolean
    isDecimal128: boolean
    isDouble: boolean
    isGuid: boolean
    isInt32: boolean
    isInt64: boolean
    isNumeric: boolean
    isObjectId: boolean
    isString: boolean
    isValidDateTime: boolean
    item?: BsonValue
    item?: BsonValue
}

/**
* This type was generated to reflect {BsonElement} (MongoDB.Bson.BsonElement)
**/
export type BsonElement = {
    name?: string
    value?: BsonValue
}

/**
* This type was generated to reflect {BsonDocument} (MongoDB.Bson.BsonDocument)
**/
export type BsonDocument = BsonValue & {
    allowDuplicateNames: boolean
    bsonType: BsonType
    elementCount: int
    elements?: BsonElement[]
    names?: string[]
    values?: BsonValue[]
    item?: BsonValue
    item?: BsonValue
}

/**
* This type was generated to reflect {AuditEventType} (Api.AuditEvents.AuditEventType)
**/
export type AuditEventType = {
    typeName?: string
    mongoDbCollection?: string
    mongoDbSet?: BsonDocument
}

/**
* This type was generated to reflect {AvailableAuditTypes} (Api.AuditEvents.AvailableAuditTypes)
**/
export type AvailableAuditTypes = {
    results?: AuditEventType[]
}
// ENTITY CONTROLLER

export class AuditEventApi extends AutoController<AuditEvent,uint, AuditEventIncludes>{

    constructor(){
        super('/v1/auditevent', new AuditEventIncludes());
    }

     /*
     * Generated from a .NET type
     * @see {AuditEventController::Detailed}
     * @url /detailed
     * @debug - method.ReturnType Api.Startup.ContentStream`2[[Api.AuditEvents.AuditEvent, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.Startup.ContentStream`2[[Api.AuditEvents.AuditEvent, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.UInt32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     detailed = (request: AuditLogSearchRequest, includes?: ApiIncludes[]): Promise<ApiList<AuditEvent>> => {
        return getList<AuditEvent>(this.apiUrl + '/detailed' + (Array.isArray(includes) ? '?includes=' + includes.join(',') : '') + '', request, { method: 'POST' } )
     }


     /*
     * Generated from a .NET type
     * @see {AuditEventController::AvailableTypes}
     * @url /available-types
     * @debug - method.ReturnType Api.AuditEvents.AvailableAuditTypes
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask`1[[System.Nullable`1[[Api.AuditEvents.AvailableAuditTypes, SocialStack.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
      */
     availableTypes = (): Promise<AvailableAuditTypes> => {
        return getJson<AvailableAuditTypes>(this.apiUrl + '/available-types', undefined, { method: 'GET' } )
     }


}

export default new AuditEventApi();
