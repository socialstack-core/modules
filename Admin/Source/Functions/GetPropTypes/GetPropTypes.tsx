
// eslint-disable-next-line no-restricted-imports
import { getJson } from 'UI/Functions/WebRequest';
import templateApi from 'Api/Template';
import autoformApi, {AutoFormField} from 'Api/AutoFormController'

export interface TypeMeta {
    /**
     * The code modules in the meta, indexed by module path.
     */
    codeModules: Record<string, CodeModuleMeta>
}

/**
 * A particular type definition within a code module.
 */
export interface CodeModuleType {
    /**
     * What this type is. 'string', 'function' etc.
     */
    name: string;

    /**
     * If it's a function, this is e.g. "React.FC". 
     * It's the name of this specific instance of the thing.
     */
    instanceName?: string;

    /**
     * True if this is a built in type such as string, boolean etc.
     */
    builtIn?: boolean;

    /**
     * If this is an interface or type (builtIn is false), the set of fields on the type.
     */
    fields?: CodeModuleTypeField[];

    /**
     * If name=variable or it's an export, detail represents the type that the var is being last set to.
     */
    detail?: CodeModuleType;

    /**
     * Generic parameters if this type has any.
     */
    genericParameters?: CodeModuleType[];

    /**
     * If this type extends other types, the set of those types.
     */
    extends?: CodeModuleType[];

    types?: UnionType[];

    /**
     * Literal value if it is a literal.
     */
    value?: string;

    elementType?: string;

    /**
     * JSDoc metadata from the type definition. Keys are tag names (e.g. 'description', 'icon'),
     * values are either a string or array of strings for repeated tags.
     */
    meta?: Record<string, string | string[]>;
}

export type UnionType = {
    name: string, 
    builtIn: boolean,
    value: string
}

/**
 * A field on an interface or type.
 */
export interface CodeModuleTypeField {
    /**
     * The name of this field.
     */
    name: string;

    /**
     * True if field is optional.
     */
    optional: boolean;

    /**
     * The type of this field.
     */
    fieldType: CodeModuleType;

    /**
     * JSDoc metadata from the field definition.
     */
    meta?: Record<string, string | string[]>;
}

export interface CodeModuleMeta {
    /**
     * Added by the loader, this is the full case sensitive name.
     */
    fullName: string;

    /**
     * The raw types in this file. Defines both the exported things and the raw interfaces and type defs.
     */
    types: CodeModuleType[];

    /**
     * The established propType set. Null if this module does not appear to export a react function.
     */
    propTypes?: Record<string, PropTypeMeta>;
}

export interface PropTypeMeta {
    /**
     * The field type.
     */
    type: CodeModuleType;

    /**
     * JSDoc metadata from the field definition.
     */
    meta?: Record<string, string | string[]>;
}

function expandVariable(type: CodeModuleType) {
    if (type.name == 'variable') {
        if (!type.detail) {
            return null;
        }

        return expandVariable(type.detail);
    }

    return type;
}

/**
 * True if the given prop represents a suitable JSX node. Primarily that is React.reactNode and any unions of it.
 * @param prop
 * @param type
 */
export function isJsx(prop: PropTypeMeta) {
    return isJsxType(prop.type);
}

/**
 * True if the given type represents a suitable JSX node. Primarily that is React.ReactNode and any unions of it.
 * @param prop
 * @param type
 */
export function isJsxType(type: CodeModuleType) {
    return containsIdentifier(type, 'React.reactNode') ||
        containsIdentifier(type, 'React.ReactNode') ||
        containsIdentifier(type, 'React.ReactElement') ||
        containsIdentifier(type, 'ReactElement') ||
        containsIdentifier(type, 'ReactNode') ||
        containsIdentifier(type, 'reactNode');
}

export function containsIdentifier(type: CodeModuleType, identifier: string) : boolean {
    if (!type) {
        return false;
    }

    if (type.name == 'union') {
        return !!(type.types?.find(type => containsIdentifier(type, identifier)));
    }

    if (type.name == 'identifier' && type.instanceName == identifier) {
        return true;
    }

    return false;
}

/**
 * If the given proptype is an array, this returns the array element type.
 * @param prop
 * @param module
 */
export function getArrayElementPropType(prop: PropTypeMeta) {
	var propType = prop.type;
	return getArrayElementType(propType);
}

export function getArrayElementType(type: CodeModuleType) {
	if(type.name == 'array'){
		return type.elementType;
	}
	return null;
}

/**
 * If the given proptype is a (or refers to) a union of constants, returns an array of those constants. 
 * Null or undefined are ignored if they are present.
 * Returns null if it contains any non-constant entry.
 * @param prop
 * @param module
 */
