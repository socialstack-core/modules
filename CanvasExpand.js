import Text from 'UI/Text'; 
import webRequest from 'UI/Functions/WebRequest';

/*
* Canvas JSON has multiple conveniences and automatically remapped fields.
* "Expanding" a canvas structure essentially does that mapping process.
*/
function expand(contentNode, onChange, onContentNode, blockRequest){
	if(contentNode.expanded){
		return contentNode;
	}
	
	// We recurse the tree via each 'content' array, expanding each node as we go.
	// The goal is to eliminate all convenience/ shorthand JSON such that e.g:
	// {"canvas": 1}  -> {"module": "Canvas", data: {"url": "canvas/1"}}
	// We'll also send off the requests for any of those url's to load.
	
	if(Array.isArray(contentNode)){
		return contentNode.map(e => expand(e, onChange, onContentNode, blockRequest));
	}
	
	if(typeof contentNode != 'object'){
		// String or number
		return {
			module: Text,
			moduleName: 'UI/Text',
			content: contentNode,
			expanded: true
		};
	}
	
	// - Add a filter function here, so other modules can also expand canvas content too.
	// contentNode = _some_global_filter_technique_(contentNode);
	
	// Canvas filter:
	if(contentNode.canvas){
		contentNode.module = "UI/Canvas";
		contentNode.data = {url: "canvas/" + contentNode.canvas};
	}
	
	// data filter:
	if(contentNode.data && typeof contentNode.data != 'object'){
		// Mapped to one prop just called data:
		contentNode.data = {
			data: contentNode.data
		};
	}
	
	// Content array load:
	if(contentNode.data && contentNode.data.items && !blockRequest){
		// Expand the items description.
		// It's either an array of things (which is just as-is), or if it's an object,
		// then it's describing where we can get the things from.
		if(!Array.isArray(contentNode.data.items)){
			
			// It's describing one or more data sources.
			
			/* items: {
				
				sources: [
					{
						type: 'blog',
						filter: OPTIONAL_LOOP_FILTER,
						map: {
							'optional': 'field map which remaps the provided objects to fields the target module wants'
						}
					},
					[
						'as-is arrays supported here too'
					]
				]
				
			}
			
			OR
			
			items: {
				type: 'blog',
				filter: OPTIONAL_LOOP_FILTER
			}
			
			*/
			
			var itms = contentNode.data.items;
			contentNode.data.items = [];
			
			var sources = itms.sources;
			
			if(!sources){
				// Almost always only one source, but just in case people want to mix content, you can provide multiple.
				sources = [
					{
						type: itms.type,
						filter: itms.filter,
						map: itms.map
					}
				];
			}
			
			sources.map(src => {
				
				if(Array.isArray(src)){
					// Can also mix in regular arrays.
					contentNode.data.items = contentNode.data.items.concat(src);
					return;
				}
				
				return webRequest(src.type + '/list', src.filter).then(result => {
					var set = result.json.results;
					
					if(src.map){
						for(var i=0;i<set.length;i++){
							set[i] = remap(set[i], src.map);
						}
					}
					
					contentNode.data.items = contentNode.data.items.concat(set);
					onChange && onChange();
				});
				
			});
			
		}
	}
	
	// URL load filter (order matters - this last):
	if(contentNode.data && contentNode.data.url && !blockRequest){
		// Expand the data object.
		
		var url = contentNode.data.url;
		
		webRequest(url).then(obj => {
			contentNode.data = obj.json;
			onChange && onChange();
		});
	}
	
	if(contentNode.content){
		contentNode.content = expand(contentNode.content, onChange, onContentNode, blockRequest);
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
		
		var module = require(mdRef);
		
		if(module){
			// Use the mapped module. It could be imported JSON, 
			// in which case we've got a regular JS object which should be passed to a canvas.
			
			if(module.default){
				contentNode.module = module.default;
			}else{
				contentNode.module = Canvas;
				contentNode.data = {
					data: module
				};
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
		if(contentNode.data){
			contentNode.module = Canvas;
		}else if(Array.isArray(contentNode.content)){
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

module.exports = expand;