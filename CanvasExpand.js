import Text from 'UI/Text'; 
import webRequest from 'UI/Functions/WebRequest';


function expand(contentNode, onChange, onContentNode, blockRequest){
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
			content: contentNode
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
	
	return contentNode;
}

module.exports = expand;