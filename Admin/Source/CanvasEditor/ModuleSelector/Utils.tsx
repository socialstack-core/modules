import { getAll as getAllPropTypes, TypeMeta, CodeModuleMeta } from 'Admin/Functions/GetPropTypes';
import componentGroupApi, {ComponentGroup} from "Api/ComponentGroup";

type ComponentSet = {
	modules: ComponentInfo[],
	directories: ComponentDirectory[],
	directoryLookup: Record<string, ComponentDirectory>
};

type ComponentDirectory = {
	name: string,
	modules: ComponentInfo[],
	path: string[]
};

type ComponentInfo = {
	name: string,
	publicName: string,
	props: CodeModuleMeta,
	directory: ComponentDirectory,
	moduleClass: any /** The react render func, but can be either a class or a function */
};

var cachedComps: ComponentSet | null = null;

export function groupByDirectory(modules: ComponentInfo[]): ComponentDirectory[] {
	var lookup: Record<string, ComponentDirectory> = {};
	var dirs: ComponentDirectory[] = [];

	for (var i = 0; i < modules.length; i++) {

		var module = modules[i];
		var dir = module.directory;
		var dirName = dir.name;
		var lookupDir = lookup[dirName];

		if (!lookupDir) { 
			lookupDir = {
				name: dir.name,
				path: dir.path,
				modules: []
			};
			dirs.push(lookupDir);
			lookup[dirName] = lookupDir;
		}

		lookupDir.modules.push(module);
	}

	return dirs;
}

const cachedComponentGroups: ComponentGroup[] = [];

const loadComponentGroupCache = async (componentGroups: number[]) => {
	if (Array.isArray(componentGroups)) {
		const requiredIds = componentGroups.filter(
			// Identify any IDs that aren't loaded. 
			id => !Boolean(cachedComponentGroups.find(existing => existing.id === id))
		)

		const results = await componentGroupApi.list({
			query: "Id = [?]",
			args: [requiredIds],
			pageIndex: 0 as uint,
			pageSize: 50 as uint
		})

		// lets cache em'
		cachedComponentGroups.push(...results.results);
	}
}

export async function collectModules(componentGroups? : number[]) : Promise<ComponentSet> {
	
	if (componentGroups && componentGroups.length != 0) {
		await loadComponentGroupCache(componentGroups);
	}
	
	if (cachedComps){
		// a component group filter has been applied.
		// do a length check to make sure if the component groups array is empty
		// we skip the filter.
		if (componentGroups && componentGroups.length != 0) {
			// so now, we need to iterate over the object
			// allowing all that are in the component group.

			const allComponents: string[] = cachedComponentGroups.flatMap(group => {
				const parsed = JSON.parse(group.allowedComponents ?? "[]");
				return Array.isArray(parsed) ? parsed : [];
			});
			
			const cacheCopy = {
				modules: [
					...cachedComps.modules.filter(
						(componentInfo: ComponentInfo) => {
							return allComponents.includes(componentInfo.publicName);
						}
					)
				],
				directories: [...cachedComps.directories],
				directoryLookup: {...cachedComps.directoryLookup},
			}
			
			return cacheCopy;
			
		}
		return cachedComps;
	}

	const propTypeCache = await getAllPropTypes();
	
	cachedComps = constructCache(propTypeCache);
	return cachedComps;
}

function constructCache(propTypeCache : TypeMeta) {
	var modules : ComponentInfo[] = [];
	var directoryLookup: Record<string, ComponentDirectory> = {};
	var directories: ComponentDirectory[] = [];

	const lowercaseMap: Record<string, CodeModuleMeta> = {};

	for (var k in propTypeCache.codeModules) {
		const module = propTypeCache.codeModules[k];
		module.fullName = k;
		lowercaseMap[k.toLowerCase()] = module;
	}

	// __mm is the superglobal used by socialstack 
	// to hold all available modules.
	for (var modName in window.__mm) {
		// Attempt to get React propTypes.
		var moduleFunc = require(modName).default;

		if (!moduleFunc) {
			continue;
		}

		// modName is e.g. UI/Thing
		
		var modulePropInfo = lowercaseMap[modName];

		if (!modulePropInfo || !modulePropInfo.propTypes) {
			continue;
		}

		// Remove the filename, and get the super group:
		const fullName = modulePropInfo.fullName
		var nameParts = fullName.split('/');
		var name = nameParts.pop();
		var directoryName = nameParts.join('/');

		if (!name) {
			continue;
		}

		var directory = directoryLookup[directoryName];

		if (!directory) {
			directory = { name: directoryName, modules: [], path: nameParts };
			directories.push(directory);
			directoryLookup[directoryName] = directory;
		}

		var moduleInfo: ComponentInfo = {
			name,
			publicName: fullName,
			directory,
			props: modulePropInfo,
			moduleClass: moduleFunc
		};

		modules.push(moduleInfo);
	}

	// Sort by name
	modules.sort((a, b) => (a.publicName > b.publicName) ? 1 : ((b.publicName > a.publicName) ? -1 : 0));

	// After the sort, such that directory lists are implicitly sorted too.
	modules.forEach(module => module.directory.modules.push(module));

	return {
		modules,
		directories,
		directoryLookup
	};
}