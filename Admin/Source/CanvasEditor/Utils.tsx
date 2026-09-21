import { CodeModuleMeta, CodeModuleType, getArrayElementType, isJsxType } from 'Admin/Functions/GetPropTypes';

export type RootProp = {
	name: string,
	componentGroups?: string[]
};

function findInterface(module: CodeModuleMeta | undefined, allModules: Record<string, CodeModuleMeta> | undefined, instanceName: string): CodeModuleType | undefined {
	const inModule = module?.types?.find(t => (t.name == 'interface' || (t.name == 'type' && !!t.fields)) && t.instanceName == instanceName);

	if (inModule) {
		return inModule;
	}

	if (allModules) {
		for (const k in allModules) {
			const found = allModules[k].types?.find(t => t.name == 'interface' && t.instanceName == instanceName);

			if (found) {
				return found;
			}
		}
	}

	return undefined;
}

/**
 * Recursively walks a prop type together with its current value, collecting the dot
 * path of every leaf which is (or can contain) a React node root. This is what makes
 * array root names dynamic - e.g. an items prop of SomeItem[] where SomeItem has a
 * content: ReactNode field produces items.0.content / items.1.content / ..., and a
 * ReactNode[] prop produces items.0 / items.1 / ...
 */
function collectRootProps(prefix: string, propType: CodeModuleType, dataValue: any, module: CodeModuleMeta | undefined, allModules: Record<string, CodeModuleMeta> | undefined, rootInfo: RootProp[], seen: Set<string>) {
	if (isJsxType(propType)) {
		if (!rootInfo.find(r => r.name == prefix)) {
			rootInfo.push({ name: prefix } as RootProp);
		}
		return;
	}

	const elementType = getArrayElementType(propType);

	if (elementType && Array.isArray(dataValue)) {
		dataValue.forEach((item, index) => {
			collectRootProps(prefix + '.' + index, elementType, item, module, allModules, rootInfo, seen);
		});
		return;
	}

	if (propType.name == 'identifier' && propType.instanceName && !seen.has(propType.instanceName)) {
		seen.add(propType.instanceName);

		const located = findInterface(module, allModules, propType.instanceName);

		if (located?.fields) {
			located.fields.forEach(field => {
				collectRootProps(prefix + '.' + field.name, field.fieldType, dataValue?.[field.name], module, allModules, rootInfo, seen);
			});
		}

		seen.delete(propType.instanceName);
	}
}

/**
 * Gets the list of props which are roots from the given prop type info.
 *
 * As well as top level jsx props, any jsx fields nested within arrays (or ReactNode
 * array items) in the given props are enumerated so their root-content elements can be
 * created and edited. Nested objects and interfaces are followed to arbitrary depth.
 *
 * @param type The module's CodeModuleMeta (must contain propTypes)
 * @param props The current prop values, used to enumerate array lengths.
 * @param allModules All code modules so interfaces defined in other files can be resolved.
 * @returns
 */
export function getRootInfo(type: CodeModuleMeta | undefined, props?: any, allModules?: Record<string, CodeModuleMeta>): RootProp[] {

	if (!type || !type.propTypes)
	{
		return [];
	}

	var {propTypes} = type;

	if(!propTypes){
		return [];
	}

	var rootInfo: RootProp[] = [];
	const seen = new Set<string>();

	for(const name in propTypes){
		const info = propTypes[name];
		collectRootProps(name, info.type, props?.[name], type, allModules, rootInfo, seen);
	}

	return rootInfo;
}

/**
 * Rewrites the data-name attributes of root-content elements owned by the given component
 * after an item has been added to or removed from an array in the prop editor. Because
 * root content is keyed by its dot path (e.g. items.2.content), removing the 2nd item
 * must delete items.1.* roots and shift items.2.* / items.3.* down to items.1.* etc so
 * existing content follows array items to their new positions.
 * @param container The react-component DOM element of the node being edited.
 * @param arrayPath The dot path of the array prop, e.g. "items" or "items.0.options".
 * @param opts
 */
export function rekeyRootsForArray(container: Element, arrayPath: string, opts: { removedAt?: number; insertedAt?: number }) {
	const { removedAt, insertedAt } = opts;
	const prefix = arrayPath + '.';

	// The react-component this container belongs to. Roots inside nested mounted
	// components are owned by those components instead and must not be touched.
	const ownedWrapper = container.closest?.('react-component') ?? container;

	container.querySelectorAll('root-content').forEach(el => {
		const wrapper = (el as HTMLElement).closest('react-component');

		if (wrapper && wrapper !== ownedWrapper) {
			return;
		}

		const rootName = el.getAttribute('data-name');

		if (!rootName || rootName.indexOf(prefix) != 0) {
			return;
		}

		const remainder = rootName.slice(prefix.length);
		const match = /^(\d+)(.*)$/.exec(remainder);

		if (!match) {
			return;
		}

		const index = parseInt(match[1], 10);
		const rest = match[2];

		if (removedAt !== undefined && index == removedAt) {
			el.remove();
			return;
		}

		if (removedAt !== undefined && index > removedAt) {
			el.setAttribute('data-name', prefix + (index - 1) + rest);
			return;
		}

		if (insertedAt !== undefined && index >= insertedAt) {
			el.setAttribute('data-name', prefix + (index + 1) + rest);
		}
	});
}
