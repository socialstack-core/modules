var __moduleGroups = null;

export function collectModules(){
	
	if(__moduleGroups){
		return __moduleGroups;
	}
	
	// __mm is the superglobal used by socialstack 
	// to hold all available modules.
	var sets = {standard:{}, renderer: {}};
	__moduleGroups={};
	
	for(var modName in __mm){
		// Attempt to get React propTypes.
		var module = require(modName).default;
		
		if(!module){
			continue;
		}
		
		var props = module.propTypes;
		
		if(!props){
			// This module doesn't have a public interface.
			continue;
		}
		
		// modName is e.g. UI/Thing
		
		// Remove the filename, and get the super group:
		var nameParts = modName.split('/');			
		var publicName = nameParts.join('/');
		var name = nameParts.pop();
		
		if(nameParts[0] == 'UI'){
			nameParts.shift();
		}
		
		var group = nameParts.join(' > ');
		var setsToJoin = null;
		
		if(module.rendererPropTypes){
			// Both sets
			setsToJoin = [sets.standard, sets.renderer];
		}else{
			// 1 set
			setsToJoin=[sets[module.moduleSet || 'standard']];
		}
		
		for(var i=0;i<setsToJoin.length;i++){
			var set = setsToJoin[i];
			if(!set[group]){
				group = set[group] = {name: group, modules: []};
			}else{
				group = set[group];
			}
			
			group.modules.push({
				name,
				publicName,
				props: set == sets.renderer && module.rendererPropTypes ? module.rendererPropTypes : props || {},
				moduleClass: module
			});
		}
	}
	
	for(var setName in sets){
		var set = sets[setName];
		
		var moduleGroups = [];
		
		var ui = set[""];
		
		if(ui){
			// UI group first always:
			moduleGroups.push(ui);
			
			if(ui.modules){
				ui.modules.sort((a,b) => (a.name > b.name) ? 1 : ((b.name > a.name) ? -1 : 0));
			}
			
			delete set[""];
		}
		
		for(var gName in set){
			var subSet = set[gName];
			
			// Sort the modules:
			if(subSet.modules){
				subSet.modules.sort((a,b) => (a.name > b.name) ? 1 : ((b.name > a.name) ? -1 : 0));
			}
			
			moduleGroups.push(subSet);
		}
		
		__moduleGroups[setName] = moduleGroups;
	}
	
	return __moduleGroups;
}