export function getConstantUnion(prop: PropTypeMeta, module: CodeModuleMeta, meta?: TypeMeta) {
    var propType = prop.type;
	return getConstantUnionType(propType, module, meta);
}

export function getConstantUnionType(type: CodeModuleType, module: CodeModuleMeta, meta?: TypeMeta){
    if (type.name == 'identifier' && type.instanceName) {
        // Lookup the identifier in the module, and pretend we received that type.
        var localType = getLocalType(type.instanceName, module);

        if (!localType && meta) {
            // Try global lookup if not found locally
            localType = findTypeGlobally(type.instanceName, meta);
        }

        if (!localType) {
            return null;
        }

        type = localType;
    }

    if (type.name == 'union' && type.types) {
        var result : string[] = [];

        for (var i = 0; i < type.types.length; i++) {
            var childType = type.types[i];

            if (isNullOrUndefined(childType)) {
                continue;
            }

            var constant = getConstantValueString(childType);

            if (!constant) {
                return null;
            }

            result.push(constant);
        }

        return result;
    }

    var constant = getConstantValueString(type);

    if (constant) {
        return [constant];
    }

    return null;
}

/**
 * If the prop refers to a content type, the name of the content type is returned - otherwise null.
 * Note that this does not work with identifiers: you must provide resolved identifiers only (which GetPropTypes does automatically anyway).
 * @param type
 */
export function getContentPropType(propType: PropTypeMeta): string | null {
    return getAsContentType(propType.type);
}

/**
 * If this is a content type, the name is returned - otherwise null.
 * Note that this does not work with identifiers: you must provide resolved identifiers only (which GetPropTypes does automatically anyway).
 * @param type
 */
export function getAsContentType(type: CodeModuleType): string | null {
    if (type.name == 'type' || type.name == 'interface') {

        // Must ultimately extend Content or VersionedContent.
        if (isExtensionOf(type, 'Content') || isExtensionOf(type, 'VersionedContent')) {
            return type.instanceName ? type.instanceName : null;
        }
    }

    return null;
}

/**
 * True if the given propType is a FileRef.
 * @param type
 */
export function isRefPropType(propType: PropTypeMeta): boolean {
    return isRefType(propType.type);
}

export function isRefType(type: CodeModuleType): boolean {
    if (type.name == 'identifier' && type.instanceName == 'FileRef') {
        return true;
    }
    return false;
}

export function getTypeName(propType: PropTypeMeta): string | undefined {
    if (!propType.type) {
        return undefined;
    }

    if (propType.type.name == 'identifier') {
        return propType.type.instanceName;
    }

    return undefined;
}

export function isFunctionPropType(propType: PropTypeMeta): boolean {
    return isFunctionType(propType.type);
}

export function isFunctionType(type: CodeModuleType): boolean {
    if(!type){
        return false;
    }
    
    if (type.name === 'function') {
        return true;
    }
    
    if (type.name === 'identifier' && type.instanceName && (type.instanceName.endsWith('EventHandler') || type.instanceName === 'Function' || type.instanceName === 'CallableFunction')) {
        return true;
    }

    return false;
}

export function isNumericPropType(propType: PropTypeMeta): boolean {
    return isNumericType(propType.type);
}

export function isNumericType(type: CodeModuleType): boolean {
    if (
        type.name == 'number' ||
        type.name == 'identifier' && (
            type.instanceName == 'sbyte' ||  type.instanceName == 'byte' || 
            type.instanceName == 'short' || type.instanceName == 'ushort' ||
            type.instanceName == 'int' || type.instanceName == 'uint' ||
            type.instanceName == 'long' || type.instanceName == 'ulong' ||
            type.instanceName == 'float' || type.instanceName == 'double')
   ) {
        return true;
    }

    return false;
}

export function isBooleanPropType(propType: PropTypeMeta): boolean {
    return isBooleanType(propType.type);
}

export function isBooleanType(type: CodeModuleType): boolean {
    if (type.name == 'boolean' || type.name == 'bool') {
        return true;
    }
    return false;
}

/**
 * True if the given type is an extension of the named type.
 */
export function isExtensionOf(type: CodeModuleType, typeName: string) {
    if (type.extends == null) {
        return false;
    }

    for (var i = 0; i < type.extends.length; i++) {
        var extOf = type.extends[i];

        if (extOf.instanceName == typeName) {
            return true;
        }

        if (isExtensionOf(extOf, typeName)) {
            return true;
        }
    }

    return false;
}

export function isNullOrUndefined(type: CodeModuleType) : boolean {
    return type.name == 'null' || type.name == 'undefined';
}

