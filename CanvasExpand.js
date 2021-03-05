import webRequest from 'UI/Functions/WebRequest';
import Text from 'UI/Text';

/*
* Canvas JSON has multiple conveniences - expanding it will, for example, resolve the module references.
*/
export function expand(contentNode, onContentNode){
	if(!contentNode || contentNode.expanded){
		return contentNode;
	}
	
	// We recurse the tree via each 'content' array, expanding each node as we go.
	// The goal is to eliminate all convenience/ shorthand JSON such that e.g:
	// {"canvas": 1}  -> {"module": "Canvas", data: {"url": "canvas/1"}}
	// We'll also send off the requests for any of those url's to load.
	
	if(Array.isArray(contentNode)){
		
		// Filter out any nulls, but then also if there's only 1 entry, return it as-is.
		var filtered = contentNode.filter(e => e != null);
		
		if(filtered.length == 0){
			return null;
		}else if(filtered.length == 1){
			return expand(filtered[0], onContentNode);
		}
		
		return filtered.map(e => expand(e, onContentNode));
	}
	
	// If it has no module but does have content, then it's a text node.
	if(!contentNode.module && contentNode.content !== undefined && !Array.isArray(contentNode.content)){
		return {
			module: Text,
			moduleName: 'UI/Text',
			data: {
				text: contentNode.content
			},
			expanded: true
		};
	}
	
	if(typeof contentNode != 'object'){
		return {
			module: Text,
			moduleName: 'UI/Text',
			data: {
				text: contentNode
			},
			expanded: true
		};
	}
	
	if(contentNode.content !== undefined){
		var expanded = expand(contentNode.content, onContentNode);
		if(!expanded){
			contentNode.content = [];
		}else if(!Array.isArray(expanded)){
			contentNode.content = [expanded];
		}else{
			contentNode.content = expanded;
		}
		
	}else{
		contentNode.content = [];
	}
	
	// Module filter - connect the right module to use.
	if(contentNode.module){
		
		var mdRef = contentNode.module;
		contentNode.moduleName = mdRef;
		var module = global.require(mdRef);
		
		if(module){
			// Use the mapped module. It could be imported JSON, 
			// in which case we've got a regular JS object which should be passed to a canvas.
			contentNode.module = module.default;
			contentNode.useCanvasRender = true;
			
		}else{
			var first = contentNode.module.charAt(0);
			if(first == first.toUpperCase()){
				// Module is missing - prompt dev to install it.
				throw new Error("Attempted to use a UI module called '" + contentNode.module + "' which you don't have installed.");
			}
			
			// It's a HTML element being used directly. Must use Canvas render for the content of these.
			contentNode.useCanvasRender = contentNode.content != null;
			
			if(!Array.isArray(contentNode.content)){
				// Must be an array here.
				contentNode.content = [contentNode.content];
			}
			
			module = contentNode.module;
		}
		
	}else{
		// Use this canvas to render the content unless there is also data.
		if(Array.isArray(contentNode.content)){
			contentNode.useCanvasRender = true;
		}else{
			contentNode = contentNode.content;
		}
	}
	
	if(onContentNode){
		if(onContentNode(contentNode) === null){
			return null;
		}
	}
	
	contentNode.expanded = true;
	return contentNode;
}


/*
item is e.g:

{
	hello: 'world',
	aField: 'aValue',
	anUnmappedField: 'this will not be in the returned object'
} 

map is e.g:

{
	outputField1: 'hello',
	outputField2: 'aField',
}

The return value is the following:

{
	outputField1: 'world',
	outputField2: 'aValue',
}

*/
function remap(item, map){
	if(!item){
		return null;
	}
	
	var result = {};
	
	for(var field in map){
		result[field] = item[map[field]];
	}
	
	return result;
}

export function mapTokens(obj, canvas, Canvas){
	var {props} = canvas;
	var result = {};
	
	for(var e in obj) {
		
		var value = obj[e];
		result[e] = value;
		
		if(!value || !value.type){
			continue;
		}
		
		var t = value.type;
		switch(t){
			case "urlToken":
				var pageRouterState = canvas.context.pageRouter.state;
				var index = pageRouterState.tokenNames.indexOf(value.name);
				result[e] = (index == null || index == -1) ? undefined : pageRouterState.tokens[index];
			break;
			case "field":
				// Field in the "item" prop. Used by renderers - they're given an item, and this essentially maps
				// from that items field to a prop of the target component.
				result[e] = props.item ? props.item[value.name] : null;
			break;
		}
	}
	
	result.__canvas = canvas;
	return result;
}