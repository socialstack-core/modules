import GraphNode from './GraphNode';

var __nodeTypes = null;


/*
* Collects all available node types.
* Does this via checking all modules for an export which inherits the GraphNode type.
*/
export function getNodeTypes(){
	if(__nodeTypes){
		return __nodeTypes;
	}
	
	var types = [];
	
	for(var modName in __mm){
		// Attempt to get React propTypes.
		var module = require(modName).default;
		
		if(!module){
			continue;
		}
		
		if(!(module.prototype instanceof GraphNode)){
			continue;
		}
		
		types.push({module, publicPath: modName});
	}
	
	return types;
}

/*
* E.g. "niceName" becomes "Nice Name"
*/
export function niceName(label, override) {
	if (override && override.length) {
		return override;
	}

	label = label.replace(/([^A-Z])([A-Z])/g, '$1 $2');
	return label[0].toUpperCase() + label.substring(1);
}