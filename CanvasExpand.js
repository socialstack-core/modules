import webRequest from 'UI/Functions/WebRequest';
import Text from 'UI/Text';

/*
* Canvas JSON has multiple conveniences - expanding it will, for example, resolve the module references.
*/
function expand(contentNode, onContentNode){
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
		
		if(mdRef.indexOf('.') == -1){
			// Expansion of e.g. UI/Canvas to UI/Canvas/Canvas.js
			var mdRefParts = mdRef.split('/');
			mdRef = mdRef + '/' + mdRefParts[mdRefParts.length-1] + '.js';
		}
		
		var module = global.getModule(mdRef);
		
		if(module){
			// Use the mapped module. It could be imported JSON, 
			// in which case we've got a regular JS object which should be passed to a canvas.
			
			if(module.default){
				contentNode.module = module.default;
			}else{
				contentNode.module = Canvas;
				contentNode.content = module;
			}
			
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
	
	onContentNode && onContentNode(contentNode);
	
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

function mapTokens(obj, canvas, Canvas){
	var {props} = canvas;
	var result = {};
	
	for(var e in obj) {
		
		var value = obj[e];
		result[e] = value;
		
		if(!value){
			continue;
		}
		
		if(!value.type){
			continue;
		}
		
		var t = value.type;
		switch(t){
			case "token":
				result[e] = props.tokens ? props.tokens[value.name] : null;
			break;
			case "urlToken":
				result[e] = (props.urlTokens || global.pageRouter.state.tokens)[value.name];
			break;
			case "contextToken":
				var tokenParts = (value.name || '').split('.');
				var currentContext = (props.contextTokens || canvas.context.app.state);
				
				for(var i=0;i<tokenParts.length;i++){
					currentContext && (currentContext = currentContext[tokenParts[i]]);
				}
				
				result[e] = currentContext;
			break;
			case "prop":
				result[e] = props[value.name];
			break;
			case "field":
				// Field in the "item" prop. Used by renderers - they're given an item, and this essentially maps
				// from that items field to a prop of the target component.
				result[e] = props.item ? props.item[value.name] : null;
			break;
			case "endpoint":
				// Loads from an endpoint.
				result[e] = null;
				
				if(value.__result){
					result[e] = value.__result;
				}else if(!value.__loading){
					value.__loading = webRequest(value.url).then(obj => {
						value.__result = obj.json;
						canvas.onChange();
					});
				}
			break;
			case "set":
			
				// An array of items from one or more endpoints.
				result[e] = mapSetValue(value, canvas, Canvas);
				
			break;
		}
	}
	
	result.__canvas = canvas;
	return result;
}

function mapSetValue(value, canvas, Canvas){
	var cached = __vCache.get(value);
	
	if(cached && cached.contentType == value.contentType){
		return cached.result;
	}
	
	// It's either an array of things (which is just as-is), or if it's an object,
	// then it's describing where we can get the things from.
	if(Array.isArray(value.items)){
		/*
		{
			type: 'set',
			items: [1,2,3,4..]								
		}
		*/
		
		// Just a simple set of items here.
		var result = value.items;
		result.renderer = (props) => <Canvas {...props} children={value.renderer} />;
		__vCache.set(value, {result, contentType: value.contentType});
		return result;
	}
	
	// It's describing one or more data sources.
	
	var sources = [];
	
	if(value.contentType){
		sources.push({
			contentType: value.contentType,
			filter: value.filter,
			map: value.map
		});
	}else if(value.source){
		sources.push(value.source);
	}else if(value.sources){
		sources = value.sources;
	}
	
	/* {
		type: 'set',
		sources: [
			{
				contentType: 'blog',
				filter: OPTIONAL_LOOP_FILTER,
				map: {
					'optional': 'advanced field map which remaps the provided objects to fields the target wants'
				}
			},
			[
				'as-is arrays supported here too'
			]
		]
		
	}
	
	OR
	
	{
		type: 'set',
		contentType: 'blog',
		filter: OPTIONAL_LOOP_FILTER
	}
	*/
	
	var result = [];
	
	sources.map(src => {
		
		if(Array.isArray(src)){
			// Can also mix in regular arrays.
			result = result.concat(src);
			return;
		}
		
		webRequest(src.contentType + '/list', src.filter).then(r => {
			var set = r.json.results;
			
			if(src.map){
				for(var i=0;i<set.length;i++){
					set[i] = remap(set[i], src.map);
				}
			}
			
			result = result.concat(set);
			__vCache.set(value, {result, contentType: value.contentType});
			result.renderer = (props) => <Canvas {...props} children={value.renderer} />;
			canvas.onChange();
		});
		
	});
	
	__vCache.set(value, {result, contentType: value.contentType});
	result.renderer = (props) => <Canvas {...props} children={value.renderer} />;
	return result;
}

var __vCache = new Map();

export {expand, mapTokens };