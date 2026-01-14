/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getOne, getJson, getText } from 'UI/Functions/WebRequest';

import { ApiIncludes } from 'Api/Includes';
// IMPORTS

import { Content, UserCreatedContent, VersionedContent, AutoController } from 'Api/Content';

import { CounterIncludes } from 'Api/Includes';

// TYPES

/**
* This type was generated to reflect {Counter} (Api.Counters.Counter)
**/
export type Counter = Content<uint> & {
    _id?: string
    sequence: long
}
// ENTITY CONTROLLER

export class CounterApi extends AutoController<Counter,uint, CounterIncludes>{

    constructor(){
        super('/v1/counter', new CounterIncludes());
    }

}

export default new CounterApi();