export function getConstantValueString(type: CodeModuleType): string | null {
    if (type.name.startsWith('literal:')) {
        return type.value ? type.value : null;
    }

    return null;
}

/**
 * locates a type by name from the given module, or null if it was not present in it.
 * @param name
 * @param module
 */
export function getLocalType(name: string, module: CodeModuleMeta) {
    return module.types?.find(type => type.instanceName == name);
}

function setupModuleMeta(meta: TypeMeta, moduleName: string, module : CodeModuleMeta) {

    // Get the export called 'default'.
    var defaultExport = module.types?.find(t => t.name == 'export' && t.instanceName == 'default');

    if (!defaultExport || !defaultExport.detail) {
        module.propTypes = undefined;
        return;
    }

    var type = expandVariable(defaultExport.detail);

    if (!type) {
        module.propTypes = undefined;
        return;
    }

    // To collect the proptypes, we're looking for type
    // to be a function called 'FC' or 'React.FC' with 1 arg.
    if (
        !(type.name == 'function' || type.name == 'identifier') ||
        !(type.instanceName == 'FC' || type.instanceName == 'React.FC')
        || !type.genericParameters || !type.genericParameters.length
    ) {
        module.propTypes = undefined;
        return;
    }

    // It can potentially have other types, like React.PropsWithChildren, nested in the generic params.
    var result: Record<string, PropTypeMeta> = {};
    expandPropTypes(meta, module, type, result);
    module.propTypes = result;
}

/**
 * Expands a type via its generic types and also any types it extends.
 * @param type
 */
function expandPropTypes(meta: TypeMeta, module: CodeModuleMeta, type: CodeModuleType, propTypes: Record<string, PropTypeMeta>) {
    if (!type) {
        return;
    }

    if ((type.name == 'function' || type.name == 'identifier') && type.genericParameters?.length) {
        if (type.instanceName == 'React.PropsWithChildren' || type.instanceName == 'PropsWithChildren') {

            // Add children prop:
            propTypes['children'] = {
                type: {
                    name: 'identifier',
                    instanceName: 'React.ReactNode'
                }
            };

        }

        // Expand the next set:
        expandPropTypes(meta, module, type.genericParameters[0], propTypes);
    } else if (type.name == 'identifier' && type.instanceName) {

        // Lookup this type locally first. If it gets no hits, then
        // we'll find it globally instead.
        var interfaceType = module.types?.find(t => (t.name == 'class' || t.name == 'interface') && t.instanceName == type.instanceName);

        if (!interfaceType) {
            interfaceType = findTypeGlobally(type.instanceName, meta);
        }

        if (!interfaceType) {
            return;
        }

        // Read its fields next and add each to the propTypes set.
        expandPropTypes(meta, module, interfaceType, propTypes);

    } else if (type.name == 'class' || type.name == 'interface') {

        // If it extends anything, add those first.
        if (type.extends) {
            for (var i = 0; i < type.extends.length; i++) {
                expandPropTypes(meta, module, type.extends[i], propTypes);
            }
        }

        // Add the explicit fields next.
        if (type.fields) {
            for (var i = 0; i < type.fields.length; i++) {
                var field = type.fields[i];

                var fieldType = field.fieldType;

                if (!fieldType) {
                    console.log("Field has no type: ", field);
                    continue;
                }

                if (fieldType.name == 'identifier' && fieldType.instanceName) {
                    // Locate this type and ref it instead.
                    var fieldTypeObj = module.types?.find(t => (t.name == 'class' || t.name == 'interface' || t.name == 'union') && t.instanceName == fieldType.instanceName);

                    if (!fieldTypeObj) {
                        fieldTypeObj = findTypeGlobally(fieldType.instanceName, meta);
                    }

                    if (fieldTypeObj) {
                        fieldType = fieldTypeObj;
                    }
                }

                propTypes[field.name] = {
                    type: fieldType,
                    meta: field.meta
                } as PropTypeMeta;
            }
        }

    }

}

/**
 * Searches for a type by name globally.
 */
function findTypeGlobally(name: string, meta: TypeMeta) {
    for (var k in meta.codeModules) {
        var module = meta.codeModules[k];
        var interfaceType = module.types.find(t => (t.name == 'class' || t.name == 'interface' || t.name == 'union') && t.instanceName == name);
        if (interfaceType) {
            return interfaceType;
        }
    }

    return undefined;
}

/**
 * Loads the type-meta cache.
 * @returns
 */
