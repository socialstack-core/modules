import {expandIncludes} from 'UI/Functions/WebRequest';
import Text from 'UI/Text';

var TEXT = '#text';

var inlineTypes = [
	TEXT, 'a', 'abbr', 'acronym', 'b', 'bdo', 'big', 'br', 'button', 'cite', 'code', 'dfn', 'em', 'i', 'img', 'input', 'kbd', 'label', 
	'map', 'object', 'output', 'q', 's', 'samp', 'select', 'small', 'span', 'strong', 'sub', 'sup', 'textarea', 'time', 'tt', 'u', 'var'
];

var inlines : Record<string, boolean> = {};
inlineTypes.forEach(type => {
	inlines[type] = true;
});

/**
 * An expanded node within a canvas tree.
 */
export interface CanvasNode {
	/**
	 * The original type name on the node, used to serialise this node if necessary.
	 */
	typeName?: string,

	/**
	 * The host element which is usually a functional component.
	 */
	type?: any,

	/**
	 * The props to pass to the host element.
	 */
	props?: Record<string, any>,

	/**
	 * The parent canvas node if there is one.
	 */
	parent?: CanvasNode,

	/**
	 * Any roots of this canvas node. These are effectively any props which are to be further expanded. 
	 * This allows the props set to be guaranteed to be as-is.
	 */
	roots: Record<string, CanvasNode>,

	/**
	 * The template-relative ID of this canvas node which is often useful for diffing purposes.
	 */
	id?: number,

	/**
	 * The ID of a template if  this canvas node originated from a template.
	 */
	templateId?: number,

	/**
	 * Child content
	 */
	content?: CanvasNode[],

	/**
	 * A text literal string.
	 */
	text?: string,

	/**
	 * Canvas data store links. They are handled when the canvas is rendered. If write is omitted, false is assumed.
	 */
	links?: Record<string, CanvasDataStoreLink>,

	/**
	 * Used as an identifier to spot already expanded canvas nodes.
	 */
	expanded?: boolean,

	/**
	 * Internally used key for React when it is rendering this node.
	 */
	__key?: string,

	/**
	 * Optionally used to force a canvas node to display without fake newlines.
	 */
	isInline?: boolean,

	/**
	 * Generic data store.
	 */
	dataStore?: any
}

export interface CanvasDataStoreLink {
	/**
	 * The name of the canvas data store field to read or write to.
	 */
	field: string,

	/**
	 * True if this link will write to the field. If omitted, false is assumed.
	 * The prop is a function which the component invokes with 1 argument (the value to write).
	 */
	write?: boolean,

	/**
	 * The primary object of the page. Field and write are ignored if this is true.
	 */
	primary?: boolean
}

function readMap(dataMap : any[], ptr : number){
	var host = dataMap.find(dm => dm.id == ptr);
	if(host){
		host.c = expandIncludes(host.c);
		return (host.f && host.c) ? host.c[host.f] : host.c;
	}
}

/**
 * Converts a generic object in to a CanvasNode.
 * @param node
 * @param onContentNode
 * @param dataMap
 * @returns
 */
function convertToNodesFromCanvas(node : any, onContentNode? : (node : CanvasNode) => void, dataMap? : any[]) : CanvasNode | null {
	if(!node){
		return null;
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
	
	if(dataMap && node.p){
		if(typeof node.p == "number"){
			// If it is a number then the whole node object is actually swapped with the pointed-at data entry.
			node = readMap(dataMap, node.p);
		}else{
			// If it is an object then it is to be resolved as d(ata) entries.
			if(!node.d){
				node.d={};
			}
			
			for(var k in node.p){
				var ptr = node.p[k];
				node.d[k] = readMap(dataMap, ptr);
			}
		}
	}
	
	var result = {} as CanvasNode;
	var type = node.t;
	
	
	if(type){
		if(type.indexOf('/') != -1){
			result.typeName = type;
			result.type = require(type).default;
			var data = node.d || node.data;
			
			if(data){
				result.props = {...data};
			}
			
			// Build the roots set.
			var roots : Record<string, CanvasNode> = {};
			
			if(node.r){
				if(Array.isArray(node.r)){
					node.r.forEach((n : any, i : number) => {
						roots[i + ''] = convertToNodesFromCanvas({t: 'span', c: n}, onContentNode, dataMap) as CanvasNode;
					})
				}else{
					for(var key in node.r){
						roots[key] = convertToNodesFromCanvas({ t: 'span', c: node.r[key] }, onContentNode, dataMap) as CanvasNode;
					}
				}
			}
			
			if(node.c){
				// Simplified case for a common scenario of the node just having children only in it.
				// Wrap it in a root node and set it as roots.children.
				roots['children'] = convertToNodesFromCanvas({ t: 'span', c: node.c }, onContentNode, dataMap) as CanvasNode;
			}
			
			for(var k in roots){
				// Indicate it is a root node by removing the span type and add a dom ref/ parent:
				var root = roots[k];
				root.type = undefined;
				root.parent = result;
			}
			
			result.roots = roots;
			
		}else{
			var data = node.d || node.data;
			result.props = {};
			result.type = type;

			// Apply classname to node
			if (data) {
				var className = data.className || data.class;
				result.props.className = className;
			}
			
			if(node.c){
				// Canvas 2
				loadCanvasChildren(node, result, onContentNode, dataMap);
			}
			
		}
	}else if(node.c){
		// a root node
		loadCanvasChildren(node, result, onContentNode, dataMap);
	}

	if (node.l) {
		result.links = node.l;
	}

	if(node.g){
		throw new Error('Unexpanded graph');
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
		result.type = TEXT as React.ElementType;
	}

	// React stability - optional pregenerated key:
	result.__key = node.__key;

	node.isInline = typeof node.type != 'string' || !!inlines[node.type];
	
	if (onContentNode && onContentNode(result) === null){
		return null;
	}
	
	return result;
}
	

function loadCanvasChildren(node: any, result : CanvasNode, onContentNode?: (node: CanvasNode) => void, dataMap? : any[]){
	var c = node.c;
	if(typeof c == 'string'){
		// It has one child which is a text node (no ID or templateID on this).
		var text = {type: TEXT, text: c, parent: result} as CanvasNode;
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
				child = convertToNodesFromCanvas(child, onContentNode, dataMap);
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

/**
* Canvas JSON has multiple conveniences. 
* Expanding it will, for example, resolve the module references.
*/
export function expand(contentNode : any, onContentNode?: (node: CanvasNode) => void){
	if (!contentNode) {
		return null;
	}

	if(contentNode.expanded) {
		return contentNode as CanvasNode;
	}
	
	var dataMap = (contentNode && contentNode.m);
	var res = convertToNodesFromCanvas(contentNode, onContentNode, dataMap);

	if (res) {
		res.expanded = true;

		// It's the root node so it has a data store too.
		// cds (canvas data store) can be initialised by the server but isn't at the moment.
		// The initialisation process is an eventual typescript.net replacement for the graph system.
		res.dataStore = contentNode.cds || {};
	}

	return res;
}