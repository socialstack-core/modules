/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// IMPORTS

// TYPES

/**
* This type was generated to reflect {Automation} (Api.Automations.Automation)
**/
export type Automation = {
    lastTrigger?: (Date | string | number | undefined)
    name?: string
    description?: string
    cronDescription?: string
    cron?: string
}

/**
* This type was generated to reflect {AutomationStructure} (Api.Automations.AutomationStructure)
**/
export type AutomationStructure = {
    results?: Automation[]
}
// NON-ENTITY CONTROLLERS

export class AutomationController {

   private apiUrl: string = '/v1/automation';

     /*
     * Generated from a .NET type
     * @see {AutomationController::Get}
     * @url /list
     * @debug - method.ReturnType Api.Automations.AutomationStructure
     * @debug - method.TrueReturnType Api.Automations.AutomationStructure
      */
     get = (): Promise<AutomationStructure> => {
        return getJson<AutomationStructure>(this.apiUrl + '/list', undefined, { method: 'GET' } )
     }


     /*
     * Generated from a .NET type
     * @see {AutomationController::Execute}
     * @url /{name}/run
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     execute = (name?: string): Promise<string> => {
        return getText(this.apiUrl + '/' + name +'/run', undefined, { method: 'GET' } )
     }


}

export default new AutomationController();