function loadCache() {
    if (_cache) {
        return Promise.resolve(_cache);
    }

    if (!_cacheLoader) {
        _cacheLoader = getJson<TypeMeta>('/pack/type-meta.json')
        .then(json => {
            for (var k in json.codeModules) {
                var module = json.codeModules[k];
                setupModuleMeta(json, k, module);
            }
            _cache = json;
            return json;
        });
    }

    return _cacheLoader;
}

let _cacheLoader: Promise<TypeMeta>;
let _cache : TypeMeta | null = null;

/**
 * Loads the prop type data. Generally call this once when e.g. the canvas editor starts up.
 * @returns
 */
const getAll = () => loadCache();

export {
    getAll
}

/**
 * Gets the prop types for a named component from the given cache set, which you get once from waiting for getAll.
 * Returns null if there was no match.
 */
export const getPropTypes = (name: string, cache: TypeMeta): Record<string, PropTypeMeta> | null => {
    var module = cache.codeModules[name];

    if (!module || !module.propTypes) {
        return null;
    }

    return module.propTypes;
};

export type TemplateModule = {
    name:string,
    types: CodeModuleMeta
}

function collectSlots(canvas: any, target: any[]) {
    if (!canvas) {
        return;
    }

    if (Array.isArray(canvas)) {
        canvas.forEach(node => collectSlots(node, target));
        return;
    }

    if (canvas.t == 'Admin/Template/Slot') {
        // Got a slot!
        var data = canvas.d;

        if (!data || !data.name) {
            return; 
        }

        let componentGroups: string[] | undefined;
        if (data.componentGroups) {
            try {
                componentGroups = typeof data.componentGroups === 'string' 
                    ? JSON.parse(data.componentGroups) 
                    : data.componentGroups;
            } catch (e) {
                componentGroups = undefined;
            }
        }

        target.push({
            name: data.name,
            componentGroups
        });
        return;
    }

    if (canvas.r) {
        // Iterate roots
        for (var rootKey in canvas.r) {
            collectSlots(canvas.r[rootKey], target);
        }
    }

    if (canvas.c) {
        collectSlots(canvas.c, target);
    }
}

export const getEditableTemplates = async (): Promise<any> => {
    return templateApi.listAll().then(templateData => {

        // rotate each template in to its known slots as propTypes.
        var loadedTemplates: any = {};

        templateData.results.forEach(template => {

            // Load template json:
            if (!template.bodyJson || !template.key) {
                return;
            }

            try {
                var loadedBody = JSON.parse(template.bodyJson);

                // Collect all its slots:
                var slots : any[] = [];
                collectSlots(loadedBody, slots);

                loadedTemplates[template.key] = {
                    rootInfo: slots
                };

            } catch {
                console.warn(`Template ${template.id} has an invalid body`);
            }
        });

        return loadedTemplates;
    });
};

export const getTemplates = async (): Promise<TemplateModule[]> => {
    return new Promise((res, rej) => {
        
        getAll().then((result) => {

            const { codeModules } = result;

            const resolution: TemplateModule[] = [];

            Object.keys(codeModules)
                    .filter(key => key.startsWith('UI/Templates'))
                    .forEach(key => {
                        resolution.push({
                            name: key,
                            types: codeModules[key]
                        })
                    })
            res(resolution)
        })
        .catch(rej)

    })
} 
export const getEmailTemplates = async (): Promise<TemplateModule[]> => {
    return new Promise((res, rej) => {
        
        getAll().then((result) => {

            const { codeModules } = result;

            const resolution: TemplateModule[] = [];

            Object.keys(codeModules)
                    .filter(key => key.startsWith('Email/Templates'))
                    .forEach(key => {
                        resolution.push({
                            name: key,
                            types: codeModules[key]
                        })
                    })
            res(resolution)
        })
        .catch(rej)

    })
}

export const getEntities = async (): Promise<CodeModuleType[]> => {
	try {
		// Call the API to fetch all available content forms
		const result = await autoformApi.allContentForms();

		// Extract the content types from the result (or default to an empty array)
		const contentTypes = result?.contentTypes ?? [];

		// Filter out any content types that include "Revision" in their name
		const filtered = contentTypes.filter(
			(contentType) => !contentType?.name?.includes("Revision")
		);

		// Map the remaining content types into CodeModuleType objects
		const entities: CodeModuleType[] = filtered.map((contentType) => ({
			name: contentType.name,
			instanceName: contentType.name,
			fields: result?.forms?.find(object => object.contentType === contentType.name)?.fields?.map((apiField: AutoFormField) => {
				return ({
					name: apiField.fieldName
				} as CodeModuleTypeField)
			}) as CodeModuleTypeField[]
		}));

		// Return the transformed list
		return entities;
	} catch (error) {
		// Propagate any error from the API
		throw error;
	}
};
