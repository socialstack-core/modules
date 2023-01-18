import webRequest from 'UI/Functions/WebRequest';
import Graph from 'UI/Functions/GraphRuntime/Graph';
import Text from 'UI/Text';

var inlineTypes = [
	TEXT, 'a', 'abbr', 'acronym', 'b', 'bdo', 'big', 'br', 'button', 'cite', 'code', 'dfn', 'em', 'i', 'img', 'input', 'kbd', 'label', 
	'map', 'object', 'output', 'q', 's', 'samp', 'select', 'small', 'span', 'strong', 'sub', 'sup', 'textarea', 'time', 'tt', 'u', 'var'
];

var inlines={};
inlineTypes.forEach(type => {
	inlines[type] = 1;
});

var TEXT = '#text';

function isCanvas2(node){
	if(Array.isArray(node)){
		for(var i=0;i<node.length;i++){
			var n = node[i];
			if(n == null){
				continue;
			}
			if(isCanvas2(n)){
				return true;
			}
		}
		
		return false;
	}
	
	if(!node){
		return false;
	}
	
	return (node.c || node.r || node.t || node.g);
}

function convertToNodesFromCanvas(node, onContentNode){
	if(!node){
		return;
	}
	
	if(Array.isArray(node)){
		// Remove any nulls in there.
		node = node.filter(n => n);
		
		if(node.length == 1){
			node = node[0];
		}else{
			node = {content: node};
		}
	}
	
	var result = {};
	var type = node.t;
	
	if(type){
		if(type.indexOf('/') != -1){
			result.typeName = type;
			result.type = require(type).default;
			
			// Only custom nodes can have data:
			result.props = result.propTypes = node.d || node.data;
			
			// Build the roots set.
			var roots = {};
			
			if(node.r){
				if(Array.isArray(node.r)){
					node.r.forEach((n, i) => {
						roots[i + ''] = convertToNodesFromCanvas({t: 'span', c: n}, onContentNode);
					})
				}else{
					for(var key in node.r){
						roots[key] = convertToNodesFromCanvas({t: 'span', c: node.r[key]}, onContentNode);
					}
				}
			}
			
			if(node.c){
				// Simplified case for a common scenario of the node just having children only in it.
				// Wrap it in a root node and set it as roots.children.
				roots.children = convertToNodesFromCanvas({t: 'span', c: node.c}, onContentNode);
			}
			
			for(var k in roots){
				// Indicate it is a root node by removing the span type and add a dom ref/ parent:
				var root = roots[k];
				root.type = null;
				root.parent = result;
			}
			
			result.roots = roots;
			
		}else{
			result.type = type;
			
			if(node.c){
				// Canvas 2
				loadCanvasChildren(node, result, onContentNode);
			}
			
		}
	}else if(node.c){
		// a root node
		loadCanvasChildren(node, result, onContentNode);
	}
	
	if(node.g){
		result.graph = new Graph(node.g);
	}
	
	if(node.i){
		result.id = node.i;
	}
	
	if(node.ti){
		result.templateId = node.ti;
	}
	
	if(node.s){
		// String (text node).
		result.text = node.s;
		result.type = TEXT;
	}
	
	node.isInline = typeof node.type != 'string' || !!inlines[node.type];
	
	if(onContentNode){
		if(onContentNode(result) === null){
			return null;
		}
	}
	
	return result;
}
	

function loadCanvasChildren(node, result, onContentNode){
	var c = node.c;
	if(typeof c == 'string'){
		// It has one child which is a text node (no ID or templateID on this).
		var text = {type: TEXT, text: c, parent: result};
		result.content = [text];
	}else{
		if(!Array.isArray(c)){
			// One child
			c = [c];
		}
		
		var content = [];
	
		for(var i=0;i<c.length;i++){
			var child = c[i];
			if(!child){
				continue;
			}
			if(typeof child == 'string'){
				//  (no ID or templateID on this)
				child = {type: TEXT, text: child, parent: result};
			}else{
				child = convertToNodesFromCanvas(child, onContentNode);
				if(!child){
					continue;
				}
				
				child.parent = result;
			}
			content.push(child);
		}
		
		result.content = content;
	}
}
	

/*
* Canvas JSON has multiple conveniences - expanding it will, for example, resolve the module references.
*/
export function expand(contentNode, onContentNode){
	if(!contentNode || contentNode.expanded){
		return contentNode;
	}
	
	if(isCanvas2(contentNode)){
		var res = convertToNodesFromCanvas(contentNode, onContentNode);
		res.expanded = true;
		res.c2 = true;
		return res;
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