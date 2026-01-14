/**
 * This file was automatically generated. DO NOT EDIT.
 */

import { ApiList, getList, getJson, getText } from 'UI/Functions/WebRequest';

// TYPES
// NON-ENTITY CONTROLLERS

export class DatabaseMigrationController {

   private apiUrl: string = '/v1/migration';

     /*
     * Generated from a .NET type
     * @see {DatabaseMigrationController::MigrateEverything}
     * @url /migrate
     * @debug - method.ReturnType System.Threading.Tasks.ValueTask
     * @debug - method.TrueReturnType System.Threading.Tasks.ValueTask
      */
     migrateEverything = (from?: string, to?: string): Promise<string> => {
        return getText(this.apiUrl + '/migrate?from=' + from + '&to=' + to + '', undefined, { method: 'GET' } )
     }


}

export default new DatabaseMigrationController();
