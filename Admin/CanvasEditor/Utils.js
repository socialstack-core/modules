export function getRootInfo(type){
	var {propTypes} = type;
	
	if(!propTypes){
		return [];
	}
	
	var rootInfo = [];
	
	for(var name in propTypes){
		var info = propTypes[name];
		
		if(info == 'jsx'){
			rootInfo.push({name});
		}else if(info.type && info.type == 'jsx'){
			rootInfo.push({name, defaultValue: info.default});
		}else if(name == 'children' && info){
			rootInfo.push({name});
		}
	}
	
	return rootInfo;
}