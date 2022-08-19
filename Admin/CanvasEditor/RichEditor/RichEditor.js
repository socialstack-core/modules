import Alert from 'UI/Alert';
import Token from 'UI/Token';
import Input from 'UI/Input';
import { ErrorCatcher } from 'UI/Canvas';
import getBuildDate from 'UI/Functions/GetBuildDate';
import ModuleSelector from 'Admin/CanvasEditor/ModuleSelector'
import PropEditor from 'Admin/CanvasEditor/PropEditor';
import omit from 'UI/Functions/Omit';

// Connect the input "ontypecanvas" render event:

var inputTypes = global.inputTypes = global.inputTypes || {};

// type="canvas"
inputTypes.ontypecanvas = function(props, _this){
	
	return <RichEditor
		id={props.id || _this.fieldId}
		className={props.className || "form-control"}
		toolbar 
		modules
		groups = {"formatting"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
	
};

// contentType="application/canvas"
inputTypes['application/canvas'] = function(props, _this){
	return <RichEditor
		id={props.id || _this.fieldId}
		className={props.className || "form-control"}
		toolbar 
		modules
		enableAdd
		groups = {"formatting"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
};

var TEXT = '#text';

/*
* Inline elements
*/
var inlineTypes = [
	TEXT, 'a', 'abbr', 'acronym', 'b', 'bdo', 'big', 'br', 'button', 'cite', 'code', 'dfn', 'em', 'i', 'img', 'input', 'kbd', 'label', 
	'map', 'object', 'output', 'q', 's', 'samp', 'select', 'small', 'span', 'strong', 'sub', 'sup', 'textarea', 'time', 'tt', 'u', 'var'
];

var permittedTypes = [
	'a', 'abbr', 'acronym', 'b', 'bdo', 'big', 'br', 'cite', 'code', 'center', 'dfn', 'em', 'i', 'kbd', 
	'output', 'q', 's', 'samp', 'small', 'span', 'strong', 'sub', 'sup', 'time', 'tt', 'u', 'var',
	'address', 'article', 'aside', 'blockquote', 'dd', 'div', 'dl', 'dt', 'footer', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 
	'header', 'hr', 'li', 'main', 'ol', 'p', 'pre', 'section', 'ul'
	
	// img, video, table - are permitted, but get converted to modules.
	// In all of the above, the only one permitted to have an attribute is <a> href.
];

var CANVAS_JSON = 'application/x-canvas';

/*
* Inline elements that can have no child nodes.
*/
var inlineNoContent = {'img': 1, 'br': 1, 'input': 1};

var inlines={};
inlineTypes.forEach(type => {
	inlines[type] = 1;
});

var permitted = {};

permittedTypes.forEach(type => {
	permitted[type] = 1;
});

/*
* Socialstack's RTE. Influenced by the UX of TinyMCE, and the code design of draft.js
*/
export default class RichEditor extends React.Component {
	
	constructor(props){
		super(props);
		this.onKeyDown = this.onKeyDown.bind(this);
		this.onMouseUp = this.onMouseUp.bind(this);
		this.onMouseDown = this.onMouseDown.bind(this);
		this.onContextMenu = this.onContextMenu.bind(this);
		this.onReject = this.onReject.bind(this);
		this.onBlur = this.onBlur.bind(this);
		this.onPaste = this.onPaste.bind(this);
		this.onCopy = this.onCopy.bind(this);
		this.onCut = this.onCut.bind(this);
		this.closeModal = this.closeModal.bind(this);
		this.normalise = this.normalise.bind(this);
		
		var rootRef =  React.createRef();
		
		var node = {
			content: [],
			dom: rootRef
		};
		
		this.state={
			node,
			rootRef,
			active: {}
		};
		
		var value = this.props.value || this.props.defaultValue;
		
		if(value){
			this.loadCanvas(this.props.value || this.props.defaultValue, 1);
		}
		
		// Initial snapshot:
		this.addStateSnapshot();
	}
	
	insertJson(canvasNode){
		if(!canvasNode){
			return;
		}
		
		var toInsert = canvasNode.type ? [canvasNode] : canvasNode.content;
		
		if(!toInsert || !Array.isArray(toInsert) || !toInsert.length){
			return;
		}
		
		var {selection, node} = this.state;
		
		if(!selection){
			// The editor isn't focused at the moment. This content isn't for us.
			return;
		}
		
		if(selection.startOffset != selection.endOffset || selection.startNode != selection.endNode){
			// Overtyping - remove this content.
			this.deleteContent(selection);
		}else{
			// Just split at the selection. This ensures pasted content gets inserted at the correct place inside text.
			this.sliceSelection(selection);
		}
		
		var start = selection.startNode;
		var index = selection.startOffset;
		
		if(start.type == TEXT){
			index = start.parent.content.indexOf(start) + 1;
			start = start.parent;
		}
		
		if(!start.content){
			start.content = toInsert;
			selection.startOffset = selection.endOffset = toInsert.length;
		}else{
			var newContent = start.content.slice(0, index).concat(toInsert);
			start.content = newContent.concat(start.content.slice(index));
			selection.startOffset = selection.endOffset = newContent.length;
		}
		
		selection.startNode = selection.endNode = start;
		
		// Update parent:
		toInsert.forEach(n => n.parent = start);
		
		// Tidy the tree up after inserting html:
		this.normalise();
	}
	
	insertHtml(html){
		var {selection, node} = this.state;
		
		if(!selection){
			// The editor isn't focused at the moment. This content isn't for us.
			return;
		}
		
		if(selection.startOffset != selection.endOffset || selection.startNode != selection.endNode){
			// Overtyping - remove this content.
			this.deleteContent(selection);
		}else{
			// Just split at the selection. This ensures pasted content gets inserted at the correct place inside text.
			this.sliceSelection(selection);
		}
		
		// Parse it:
		var div = document.createElement('div');
		div.innerHTML = html;
		
		// Convert to nodes:
		var parent;
		var parentOffset;
		if(selection.startNode.type == TEXT){
			parent = selection.startNode.parent;
			parentOffset = parent.content.indexOf(selection.startNode) + 1;
		}else{
			parent = selection.startNode;
			parentOffset = selection.startOffset;
		}
		
		var nodes = this.convertToNodes(div.childNodes, parent);
		var newContent = parent.content.slice(0, parentOffset).concat(nodes);
		var constructedContent = newContent.concat(parent.content.slice(parentOffset));
		parent.content = constructedContent;
		
		// Place at end:
		selection.startNode = selection.endNode = parent;
		selection.startOffset = selection.endOffset = newContent.length;
		
		// Tidy the tree up after inserting html:
		this.normalise();
	}
	
	onNewSnapshot(snap){
		this.latestSnapshot = snap;
		this.props.onSnapshot && this.props.onSnapshot(snap);
	}
	
	loadCanvas(json, init){
		try{
			var root = JSON.parse(json);

			var convertedRoot = this.convertToNodesFromCanvas(root);
			
			if(!convertedRoot.type && !convertedRoot.content){
				convertedRoot = {content: []};
			}
			
			if(!convertedRoot){
				convertedRoot = {content: []};
			}
			
			if(convertedRoot.type){
				// It becomes a parent:
				var child = convertedRoot;
				convertedRoot = {content: [child]};
				child.parent = convertedRoot;
			}
			
			convertedRoot.dom = this.state.rootRef;
			
			if(init){
				this.state.node = convertedRoot;
			}else{
				this.setState({error: null, node: convertedRoot});
			}
			
		}catch(e){
			if(init){
				this.state.error = {e, src: json};
			}else{
				this.setState({error: {e, src: json}});
			}
		}
	}
	
	loadCanvasChildren(node, result){
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
					child = this.convertToNodesFromCanvas(child);
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
	
	getRootInfo(type){
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
	
	convertToNodesFromCanvas(node){
		if(!node){
			return;
		}
		
		if(Array.isArray(node)){
			// Remove any nulls in there.
			node = node.filter(n => n);
			
			if(node.length == 1){
				node = node[0];
			}else{
				node = { c: node};
			}
		}
		
		var result = {};
		var type = node.t || node.type || node.module;
		
		if(type){
			if(type.indexOf('/') != -1){
				result.typeName = type;
				result.type = require(type).default;
				var editable = result.type.editable;
				
				if(node.t){
					// OnLoad only invoked on canvas2 nodes.
					editable && editable.onLoad && editable.onLoad(node);
				}
				
				// Only custom nodes can have data:
				result.props = result.propTypes = node.d || node.data || {};
				
				// Build the roots set.
				var roots = {};
				
				if(node.r){
					if(Array.isArray(node.r)){
						node.r.forEach((n, i) => {
							roots[i + ''] = this.convertToNodesFromCanvas({t: 'span', c: n});
						})
					}else{
						for(var key in node.r){
							roots[key] = this.convertToNodesFromCanvas({t: 'span', c: node.r[key]});
						}
					}
				}
				
				// Does the type have any roots that need adding?
				var rootSet = this.getRootInfo(result.type);
				
				for(var i=0;i<rootSet.length;i++){
					var rootInfo = rootSet[i];
					if(!roots[rootInfo.name]){
						// Note: these empty roots will be automatically rendered with a fake <br> in them so they have height.
						roots[rootInfo.name] = {t: 'span', content: []}
					}
				}
				
				if(node.c){
					// Simplified case for a common scenario of the node just having children only in it.
					// Wrap it in a root node and set it as roots.children.
					roots.children = this.convertToNodesFromCanvas({t: 'span', c: node.c});
				}else if(node.content){
					// Canvas 1 (depr)
					roots.children = this.convertToNodesFromCanvas({type: 'span', content: node.content});
				}
				
				for(var k in roots){
					// Indicate it is a root node by removing the span type and add a dom ref/ parent:
					var root = roots[k];
					root.type = null;
					root.parent = result;
					root.dom = React.createRef();
				}
				
				result.roots = roots;
				
			}else{
				result.type = type;
				
				if(node.c){
					// Canvas 2
					this.loadCanvasChildren(node, result);
				}else if(node.content){
					// Canvas 1 (depr). Its text nodes are HTML.
					var c = node.content;
					
					if(typeof c == 'string'){
						// It has one child which is a text node (no ID or templateID on this).
						var div = document.createElement('div');
						div.innerHTML = c;
						result.content = this.convertToNodes(div.childNodes, result);
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
								var div = document.createElement('div');
								div.innerHTML = child;
								content = content.concat(this.convertToNodes(div.childNodes, result));
								
							}else{
								child = this.convertToNodesFromCanvas(child, result);
								if(!child){
									continue;
								}
								
								if(!child.type){
									// typeless nodes from C1 must be merged in.
									if(child.content){
										child.content.forEach(n => n.parent = result);
										content = content.concat(child.content);
									}
								}else{
									child.parent = result;
									content.push(child);
								}
							}
						}
						
						result.content = content;
					}
				}
				
			}
		}else if(node.c){
			// a root node
			this.loadCanvasChildren(node, result);
		}
		
		if(node.i){
			result.id = node.i;
		}else if(node.id){
			result.id = node.id;
		}
		
		if(node.ti){
			result.templateId = node.ti;
		}else if(node.templateId){
			result.templateId = node.templateId;
		}
		
		if(node.s){
			// String (text node).
			result.text = node.s;
			result.type = TEXT;
		}
		
		return result;
	}
	
	convertToNodes(nodes, parent){
		if(!nodes){
			return [];
		}
		
		var output = [];
		
		for(var i=0;i<nodes.length;i++){
			var node = nodes[i];
			if(!node){
				continue;
			}
			
			var name = node.nodeName.toLowerCase();
			
			if(name == '#text'){
				output.push({
					type: TEXT,
					text: node.textContent,
					parent
				});
				
			}else if(name.length && name[0] != '#' && name.indexOf(':') == -1){ // Avoiding comments and garbage from Word.
				
				if(!permitted[name]){
					// Skip it.
					// TODO: handle <img>, <video>, <audio> and <table> if the set of permitted custom modules will accept them. <img> -> UI/Image for example.
					continue;
				}
				
				var props = undefined;
				var ssComponent = null;
				
				if(name == 'a'){
					// href is allowed.
					props = {
						href: node.getAttribute('href')
					};
					
					ssComponent = 'UI/Link';
					
				}else{
					ssComponent = name == 'span' && node.getAttribute('module');
				}
				
				var n = {
					type: ssComponent ? require(ssComponent).default : name,
					parent,
					props
				};
				
				if(ssComponent){
					n.typeName = ssComponent;
					n.roots = {
						children: {
							content: this.convertToNodes(node.childNodes, n)
						}
					};
				}else{
					n.content = this.convertToNodes(node.childNodes, n);
				}
				output.push(n);
			}
		}
		
		return output;
	}
	
	onReject(e){
		e.preventDefault();
	}
	
	onContextMenu(e){
		e.preventDefault();
		return false;
	}
	
	onMouseDown(e){
		this._mDown = true;
	}
	
	onMouseUp(e){
		// Needs to look out for e.g. selecting text but mouse-up outside the text area.
		// Because of the above, this is a global mouseup handler. Ensure it does the minimal amount possible.
		
		if(this.state.rightClick){
			this.setState({rightClick: null});
		}else if(e.button == 2){
			// Right click menu
			var {node} = this.state;
			var current = e.target;
			var target = this.getNode(current, node);
			
			while(!target && current){
				current = current.parentNode;
				target = this.getNode(current, node);
			}
			
			this.setState({
				rightClick: {
					node: target,
					x: e.clientX,
					y: e.clientY
				}
			});
		}
		
		if(this.state.sourceMode){
			this._mDown = 0;
			return;
		}
		
		if(this._mDown || this.state.selection){
			// If mouse down was on us, or we had a selection, update selection.
			this._mDown = 0;
			this.updateSelection();
		}
	}
	
	mapSelection(domSelection, selection){
		var {node} = this.state;
		this.updateRefs(node, node.dom.current);
		
		selection.startOffset = domSelection.anchorOffset;
		selection.startNode = this.getNode(domSelection.anchorNode, node);
		selection.endOffset = domSelection.focusOffset;
		selection.endNode = this.getNode(domSelection.focusNode, node);
		
		var start = selection.startNode;
		if(!start || !selection.endNode){
			return null;
		}
		
		// First, do we need to flip start and end? That happens if the user selected backwards.
		if(start == selection.endNode){
			var e = selection.endOffset;
			if(selection.startOffset > e){
				selection.endOffset = selection.startOffset;
				selection.startOffset = e;
			}
		}else{
			// Is the start of the selection actually after the end?
			// If so we need to flip them over such that start is always the first one.
			this.updateCaretPositions(node, 0);
			
			var startPosition = this.getCaretPosition(start, selection.startOffset);
			var endPosition = this.getCaretPosition(selection.endNode, selection.endOffset);
			
			if(startPosition > endPosition){
				var sOffset = selection.startOffset;
				selection.startNode = selection.endNode;
				selection.startOffset = selection.endOffset;
				selection.endOffset = sOffset;
				selection.endNode = start;
				start = selection.endNode;
			}
		}
		
		// If startNode and/ or endNode are a custom element, use the parent node for the selection instead.
		// That's because it'll be marked contentEditable=false and the cursor won't show.
		if(this.isCustom(start)){
			var p = start.parent;
			selection.startOffset = p.content.indexOf(start) + 1;
			selection.startNode = p;
		}
		
		if(this.isCustom(selection.endNode)){
			var p = selection.endNode.parent;
			selection.endOffset = p.content.indexOf(selection.endNode) + 1;
			selection.endNode = p;
		}
		
		return selection;
	}
	
	updateSelection(){
		// Updates our state selection based on wherever the browser elected to put the cursor.
		// We also track the complete tree of nodes with config options.
		var selection = this.mapSelection(window.getSelection(), {});
		
		if(!this.state.selection && !selection){
			// no-op
			return;
		}
		
		var parents = this.getUniqueParents(selection);
		this.setState({selection, active: parents, highlight: selection ? selection.startNode : null, highlightLocked: true});
	}
	
	splitIfText(node, offset){
		if(node.type == TEXT && offset != 0 && offset < node.text.length){
			var text = {
				type: TEXT,
				text: node.text.substring(offset)
			};
			
			node.text = node.text.substring(0, offset);
			
			// Insert it:
			this.addNode(text, node.parent, node.parent.content.indexOf(node) + 1);
			return text;
		}
	}
	
	/**
	* Slices the given selection, splitting any text nodes on boundaries of it. This is the only tree modification it will do.
	* The complete "parent most" nodes inside the given selection, including any newly sliced pieces, are returned in an array.
	*/
	sliceSelection(selection, root){
		if(!root){
			root = this.state.node;
		}
		// If startNode is text, split it into two.
		var newText = this.splitIfText(selection.startNode, selection.startOffset);
		
		if(newText){
			// A split happened. May need to put endNode inside it.
			if(selection.endNode == selection.startNode && selection.endOffset != selection.startOffset){
				selection.endNode = newText;
				selection.endOffset -= selection.startOffset;
			}
		}
		
		var newEnd = this.splitIfText(selection.endNode, selection.endOffset);
		
		if(newEnd){
			// Put caret inside it:
			selection.endNode = newEnd;
			selection.endOffset = 0;
		}
		
		// Ok, text at the start and end of the selection has been dealt with.
		// Next, we'll gather all nodes inside the selection.
		// Note that we now know every leaf node we encounter will be atomic - either fully inside or fully outside the range.
		// The next goal is to return "root most" nodes inside the range, such that it is not always an array of leaf nodes but does also include their parents if a parent is fully selected.
		
		var results = [];
		
		this.updateCaretPositions(root, 0);
		var endCaretPosition = this.getCaretPosition(selection.endNode, selection.endOffset);
		var startCaretPosition = this.getCaretPosition(selection.startNode, selection.startOffset);
		
		this.addIfInRange(startCaretPosition, endCaretPosition, results, root);
		return results;
	}

	slicePartialSelection(selection, root){

		if(!root){
			root = this.state.node;
		}
		// If startNode is text, split it into two.
		var newText = this.splitIfText(selection.startNode, selection.startOffset);
		
		if(newText){
			// A split happened. May need to put endNode inside it.
			if(selection.endNode == selection.startNode && selection.endOffset != selection.startOffset){
				selection.endNode = newText;
				selection.endOffset -= selection.startOffset;
			}
		}
		
		var newEnd = this.splitIfText(selection.endNode, selection.endOffset);
		
		if(newEnd){
			// Put caret inside it:
			selection.endNode = newEnd;
			selection.endOffset = 0;
		}
		
		// Ok, text at the start and end of the selection has been dealt with.
		// Next, we'll gather all nodes inside the selection.
		// Note that we now know every leaf node we encounter will be atomic - either fully inside or fully outside the range.
		// The next goal is to return "root most" nodes inside the range, such that it is not always an array of leaf nodes but does also include their parents if a parent is fully selected.
		
		var results = [];
		
		this.updateCaretPositions(root, 0);
		var endCaretPosition = this.getCaretPosition(selection.endNode, selection.endOffset);
		var startCaretPosition = this.getCaretPosition(selection.startNode, selection.startOffset);
		
		this.addIfInPartialRange(startCaretPosition, endCaretPosition, results, root);
		return results;
	}
	
	addIfInPartialRange(start, end, results, node){
		if(node.caretEnd < start || node.caretStart > end){
			// Completely misses this node.
			return;
		}
		
		// It overlaps in some way. If it's completely inside, then add the whole thing.
		if(node.caretStart >= start || node.caretEnd <= end){
			results.push(node);
		}else{
			if(node.content){
				// It partially overlaps. Enter its child nodes:
				for(var i=0;i<node.content.length;i++){
					this.addIfInPartialRange(start, end, results, node.content[i]);
				}
			}
			
			if(node.roots){
				for(var k in node.roots){
					this.addIfInPartialRange(start, end, results, node.roots[k]);
				}
			}
		}
	}

	addIfInRange(start, end, results, node){
		if(node.caretEnd < start || node.caretStart > end){
			// Completely misses this node.
			return;
		}
		
		// It overlaps in some way. If it's completely inside, then add the whole thing.
		if(node.caretStart >= start && node.caretEnd <= end){
			results.push(node);
		}else{
			if(node.content){
				// It partially overlaps. Enter its child nodes:
				for(var i=0;i<node.content.length;i++){
					this.addIfInRange(start, end, results, node.content[i]);
				}
			}
			
			if(node.roots){
				for(var k in node.roots){
					this.addIfInRange(start, end, results, node.roots[k]);
				}
			}
		}
	}
	
	// Gets caret position (a number). You must updateCaretPositions if you have made any tree changes since the last time it was updated.
	getCaretPosition(node, offset){
		if(node.type == TEXT || (node.type && typeof node.type != 'string')){
			// It's just relative to caretStart:
			return node.caretStart + offset;
		}
		
		// Otherwise it's an offset through the child nodes:
		if(offset == 0){
			return node.caretStart;
		}
		
		// +1 to target _this_ node's caret position.
		if(node.roots){
			return node.roots.children.content[offset - 1].caretEnd + 1;
		}
		
		return node.content[offset - 1].caretEnd + 1;
	}
	
	/*
	* Counts the number of caret steps in the given node, up to the given offset (excluding it).
	*/
	updateCaretPositions(node, overall){
		var result = 1; // At the start of the node is always 1, even if this node is empty.
		node.caretStart = overall + result;
		
		if(node.type == TEXT){
			result += node.text.length;
			node.caretEnd = overall + result;
		}else if(node.roots){
			// Functions just like content
			for(var k in node.roots){
				var root = node.roots[k];
				// The +1 is for the spaces between (or after all of) the nodes.
				result += this.updateCaretPositions(root, result + overall) + 1;
			}
			node.caretEnd = overall + result;
		}else if(node.content){
			for(var i=0;i<node.content.length;i++){
				// The +1 is for the spaces between (or after all of) the nodes.
				result += this.updateCaretPositions(node.content[i], result + overall) + 1;
			}
			node.caretEnd = overall + result;
		}else{
			node.caretEnd = node.caretStart;
		}
		return result;
	}
	
	getSelectionContents(){
		var result = {html: '', text: '', json: ''};
		var sel = window.getSelection();
		if (sel.rangeCount) {
			var container = document.createElement("div");
			for (var i = 0, len = sel.rangeCount; i < len; ++i) {
				container.appendChild(sel.getRangeAt(i).cloneContents());
			}
			result.html = container.innerHTML;
			result.text = container.textContent;
		}
		
		result.json = this.copyJson();
		return result;
	}
	
	onCopy(e){
		this.handleCopy(e);
	}
	
	onCut(e){
		this.handleCopy(e, true);
	}
	
	handleCopy(e, isCut){
		var cpd = (e.clipboardData || window.clipboardData);
		e.preventDefault();
		var selectionData = this.getSelectionContents();
		cpd.setData(CANVAS_JSON, selectionData.json);
		cpd.setData('html', selectionData.html);
		cpd.setData('text', selectionData.text);
		
		if(isCut){
			this.deleteSelectedContent();
		}
	}
	
	onPaste(e){
		e.preventDefault();
		var cpd = (e.clipboardData || window.clipboardData);
		var paste = cpd.getData(CANVAS_JSON);
		if(paste){
			// Pasted canvas JSON.
			// This is the format that permits copying custom props as well.
			try{
				var canvasJson = JSON.parse(paste);
				var fromCanvas = this.convertToNodesFromCanvas(canvasJson);
				this.insertJson(fromCanvas);
			}catch(e){
				console.log(e);
			}
			return;
		}
		
		paste = cpd.getData('text/html');
		if(paste){
			// Windows HTML clipboard often wraps clipboard data in some garbage that we need to strip:
			var sFrag = '<!--StartFragment-->';
			var eFrag = '<!--EndFragment-->';
			var index = paste.indexOf(sFrag);
			if(index != -1){
				paste = paste.substring(index + sFrag.length);
				
				index = paste.lastIndexOf(eFrag);
				
				if(index != -1){
					paste = paste.substring(0, index);
				}
			}
		}else{
			paste = cpd.getData('text');
			if(!paste){
				return;
			}
			
			paste = paste.replace(/\n/gi, '<br/>').replace(/\r/gi, '');
		}
		paste = paste.trim();
		this.insertHtml(paste);
	}
	
	onBlur(e){
		this.setState({selection: null});
	}
	
	getListType(node){
		if(!node){
			return null;
		}
		if(node.type == 'ul' || node.type == 'ol'){
			return node.type;
		}
		return node.parent ? this.getListType(node.parent) : null;
	}
	
	redo(){
		var toRestore = this.redoSnapshot;
				
		if(toRestore){
			
			// Advance the redo stack:
			this.redoSnapshot = toRestore.next;
			
			// Latest is our newest snapshot:
			toRestore.previous = this.latestSnapshot;
			toRestore.next = null;
			toRestore.restore();
			this.onNewSnapshot(toRestore);
		}
	}
	
	canRedo(){
		return this.redoSnapshot;
	}
	
	canUndo(){
		var latest = this.latestSnapshot;
		return (latest && latest.previous);
	}
	
	undo(){
		var latest = this.latestSnapshot;
		
		if(latest && latest.previous){
			var toRestore = latest.previous;
			
			// Push latest onto the redo stack.
			latest.next = this.redoSnapshot;
			latest.previous = null;
			this.redoSnapshot = latest;
			toRestore.restore();
			this.onNewSnapshot(toRestore);
		}
	}
	
	onKeyDown(e){
		var {selection} = this.state;
		
		if(e.altKey){
			return;
		}
		
		// Skip modifier keys:
		if(
			(e.keyCode <= 47 && e.keyCode != 32 && e.keyCode != 21 && e.keyCode != 25) ||   // control chars (except spacebar, tab and enter)
			(e.keyCode >= 91 && e.keyCode <= 95) || // OS (windows button) keys
			(e.keyCode >= 112 && e.keyCode <= 151) // F keys
		){
			if(!selection){
				// These keys like delete, home etc always require a known caret location.
				return;
			}
			
			var {startNode} = selection;
		
			if(e.keyCode >= 35 && e.keyCode <= 40){
				// Arrow keys plus home/ end. Let the browser move the caret.
				setTimeout(() => this.updateSelection(), 1);
				return;
			}
			
			// F5 special case - allow default
			if(e.keyCode == 116){
				return;
			}
			
			e.preventDefault();
			
			if(e.keyCode == 8){
				// Delete <-
				this.deleteSelectedContent(-1);
			}else if(e.keyCode == 9){
				
				if(this.state.active['li']){
					
					var listType = this.getListType(startNode);
					if(!listType){
						listType = 'ul';
					}
								
					// Force insersion of list:
					this.insertWrap(selection, [listType, 'li'], null, true);
				}
				
			}else if(e.keyCode == 13){
				
				// Enter
				
				if(selection.startOffset != selection.endOffset || startNode == selection.endNode){
					// Overtyping - remove this content.
					this.deleteContent(selection);
				}
				
				if(e.shiftKey){
					// Insert a <br> in current content, based on location of the caret.
					var brParent = selection.startNode;
					var brOffset = selection.startOffset;
					
					if(startNode.type == TEXT){
						// Split the start node in two.
						brParent = brParent.parent;
						brOffset = brParent.content.indexOf(selection.startNode) + 1;
					}
					
					this.addNode(
						{
							type: 'br'
						},
						brParent,
						brOffset
					);
					
					if(startNode.type == TEXT){
						// Insert another text node, which might involve some part of the existing text node.
						var newText = {
							type: TEXT,
							text: startNode.text.substring(selection.startOffset)
						};
						
						startNode.text = startNode.text.substring(0, selection.startOffset);
						
						this.addNode(
							newText,
							brParent,
							brOffset + 1
						);
						
						selection.startNode = selection.endNode = newText;
						selection.startOffset = selection.endOffset = 0;
					}else{
						selection.startNode = selection.endNode = brParent;
						selection.startOffset = selection.endOffset = brOffset + 1;
					}
					
				}else{
					// Create a new block element, as a duplicate of the current selections nearest block element.
					var currentBlock = this.getBlockOrRoot(selection.startNode);
					
					if(!currentBlock || !currentBlock.type){
						// If previous element was not inside a block element, put it and any sibling inlines in a <p>, 
						// and we're then effectively duplicating that.
						
						var root = currentBlock ? currentBlock : this.state.node;
						
						currentBlock = {
							type: 'p',
							content: []
						};
						
						// Get the root-most parent:
						var rootMost = this.rootMostParent(selection.startNode);
						
						// Its index:
						var rootIndex = root.content.indexOf(rootMost);
						
						// Starting at rootIndex - 1, go backwards collecting all inlines:
						var startingIndex = 0;
						
						for(var i=rootIndex - 1;i>=0;i--){
							if(!this.isInline(root.content[i])){
								startingIndex = i + 1;
								break;
							}
						}
						
						// Splice that segment:
						currentBlock.content = root.content.splice(startingIndex, 1 + rootIndex - startingIndex);
						
						currentBlock.content.forEach(child => child.parent = currentBlock);
						
						this.addNode(currentBlock, root, startingIndex);
					}
					
					var insertIndex = currentBlock.parent.content.indexOf(currentBlock) + 1;
					
					// If the current block is a list item, duplicate it, otherwise use <p>.
					
					var newBlock = {
						type: currentBlock.type == 'li' ? 'li' : 'p',
						content: []
					};
					
					// TODO: This needs to move all other _inline_ elements after startNode in the current as well rather than just one and only text node.
					// <p>Test 2 <img /></p> hit enter anywhere in the text. It leaves the img behind incorrectly.
					// Also try it with formatting: <p><b>Tes|t 2 </b><img /></p>. In this situ it needs to duplicate any parent inline elements.
					
					if(startNode.type == TEXT && selection.startOffset < startNode.text.length){
						// Insert another text node, which might involve some part of the existing text node.
						var newText = {
							type: TEXT,
							text: startNode.text.substring(selection.startOffset)
						};
						
						startNode.text = startNode.text.substring(0, selection.startOffset);
						
						this.addNode(newText, newBlock);
						
						selection.startNode = selection.endNode = newText;
						selection.startOffset = selection.endOffset = 0;
					}else{
						selection.startNode = selection.endNode = newBlock;
						selection.startOffset = selection.endOffset = 0;
					}
					
					this.addNode(newBlock, currentBlock.parent, insertIndex);
				}
				
				this.normalise();
				
			}else if(e.keyCode == 46){
				// Delete -> 
				this.deleteSelectedContent(1);
			}
			
			return;
		}
		
		if(e.ctrlKey || e.metaKey){ // metaKey for mac
			
			// Special case for formatting shortcuts. They require a caret.
			if(selection){
				if(e.keyCode == 66){
					// ctrl+b
					e.preventDefault();
					this.applyWrap(selection, 'b');
				}else if(e.keyCode == 73){
					// ctrl+i
					e.preventDefault();
					this.applyWrap(selection, 'i');
				}else if(e.keyCode == 85){
					// ctrl+u
					e.preventDefault();
					this.applyWrap(selection, 'u');
				}
			}
			
			if(e.keyCode == 89){
				// ctrl+y (redo)
				e.preventDefault();
				this.redo();
				
			}else if(e.keyCode == 65){
				e.preventDefault();
				// ctrl+a
				var node = this.state.node;
				this.setState({selection: {
					startNode: node,
					endNode: node,
					startOffset: 0,
					endOffset: node.content.length
				}});
				
			}else if(e.keyCode == 90){
				// ctrl+z (undo)
				e.preventDefault();
				this.undo();
			}
			
			return;
		}
		
		e.preventDefault();
		
		// Replace selection (if it has anything in it) with the typed key.
		if(!selection){
			return;
		}
		
		var key = e.key;
		
		var {startNode} = selection;
		
		if(selection.startOffset != selection.endOffset || startNode != selection.endNode){
			// Overtyping - remove this content.
			this.deleteContent(selection);
			startNode = selection.startNode;
		}
		
		if(startNode.type != TEXT){
			// Insert a text node at this location.
			var textNode = {type: TEXT, text: ''};
			this.addNode(textNode, startNode);
			startNode = textNode;
			selection.startOffset = 0;
		}
		
		// Special case for spacebar - if the char before the cursor is already a regular whitespace, insert an nbsp.
		if(e.keyCode == 32 && selection.startOffset != 0){
			var charCode = startNode.text.charCodeAt(selection.startOffset - 1);
			if(charCode == 32){
				key = '\xA0'; // nbsp
			}
		}
		
		if(key == '@'){
			// Possible @mention. Permitted if at start of text (startOffset=0) or prev char is a space/ nbsp.
			var isMention = selection.startOffset == 0;
			if(!isMention){
				var p = startNode.text[selection.startOffset - 1];
				if(p == ' ' || p == '\xA0'){
					isMention = true;
				}
			}
			
			if(isMention && this.props.handleMentions){
				
				// Insert a token instead:
				this.sliceSelection(selection);
				
				var parent;
				var insertIndex;
				
				if(selection.startNode.type == TEXT){
					parent = selection.startNode.parent;
					insertIndex = parent.content.indexOf(selection.startNode) + 1;
				}else{
					parent = selection.startNode;
					insertIndex = selection.startOffset;
				}
				
				var token = {
					type: Token,
					typeName: 'UI/Token'
				};
				
				selection.startNode = selection.endNode = this.addNode({type: TEXT, text: '@'}, token);
				
				this.addNode(token, parent, insertIndex);
				
				// Update current position:
				selection.startOffset = selection.endOffset = 1;
				
				this.normalise(true);
				return;
			}
		}
		
		if(selection.startOffset == startNode.text.length){
			startNode.text += key;
		}else{
			// Insert character into middle of text:
			startNode.text = startNode.text.substring(0, selection.startOffset) + key + startNode.text.substring(selection.startOffset);
		}
		
		// Update end:
		selection.startOffset+= key.length;
		selection.endOffset = selection.startOffset;
		selection.startNode = selection.endNode = startNode;
		
		this.normalise(true);
	}
	
	/*
	debugNode(node){
		if(node.type == TEXT){
			return node.text;
		}
		
		var isHtml = node.type && typeof node.type == 'string';
		
		var res = isHtml ? "<" + node.type + ">" : "";
		
		if(node.content){
			for(var i=0;i<node.content.length;i++){
				res += this.debugNode(node.content[i]);
			}
		}
		
		if(isHtml){
			res += "</" + node.type + ">";
		}
		
		return res;
	}
	
	debugUndoRedo(){
		var undo = [];
		var latest = this.latestSnapshot;
		while(latest){
			undo.push(this.debugNode(latest.node));
			latest = latest.previous;
		}
		
		var redo = [];
		var latest = this.redoSnapshot;
		while(latest){
			redo.push(this.debugNode(latest.node));
			latest = latest.next;
		}
		
		console.log(undo, redo);
	}
	*/
	
	displayModuleName(name){
		if(!name){
			return '';
		}
		var parts = name.split('/');
		if(parts[0] == 'UI'){
			parts.shift();
		}
		
		return parts.join(' > ');
	}
	
	renderContextMenu(){
		var {rightClick, selection} = this.state;
		
		var buttons = [];
		
		var {node} = rightClick;
		
		var cur = node;

		while(cur){
			if(cur.type){
				if(typeof cur.type != 'string'){
					
					((c) => {
						var displayName = this.displayModuleName(c.typeName);
						buttons.push({
							onClick: (e) => {
								e.preventDefault();
								
								if(!this.cfgWindow || this.cfgWindow.window.closed){
									var cfgWin = window.open(
										location.origin + '/pack/rte.html?v=' + getBuildDate().timestamp,
										displayName,
										"toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=600,height=400"
									);
									
									this.cfgWindow = {
										window: cfgWin
									};
									
									// The window was already loaded otherwise - we don't have to wait for the load event.
									if(!cfgWin.document || !cfgWin.document.body.parentNode.className){
										this.cfgWindow.loaders = [];
										
										cfgWin.addEventListener("load", e => {
											if(this.cfgWindow.window != cfgWin){
												return;
											}
											
											var { loaders } = this.cfgWindow;
											this.cfgWindow.loaders = null;
											loaders.map(loader => loader(cfgWin));
										}, false);
									}
									
									
								}else{
									var cfgWin = this.cfgWindow.window;
									cfgWin.focus && cfgWin.focus();
								}
								
								var configReady = cfgWin => {
									(React.render || ReactDom.render)(
										<div className="prop-editor-window">
											<PropEditor optionsVisibleFor = {c} onChange={() => this.normalise()}/>
										</div>,
										cfgWin.document.body
									);
								};
								
								if(this.cfgWindow.loaders){
									this.cfgWindow.loaders.push(configReady);
								}else{
									// This delay helps avoid issues when the window was already open but wasn't set to cfgWin.
									setTimeout(() => {
										configReady(this.cfgWindow.window);
									}, 50);
								}
							},
							icon: 'cog',
							text: 'Edit ' + displayName
						});
						
						if(this.props.modules){
							buttons.push({
								onClick: (e) => {
									e.preventDefault();
									this.removeNode(c, selection);
									this.normalise();
								},
								icon: 'trash',
								text: 'Delete ' + displayName
							});
							
							buttons.push({
								onClick: (e) => {
									e.preventDefault();
									
									var endOffset = 0;
									
									var parent = c.parent;
									var parentIndex;
									
									if(parent.roots){
										parentIndex = parent.roots.children.content.indexOf(c);
									}else{
										parentIndex = parent.content.indexOf(c);
									}
									
									if(parentIndex != -1){
										this.setState({ selection: {
											startNode: parent,
											endNode: parent,
											startOffset: parentIndex,
											endOffset: parentIndex+1
										} });
									}
								},
								icon: 'marker',
								text: 'Select ' + displayName
							});
							
							buttons.push({
								onClick: (e) => {
									e.preventDefault();
									this.setState({ selectOpenFor: {node: c, isAfter: false} });
								},
								icon: 'plus',
								text: 'Insert before ' + displayName + '..'
							});
							
							buttons.push({
								onClick: (e) => {
									e.preventDefault();
									this.setState({ selectOpenFor: {node: c, isAfter: true} });
								},
								icon: 'plus',
								text: 'Insert after ' + displayName + '..'
							});
						}
					})(cur);
				}
			} else {

				((c) => {
					buttons.push({
						onClick: (e) => {
							e.preventDefault();
							this.setState({selectOpenFor: {node: c, isAfter: false}})
						},
						icon: 'plus',
						text: 'Add here'
					});
				})(cur);
			}
			cur = cur.parent;
		}
		
		buttons.push({
			onClick: () => this.setState({sourceMode: {src: this.latestSnapshot.toCanvasJson(true) || '{"c": ""}'}}),
			icon: 'code',
			text: 'Edit JSON source'
		});

		return <div className="context-menu" style={{
				left: rightClick.x + 'px',
				top: rightClick.y + 'px'
			}}>
			{buttons.map(cfg => {
				
				return (
					<button className="context-btn" onMouseDown={cfg.onClick}>
						<i className={"fa fa-fw fa-" + cfg.icon} />
						{cfg.text}
					</button>
				);
				
			})}
		</div>;
	}
	
	clearContextMenu(e){
		if(this.state.rightClick){
			this.setState({rightClick: null});
		}
	}
	
	rootMostParent(node){
		var root = this.state.node;
		while(node){
			if(node.parent == root || (node.parent && !node.parent.type)){
				return node;
			}
			node = node.parent;
		}
	}
	
	/* 
		True if the given parent object is a parent of (or equal to) the given DOM node.
	*/
	isDomParentOf(parent, node){
		var p = node;
		while(p){
			if(p == parent){
				return true;
			}
			p = p.parentNode;
		}
	}
	
	/* 
		True if the given parent object is a parent of (or equal to) the given node.
	*/
	isParentOf(parent, node){
		var p = node;
		while(p){
			if(p == parent){
				return true;
			}
			p = p.parent;
		}
	}
	
	isInline(node){
		return typeof node.type != 'string' || !!inlines[node.type];
	}
	
	deleteSelectedContent(dir){
		var {selection} = this.state;
		
		if(selection.startOffset == selection.endOffset && selection.startNode == selection.endNode){
			// No actual selection - select 1 "character", forward or back (if DEL or backspace).
			// The best way to do that is via the widely supported getSelection.modify method - the browser is fully in control of the caret movement as a result.
			var sel = window.getSelection();
			sel.modify("extend", dir == 1 ? "forward" : "backward", "character");
			
			// Make our selection from the window one:
			this.mapSelection(sel, selection);
			
			// Un extend the selection:
			if(dir == 1) {
				sel.collapseToStart();
			}else{
				sel.collapseToEnd();
			}
			
		}
		
		this.deleteContent(selection);
		
		selection.endOffset = selection.startOffset;
		selection.endNode = selection.startNode;
		this.normalise();
	}
	
	/*
	* Useful for setting the active state of formatting buttons on the UI. 
	* Gets the unique parent nodes for the given selection or node (specifically, it's at the start of the selection).
	*/
	getUniqueParents(node){
		if(!node){
			return {};
		}
		if(node.startNode){
			node = node.startNode;
		}
		var parents = {};
		while(node != null){
			if(node.type && node.type != TEXT){
				if(typeof node.type === 'string'){
					parents[node.type] = true;
				}else{
					parents[node.typeName] = true;
				}
			}
			node = node.parent;
		}
		
		return parents;
	}
	
	/*
	* Wraps (or unwraps) the given selection in an ele of the given type, with the given props.
	*/
	applyWrap(selection, type, props, roots){
		if(!selection){
			return;
		}
		
		var active = this.getUniqueParents(selection);
		
		if(active[type]){
			// It's already active - we'll want to remove it.
			this.removeWrap(selection, type);
		}else{
			// Insert it.
			this.insertWrap(selection, type, props, false, roots);
		}
	}
	
	removeWrapsIn(node, type, selection){
		if(node.content){
			for(var i=node.content.length-1;i>=0;i--){
				var child = node.content[i];
				this.removeWrapsIn(child, type, selection);
				if(child.type == type){
					this.removeNode(child, selection, true);
				}
			}
		}
		
		if(node.roots){
			for(var k in node.roots){
				var child = node.roots[k];
				// No need to check child.type as it's always null for a root.
				this.removeWrapsIn(child, type, selection);
			}
		}
	}
	
	/* Unwraps selection with inline ele of given type. May end up generating multiple ele's. */
	removeWrap(selection, type){
		
		// First, split the selection:
		var nodesInSelection = this.sliceSelection(selection);
		
		// Remove from any nodes inside the sliced set.
		nodesInSelection.forEach(node => {
			// Check its children:
			this.removeWrapsIn(node, type, selection);
			
			if(node.type == type){
				this.removeNode(node, selection, true);
			}
		});
		
		// Next, for each parent of the nodes in the selection, check if it is the type we want to remove.
		nodesInSelection.forEach(node => {
			var n = node.parent;
			while(n){
				var next = n.parent;
				if(n.type == type){
					this.removeNode(n, selection, true);
				}
				n = next;
			}
		});
		
		// Future todo (if necessary) - re-add the wrap to any parent-most nodes that are not inside the selection.
		
		// Tidy the tree up after modifying it (this also stores an undo snapshot amongst other things).
		this.normalise();
	}
	
	deepClone(node, selection, clonedSelection){
		if(!node){
			return null;
		}
		var result = {
			type: node.type,
			typeName: node.typeName,
			text: node.text,
			id: node.id,
			templateId: node.templateId
		};
		
		if(selection){
			if(node == selection.startNode){
				clonedSelection.startNode = result;
			}
			if(node == selection.endNode){
				clonedSelection.endNode = result;
			}
		}
		
		if(node.content){
			var content = [];
			for(var i=0;i<node.content.length;i++){
				var clone = this.deepClone(node.content[i], selection, clonedSelection);
				content.push(clone);
				clone.parent = result;
			}
			result.content = content;
		}
		
		if(node.roots){
			var roots = {};
			for(var k in node.roots){
				var clone = this.deepClone(node.roots[k], selection, clonedSelection);
				roots[k] = clone;
				clone.parent = result;
			}
			result.roots = roots;
		}
		
		if(node.props){
			result.props = {...node.props};
		}
		
		return result;
	}
	
	/* Wraps selection with inline ele of given type. May end up generating multiple ele's. */
	insertWrap(selection, types, props, forceInsert, roots){
		var nodesThatRequireWrapping = this.sliceSelection(selection);
		
		if(!Array.isArray(types)){
			types = [types];
		}
		
		var primaryType = types[types.length-1];
		
		// Is it a module?
		var typeName = undefined;
		var customType = false;
		
		if(primaryType.indexOf('/') != -1){
			typeName = primaryType;
			primaryType = require(primaryType).default;
			customType = true;

			var props = null;		
		
			if(primaryType && primaryType.propTypes){
				props = primaryType.propTypes;
				var nameParts = typeName.split('/');			
			}
		}
		
		if(!!inlines[primaryType] || (primaryType.editable && primaryType.editable.inline)){
			
			// Remove from the nodes if it already exists in there for tidiness:
			nodesThatRequireWrapping.forEach(node => {
				// Check its children:
				this.removeWrapsIn(node, primaryType, selection);
			});
			
			nodesThatRequireWrapping.forEach(node => {
				if(node.type == primaryType){
					// Nothing to do - it's already the type we want.
					return;
				}
				
				var newParent = {type: primaryType, typeName, props: {}};
				
				var origParent = node.parent;
				var nodeContent = origParent ? [node] : node.content;
				
				if(customType){
					// it's a root:
					var childRoot = {
						content: nodeContent
					};
					
					var clonedRoots = {}; 
					for(var k in roots){
						clonedRoots[k] = this.deepClone(roots[k]);
					}
					clonedRoots.children = childRoot;
					newParent.roots = clonedRoots;
					
					for(var k in clonedRoots){
						clonedRoots[k].parent = newParent;
					}
					
					nodeContent.forEach(n => n.parent = childRoot);
				}else{
					newParent.content = nodeContent;
					nodeContent.forEach(n => n.parent = newParent);
				}
				
				if(origParent){
					var index = origParent.content.indexOf(node);
					origParent.content[index] = newParent;
					newParent.parent = origParent;
				}else{
					// Root: ctrl+a wrapping everything. Content becomes all of roots content, and we get pushed into it.
					node.content=[newParent];
					newParent.parent = node;
					
					// Special case - edit the selection as well: 
					if(selection.endNode == node){
						selection.endOffset = 1;
					}
					
					if(selection.startNode == node){
						selection.startOffset = 0;
					}
				}
				
			});
		}else{
			// Block type. Collect all unique leaf-most block parents, and then based on heuristics either convert them or wrap all their childnodes.
			// Note that the root node is considered a block parent in this check.
			var uniqueBlockParents = [];
			
			if(!nodesThatRequireWrapping.length){
				// Nothing actually selected, but still applies to object.
				uniqueBlockParents.push(this.getBlockOrRoot(selection.startNode));
			}
			
			nodesThatRequireWrapping.forEach(node => {
				var block = this.getBlockOrRoot(node);
				
				if(uniqueBlockParents.indexOf(block) == -1){
					uniqueBlockParents.push(block);
				}
			});
			
			uniqueBlockParents.forEach(blockNode => {
				
				var typeIndex = types.length-1;
				
				while(typeIndex >= 0){
					
					var type = types[typeIndex];
					
					// 0 = Change the type of the given node.
					// 1 = Wrap all the children of the given node in a new block type.
					// 2 = Wrap the given node in a new node of the new block type.
					var action = 0;
					
					if(blockNode == this.state.node || !blockNode.type){
						// Root node. Must wrap all its child nodes in the new block type.
						action = 1;
					}else if(forceInsert){
						action = 1;
					}else{
						// Depending on the type of node that blockNode is vs. type, it may be either converted, 
						// parented to the new node, or have the new node as a child.
						/*if(blockNode.type == 'li' && type != 'li'){
							action = 1;
						}else if(type == 'li'){
							action = 0;
						}else{
							action = 0;
						}*/
					}
					
					if(action == 0){
						blockNode.type = type;
					}else if(action == 1){
						// Wrap blockNode's children with a new node:
						var newNode = {type, props: props ? {...props} : null, content: blockNode.content};
						blockNode.content = [newNode];
						newNode.parent = blockNode;
						newNode.content.forEach(n => n.parent = newNode);
					}else{
						// Wrap blockNode with new node:
						var newNode = {type, props: props ? {...props} : null, content: [blockNode]};
						var index = blockNode.parent.indexOf(blockNode);
						blockNode.parent.content[index] = newNode;
						newNode.parent = blockNode.parent;
						blockNode.parent = newNode;
						blockNode = newNode;
					}
					
					typeIndex--;
					blockNode = blockNode.parent;
				}
				
			});
		}
		
		// Normalise the tree to indicate we're done modifying it:
		this.normalise();
	}
	
	deleteContent(selection){
		// Delete everything from start -> end
		var node = selection.startNode;
		
		if(node == selection.endNode && selection.startOffset == selection.endOffset){
			// If they are the same, nothing happens.
			return;
		}
		
		// Slice the selection, returning all the unique parent-most nodes inside it. Will only modify the tree by slicing text nodes.
		var toDelete = this.sliceSelection(selection);
		
		var partialSelections = this.slicePartialSelection(selection); // This is an attempt at slicing out a parent caught in a partial selection. 
		
		// Do we need to handle our partial selections? We only worry about those when our sliceSelection didn't produce a clean result.
		if(toDelete.length == 0) {
			// Let's step through the partial selections.
			partialSelections.forEach(partial => {
				if(partial == selection.endNode.parent) {
					toDelete.push(partial);
				}
			})
		}


		// If any parents of endNode are marked for deletion, don't delete them.
		// This is because if endNode has other inline siblings, they shouldn't lose their formatting, 
		// but they should however lose all *block elements* that they are inside.
		var relocateEnd = false;
		var curParent = selection.endNode.parent;
		var toMerge = [];
		while(curParent){
			var parentIndex = toDelete.indexOf(curParent);
			if(parentIndex != -1){

				// It wants to delete a parent. Is it inline?
				if(this.isInline(curParent)){

					// Prevent removal of inline parents:
					toDelete.splice(parentIndex, 1);
				}else{

					// Block parent. This does get deleted. 
					// Any of its children that aren't being deleted are merged into the parent of startNode.
					toMerge.push(curParent);
				}
			}
			curParent = curParent.parent;
		}
		
		console.log("Before remove", selection);
		
		toDelete.map(node => this.removeNode(node, selection));
		
		var mergeInto = node;
		var mergeIndex = selection.startOffset;
		
		if(node.type == TEXT || typeof node.type != 'string' && node.parent && node.type){
			// Merge into the parent of node, just after it.
			mergeInto = mergeInto.parent;
			mergeIndex = mergeInto.content.indexOf(node) + 1;
		} // Otherwise we merge in just after the startOffset in the start node.
		
		toMerge.map(n => {
			// These elements are already in render order, so:
			n.content.forEach(c => {
				this.addNode(c, mergeInto, mergeIndex);
				mergeIndex++;
			});
		});
	}
	
	normalise(isMinorState){
		var {node} = this.state;
		var selection = this.state.selection;
		this.normaliseNode(node, selection);
		var parents = this.getUniqueParents(selection);
		this.setState({selection, active: parents, highlight: selection ? selection.startNode : null, highlightLocked: true});		
		this.addStateSnapshot(isMinorState, node, selection);
	}
	
	getMaxId(node, currentMax){
		if(!node){
			return;
		}
		
		if(node.id > currentMax){
			currentMax = node.id;
		}
		
		if(node.content){
			for(var i=0;i<node.content.length;i++){
				var check = this.getMaxId(node.content[i], currentMax);
				if(check > currentMax){
					currentMax = check;
				}
			}
		}
		
		return currentMax;
	}
	
	getTextOnly(node){
		// Iterate through the tree collecting only text nodes.
		if(!node){
			return '';
		}
		
		if(node.type == TEXT){
			return node.text;
		}
		
		var result = '';
		if(node.content){
			for(var i=0;i<node.content.length;i++){
				result += this.getTextOnly(node.content[i]);
			}
		}
		
		if(node.roots && node.roots.children){
			result += this.getTextOnly(node.roots.children);
		}
		
		return result;
	}
	
	toCanvasFormat(node, options){
		if(!node){
			return null;
		}
		
		if(Array.isArray(node)){
			var content = [];
			
			for(var i=0;i<node.length;i++){
				var converted = this.toCanvasFormat(node[i], options);
				if(converted){
					content.push(converted);
				}
			}
			
			if(!content.length){
				return null;
			}
			
			if(content.length == 1){
				return content[0];
			}
			
			return content;
		}
		
		var resultNode = {};
		var attribs = false;
		
		if(node.type == TEXT){
			resultNode.s = node.text;
			attribs = true;
		}else{
			if(node.type){
				if(typeof node.type == 'string'){
					resultNode.t = node.type;
				}else{
					// Custom components can have data (props) as well.
					resultNode.t = node.typeName;
					
					if(node.props){
						resultNode.d = node.props;
					}
					
					if(node.roots){
						// If there is only 1 key and it is children, then we apply it to resultNode.c:
						var keys = Object.keys(node.roots);
						
						if(keys.length){
							if(keys.length == 1 && keys[0] == 'children'){
								var c = this.toCanvasFormat(node.roots.children.content, options);
								if(c){
									resultNode.c = c;
								}
							}else{
								resultNode.r = {};
								for(var k in node.roots){
									resultNode.r[k] = this.toCanvasFormat(node.roots[k].content, options);
								}
							}
						}
					}
				}
				
				attribs = true;
			}
			
			if((!node.parent || typeof node.type == 'string') && node.content && node.content.length){
				var content = this.toCanvasFormat(node.content, options);
				
				if(content){
					resultNode.c = content;
					attribs = true;
				}
			}
		}
		
		if(options && options.id){
			// Only include id if we need to.
			if(node.id){
				resultNode.i = node.id;
				attribs = true;
			}else{
				options.id++;
				resultNode.i = options.id;
				attribs = true;
			}	
		}else if(node.templateId){
			// This id is required.
			resultNode.i = node.id;
			attribs = true;
		}
		
		if(node.templateId){
			resultNode.ti = node.templateId;
			attribs = true;
		}
		
		if(node.type && node.type.editable && node.type.editable.onSave){
			node.type.editable.onSave(resultNode, node);
		}
		
		if(!resultNode.i && !resultNode.ti && node.type == TEXT){
			// This is a {c: "text"} node which can be shortformed to just the text:
			return node.text;
		}
		
		// If it has no attributes at all, return null.
		return attribs ? resultNode : null;
	}
	
	cloneNodeAndSelection(node, selection){
		var clonedSelection = {};
		var clonedNode = this.deepClone(node, selection, clonedSelection);
		
		clonedSelection.startOffset = selection ? selection.startOffset : 0;
		clonedSelection.endOffset = selection ? selection.endOffset : 0;
		
		// If the snapshots selection is null, select the root node.
		// This occurs when restoring to an empty editor, and unfocuses the editor meaning you'd have to click on it otherwise.
		if(!clonedSelection.startNode || !clonedSelection.endNode){
			clonedSelection.startNode = clonedNode;
			clonedSelection.endNode = clonedNode;
			clonedSelection.startOffset = 0;
			clonedSelection.endOffset = 0;
		}
		
		return {clonedNode, clonedSelection};
	}
	
	addStateSnapshot(isMinor, node, selection){
		
		if(!node){
			// Snap the state:
			node = this.state.node;
			selection = this.state.selection;
		}
		
		var {clonedNode, clonedSelection} = this.cloneNodeAndSelection(node, selection);
		
		var snapshot = {
			isMinor,
			time: Date.now(),
			node: clonedNode,
			selection: clonedSelection,
			toCanvasJson: (pretty) => {
				// Converts the node to canvas JSON.
				
				var options = {};
				
				if(!this.props.withoutIds){
					// Add IDs in the output if they don't already have them.
					options.id = this.getMaxId(snapshot.node, 0) || 1;
				}
				
				var cfNode = this.toCanvasFormat(snapshot.node, options);
				
				if(!cfNode){
					return '';
				}
				
				var json = JSON.stringify(cfNode, null, pretty ? '\t' : null);
				return json;
			},
			restore: () => {
				// Snapshots must be cloned on restore. Otherwise if you edited it and then tried to go 
				// back/ forward to the same snapshot, it will have been modified.
				
				var selection = {
					startOffset: snapshot.selection.startOffset,
					endOffset: snapshot.selection.endOffset
				};
				
				var node = this.deepClone(snapshot.node, snapshot.selection, selection);
				
				// Use root dom ref:
				node.dom = this.state.node.dom;
				snapshot.time = Date.now();
				
				this.setState({node, selection});
			}
		};
		
		var latest = this.latestSnapshot;
		snapshot.previous = latest;
		
		// If the latest snapshot is also minor, it just replaces it if the time diff is short enough.
		if(isMinor && latest && latest.isMinor && (snapshot.time - latest.time) < 500){
			// Replace it.
			snapshot.previous = latest.previous;
		}
		
		this.redoSnapshot = null;
		this.onNewSnapshot(snapshot);
		return snapshot;
	}
	
	normaliseNode(node, selection){
		if(!node){
			return;
		}
		
		// Actions performed:
		// - Tidy any children.
		// - Remove any empty inline elements. This includes any custom ele's that act inline.
		// - Merge side-by-side inline nodes (text nodes included).
		
		if(node.content){
			// Tidy any children. Go backwards because they can remove themselves.
			for(var i=node.content.length - 1;i>=0;i--){
				this.normaliseNode(node.content[i], selection);
			}
		}
		
		if(node.roots){
			// Tidy any children. Go backwards because they can remove themselves.
			for(var k in node.roots){
				var root = node.roots[k];
				this.normaliseNode(root, selection);
				
				if(k == 'children' && (!root.content || !root.content.length)){
					var editInfo = node.type.editable;
					if(editInfo && editInfo.inline){
						// Remove the parent custom node as well - it's empty and acts like an inline element.
						this.removeNode(node, selection);
						return;
					}
				}
			}
		}

		// Is a custom object.
		if(node.typeName) {
			if(node.type.onEdit) {
				node.type.onEdit(node);
			}
		}
		
		// Are we empty? If yes and it's an inline ele, delete the node.
		if(node.type == TEXT){
			if(!node.text){
				// Empty text node.
				this.removeNode(node, selection);
			}
			return;
		}else if((!node.content || !node.content.length) && !!inlines[node.type] && !inlineNoContent[node.type]){
			// Empty inline element.
			this.removeNode(node, selection);
			return;
		}
		
		if(node.content){
			// Merge side by side inline nodes:
			var prev = false;
			for(var i=node.content.length - 1;i>=0;i--){
				var c = node.content[i];
				if(!inlines[c.type]){ // use inlines - don't merge custom types
					prev = false;
					continue;
				}
				
				if(prev && c.type == prev.type){
					// 2 inline nodes of the same basic html type in a row - merge them now:
					this.removeNode(prev, selection, 2);
					
					if(c.type != TEXT){						
						// Normalise it again:
						this.normaliseNode(c, selection);
					}
				}
				
				prev = c;
			}
		}
	}
	
	// Gets first block node by going down the tree, or root.
	getBlockOrRoot(node){
		if(node == this.state.node || !node.type || (typeof node.type == 'string' && !inlines[node.type])){
			// It's a block node (or a root).
			return node;
		}
		
		return this.getBlockOrRoot(node.parent);
	}
	
	addNode(node, parent, index){
		if(parent.type && typeof parent.type != 'string'){
			// It's a custom component. Child content goes into a root called children.
			var roots = parent.roots;
			
			if(!roots){
				parent.roots = roots = {};
			}
			
			if(!roots.children){
				roots.children = {parent, content: []};
			}
			
			// Add into the child root:
			return this.addNode(node, roots.children, index);
		} else if(!parent.content) {
			parent.content = [];
		}
		
		if(index === undefined){
			parent.content.push(node);
		}else{
			// Insert at given index:
			parent.content.splice(index,0,node);
		}
		node.parent = parent;
		return node;
	}
	
	removeNode(node, selection, retainChildren){
		if(!node.parent || !node.type){
			// It's a root. Just empty it instead.
			node.content = [];
			
			if(selection.startNode == node){
				selection.startOffset = 0;
			}
			
			if(selection.endNode == node){
				selection.endOffset = 0;
			}
			
			return;
		}
	
		var index = node.parent.content.indexOf(node);
		
		if(retainChildren){
			if(retainChildren == 2){
				// Kids are merged into neighbour.
				var neighbour = node.parent.content[index - 1];
				
				if(neighbour.type == TEXT){
					
					// If the caret was in node, move to the equiv position inside neighbour:
					if(selection){
						var origLength = neighbour.text.length;
						
						if(selection.startNode == node){
							selection.startNode = neighbour;
							selection.startOffset += origLength;
						}
						
						if(selection.endNode == node){
							selection.endNode = neighbour;
							selection.endOffset += origLength;
						}
					
						// If it was just before the node, put it at the end of the neighbour:
						if(selection.startNode == node.parent && selection.startOffset == index){
							selection.startNode = neighbour;
							selection.startOffset = origLength;
						}
						
						if(selection.endNode == node.parent && selection.endOffset == index){
							selection.endNode = neighbour;
							selection.endOffset = origLength;
						}
					}
					
					// If the first ends in a space, and the second starts with a space, use nbsp.
					if(neighbour.text.length > 0 && node.text.length > 0 && neighbour.text[neighbour.text.length - 1] == ' ' && node.text[0] == ' '){
						neighbour.text += '\xA0' + node.text.substring(1);
					}else{
						neighbour.text += node.text;
					}
					
				}else{
					// Merge children
					var offset = neighbour.content.length;
					neighbour.content = neighbour.content.concat(node.content);
					node.content.forEach(n => n.parent = neighbour);
					
					// Was the caret in this node?
					if(selection){
						if(selection.startNode == node){
							selection.startNode = neighbour;
							selection.startOffset += offset;
						}
						
						if(selection.endNode == node){
							selection.endNode = neighbour;
							selection.endOffset += offset;
						}
					}
					
				}
				
				// Remove from parent:
				node.parent.content.splice(index, 1);
			}else{
				// kids are merged into parent
				
				node.parent.content = node.parent.content.slice(0, index).concat(node.content || []).concat(node.parent.content.slice(index + 1));
				node.content.forEach(child => child.parent = node.parent);
				
				if(selection){
					if(selection.startNode == node){
						selection.startNode = node.parent;
						selection.startOffset = index;
					}
					
					if(selection.endNode == node){
						selection.endNode = node.parent;
						selection.endOffset = index;
					}
				}
			}
			
		}else{
			// Remove from parent:
			node.parent.content.splice(index, 1);
			
			// If caret is in the node, or in any of its children, relocate to the start of the parent.
			if(selection){
				
				if(this.isParentOf(node, selection.startNode)){
					selection.startNode = node.parent;
					selection.startOffset = index;
				}else if(selection.startNode == node.parent && selection.startOffset >= index && selection.startOffset != 0){
					// The caret was somewhere after this node - relocate it.
					selection.startOffset--;
				}
				
				if(this.isParentOf(node, selection.endNode)){
					selection.endNode = node.parent;
					selection.endOffset = index;
				}else if(selection.endNode == node.parent && selection.endOffset >= index && selection.endOffset != 0){
					// The caret was somewhere after this node - relocate it.
					selection.endOffset--;
				}
			}
		}
		
	}
	
	renderNode(node){
		if(Array.isArray(node)){
			return node.map((n,i) => this.renderNode(n));
		}
		
		var NodeType = node.type;
		
		if(NodeType == TEXT){
			return node.text;
		}else if(typeof NodeType === 'string'){
			if(!node.dom){
				node.dom = React.createRef();
			}
			
			var childContent = null;
			
			if(node.content && node.content.length){
				childContent = this.renderNode(node.content, node);
			}else if(!this.isInline(node)){
				// Fake a <br> such that block elements still have some sort of height.
				childContent = this.renderNode({type:'br', props: {'rte-fake': 1}});
			}
			
			return <NodeType ref={node.dom} {...node.props}>{childContent}</NodeType>;
		}else{
			// Custom component
			var props = {...node.props, _rte: this};
	
			if(!node.dom){
				node.dom = React.createRef();
			}
			
			if(node.roots){
				var children = null;
				
				for(var k in node.roots){
					var root = node.roots[k];
					
					var isChildren = k == 'children';
					
					if(isChildren && NodeType.editable){
						root.dom = node.dom;
					}else if(!root.dom){
						root.dom = React.createRef();
					}
					
					var rendered;
					
					if(isChildren && NodeType.editable){
						rendered = this.renderNode(root.content);
					}else{
						rendered = <span ref={root.dom} contentEditable="true">{
							this.renderNode(
								(!root.content || !root.content.length) ? {type:'div', props: {
									contentEditable: false,
									className: 'rte-fake-root',
									onClick: e => {
										// We'll manually set the caret to actually be the parent
										var parent = this.getNode(e.target.parentNode, this.state.node);
										if(parent){
											this.setState({
												selection: {
													startNode: parent,
													endNode: parent,
													startOffset: 0,
													endOffset: 0
												}
											});
										}
									}
								}, content: [{type: 'br', props: {'rte-fake': 1}}]} : root.content
							)
						}</span>;
					}
					
					if(isChildren){
						children = rendered;
					}else{
						props[k] = rendered;
					}
				}
				
				// Note that you're either oneRoot or editable - can't be both. editable implies oneRoot which is directly editable (it has no additional wrappers).
				if(NodeType.oneRoot){
					props.contentEditable="false";
					props.rootRef = node.dom;
					return <ErrorCatcher node={node}><NodeType {...props}>{children}</NodeType></ErrorCatcher>;
				}else if(NodeType.editable){
					props.rootRef = node.dom;
					return <ErrorCatcher node={node}><NodeType {...props}>{children}</NodeType></ErrorCatcher>;
				}else{
					return <span ref={node.dom} module={node.typeName} contentEditable="false">
						<ErrorCatcher node={node}><NodeType {...props}>{children}</NodeType></ErrorCatcher>
					</span>;
				}
				
			}else{
				// It has no content inside it; it's purely config driven.
				// Either wrap it in a span (such that it only has exactly 1 DOM node, always), unless the module tells us it has one node anyway:
				
				// Note that you're either oneRoot or editable - can't be both. editable implies one root.
				if(NodeType.oneRoot){
					props.contentEditable="false";
					props.rootRef = node.dom;
					return <ErrorCatcher node={node}><NodeType {...props} /></ErrorCatcher>;
				}else if(NodeType.editable){
					props.rootRef = node.dom;
					return <ErrorCatcher node={node}><NodeType {...props} /></ErrorCatcher>;
				}else{
					return <span ref={node.dom} module={node.typeName} contentEditable="false">
						<ErrorCatcher node={node}><NodeType {...props} /></ErrorCatcher>
					</span>;
				}
			}
		}
	}
	
	isCustom(node){
		return (node && node.type && typeof node.type != 'string');
	}

	closeModal(){
		this.setState({
			selectOpenFor: null,
			optionsVisibleFor: null,
			rightClick: null
		});
		//this.updated();
	}

	
	
	
	render() {
		var {node, error, sourceMode} = this.state;
		
		if(error){
			sourceMode = {src: error.src};
		}
		
		var { toolbar, name } = this.props;
		
		if(sourceMode){
			return <div className="rich-editor with-toolbar">
				<div className="rte-toolbar">
					<button onClick={e => {
						e.preventDefault();
						this.setState({sourceMode: null});
						
						if(!this.ir){
							return;
						}
						
						// Grab the value and try loading it:
						var val = this.ir.onGetValue ? this.ir.onGetValue(null, this.ir) : this.ir.value;
						this.loadCanvas(val);
						this.addStateSnapshot();
						
					}}>Preview</button>
				</div>
				{error && <Alert type='error'>
					<p>
						<b>Uh oh!</b>
					</p>
					<p>
						This editor wasn't able to load a preview because your JSON is invalid.
					</p>
					<p>{error.e.message}</p>
				</Alert>}
				<Input inputRef={ir=>this.ir=ir} name={name} type="textarea" contentType="application/json" className="form-control json-preview" defaultValue={sourceMode.src} />
			</div>;
			
		}
		
		return <div className={"rich-editor " + (toolbar ? "with-toolbar" : "no-toolbar")} data-theme={"main"} onContextMenu={this.onContextMenu} onMouseDown={this.onMouseDown}>
			{toolbar && (<div className="rte-toolbar">
				{this.surroundButton('Bold', 'b', 'far fa-bold', null, true)}
				{this.surroundButton('Underline', 'u', 'far fa-underline', null, true)}
				{this.surroundButton('Italic', 'i', 'far fa-italic', null, true)}
				{this.surroundButton('Strike', 's', 'far fa-strikethrough', null, true)}
				{/*this.surroundButton('Link', 'UI/Link', 'far fa-link', null, true, null, {href:{content:[{type: TEXT, text:'https://www.example.com/'}]}})*/}
				<button 
					disabled={!this.canUndo()}
					title={`Undo`}
					onMouseDown={e => {
						// Prevent focus change
						e.preventDefault();
						
						this.undo();
					}}
					onMouseUp={e => e.preventDefault()}
					onClick={e => {
						// Prevent focus change
						e.preventDefault();
					}}
				>
				Undo
				</button>
				<button 
					disabled={!this.canRedo()}
					title={`Redo`}
					onMouseDown={e => {
						// Prevent focus change
						e.preventDefault();
						
						this.redo();
					}}
					onMouseUp={e => e.preventDefault()}
					onClick={e => {
						// Prevent focus change
						e.preventDefault();
					}}
				>
				Redo
				</button>
				{/*this.surroundButton('Token', 'UI/Token', 'far fa-brackets-curly', null, true)*/}
				{/*this.surroundButton('Quote', 'blockquote', 'far fa-quote-right', 'Quote')*/}
				{this.menuButton('Heading..', () => {
					return <>
						<div>{this.surroundButton('Set as header', 'h1', null, 'Heading')}</div>
						<div>{this.surroundButton('Set as header', 'h2', null, 'Heading 2')}</div>
						<div>{this.surroundButton('Set as header', 'h3', null, 'Heading 3')}</div>
						<div>{this.surroundButton('Set as header', 'h4', null, 'Heading 4')}</div>
						<div>{this.surroundButton('Set as header', 'h5', null, 'Heading 5')}</div>
						<div>{this.surroundButton('Set as header', 'h6', null, 'Heading 6')}</div>
					</>;
				})}
				{/*this.menuButton('List..', () => {
					return <>
						<div>{this.surroundButton('List', ['ul', 'li'], 'far fa-list-ul', 'Bullet point list')}</div>
						<div>{this.surroundButton('List', ['ol', 'li'], 'far fa-list-ol', 'Numbered list')}</div>
					</>;
				})*/}
				{/*this.props.modules && this.renderButton('Add something else', <i className={'fa fa-plus'} />, () => {
					// Show modal
					this.setState({ selectOpenFor: true })
				}, !this.state.selection)*/}
			</div>)}
			<div ref={node.dom} className="rte-content" contentEditable="true" 
				onKeyDown={this.onKeyDown} onDragStart={this.onReject} onBlur={this.onBlur} 
				onPaste={this.onPaste} onCopy={this.onCopy} onCut={this.onCut}>
				{this.renderNode(node.content)}
			</div>
			<input ref={ir=>{
				this.mainIr=ir;
				if(ir){
					ir.onGetValue=(val, ele)=>{
						if(ele == ir){
							return this.latestSnapshot.toCanvasJson(true);
						}
					};
				}
			}} name={name} type='hidden' />
			
			
				<div className="canvas-editor-popups" 
					onContextMenu={e => {
						e.preventDefault();
						return false;
				}}>
					{this.state.rightClick && this.renderContextMenu()}
					<ModuleSelector 
						closeModal = {() => this.closeModal()} 
						selectOpenFor = {this.state.selectOpenFor} 
						groups = {this.props.groups}
						onSelected = {module => {
							
							var {selectOpenFor, highlight} = this.state;
							var insertIndex;
						
							var insertInto = selectOpenFor.node.parent;
							var insertIntoRoot = false;
							if(!selectOpenFor.node.parent) {
								// This means we are likely the parent most object, so let's find what is active and place on it.
								insertInto = selectOpenFor.node;
								insertIntoRoot = true;
							}
							
							if(insertIntoRoot){
								insertIndex = insertInto.content.indexOf(highlight);
							}else if(insertInto.roots){
								insertIndex = insertInto.roots.children.content.indexOf(selectOpenFor.node);
							}else{
								insertIndex = insertInto.content.indexOf(selectOpenFor.node);
							}
							
							if(selectOpenFor.isAfter){
								insertIndex++;
							}
							
							var module = {
								typeName: module.publicName,
								type: module.moduleClass,
								parent: insertInto,
								props: {}
							}
							
							// Build the root set
							var roots = {};
							
							// Does the type have any roots that need adding?
							var rootSet = this.getRootInfo(module.type);

							for(var i=0;i<rootSet.length;i++){
								var rootInfo = rootSet[i];
								if(!roots[rootInfo.name]){
									// Note: these empty roots will be automatically rendered with a fake <br> in them so they have height.
									roots[rootInfo.name] = {t: 'span', content: []}
								}
							}
							
							if(Object.keys(roots).length){
								module.roots = roots;
							}
							
							console.log(module);
							
							this.addNode(module, insertInto, insertIndex);
							
							this.normalise();
							this.closeModal();
						}}
					/>
				</div>

		</div>;
	}
	
	menuButton(title, onOpen){
		var {openMenu} = this.state;
		var isActiveMenu = (openMenu == title);
		
		return <span className="rte-submenu-container">
			{this.renderButton(title, title, () => {
				this.setState({openMenu: isActiveMenu ? null : title});
			})}
			{isActiveMenu ? <div className="rte-submenu">{onOpen()}</div> : null}
		</span>;
	}
	
	surroundButton(title, types, icon, content, requireSelection, props, roots){
		if(!content){
			content = title;
		}
		var {active, selection} = this.state;
		
		// If there's no selection, then none are clickable.
		var disabled = !selection || (requireSelection && (selection.startNode == selection.endNode && selection.startOffset == selection.endOffset));
		
		var checkFor = types;
		
		if(Array.isArray(types)){
			// Inserting multiple at once.
			checkFor = types[0];
		}
		
		return <button 
			disabled={disabled}
			title={title}
			onMouseDown={e => {
				// Prevent focus change
				e.preventDefault();
				
				// Close any drops:
				this.setState({openMenu: null});
				
				this.applyWrap(this.state.selection, types, props, roots);
			}}
			onMouseUp={e => e.preventDefault()}
			className={active[checkFor] ? "active" : ""}
			onClick={e => {
				// Prevent focus change
				e.preventDefault();
			}}
		>
			{icon && <i className={icon}></i>}
			{!icon && content}
		</button>;
	}
	
	renderButton(title, content, onClick, disabled){
		return <button 
			disabled={disabled}
			title={title}
			onMouseDown={e => {
				// Prevent focus change
				e.preventDefault();
				onClick(e);
			}}
			onMouseUp={e => e.preventDefault()}
			onClick={e => {
				// Prevent focus change
				e.preventDefault();
			}}
		>
			{content}
		</button>;
	}
	
	/*
	* Gets the state node related to the given dom node. Must updateRefs before using this.
	*/
	getNode(domNode, check){
		if(Array.isArray(check)){
			for(var i=0;i<check.length;i++){
				var res = this.getNode(domNode, check[i]);
				if(res){
					return res;
				}
			}
			return;
		}
		
		if(check.dom && check.dom.current == domNode){
			return check;
		}
		
		if(check.content){
			var res = this.getNode(domNode, check.content);
			if(res){
				return res;
			}
		}
		
		if(check.roots){
			for(var k in check.roots){
				var root = check.roots[k];
				if(!root.dom.current){
					continue;
				}
				var res = this.getNode(domNode, root);
				if(res){
					return res;
				} 
			}
		}
		
		return;
	}
	
	updateRefs(node, dom){
		if(Array.isArray(node.content)){
			node.content.forEach((n,i) => {
				// 1:1 assumption:
				this.updateRefs(n, dom.childNodes[i]);
			});
		}
		
		// Make sure all DOM text nodes, roots and regular ele's are associated correctly.
		if(node.dom){
			node.dom.current = dom;
		}else{
			node.dom = {current: dom};
		}
		
		if(node.roots){
			for(var k in node.roots){
				var r = node.roots[k];
				
				if(r.dom.current){
					// Roots can actually be unmounted. This happens if a custom component conditionally renders them.
					// Note that if it is editable or oneRoot it must not conditionally render the root.
					
					// Always use the ref in this root. Don't use dom.
					// This is the "root" part of these fields - they act as independent roots in the dom from our point of view.
					this.updateRefs(r, r.dom.current);
				}
			}
		}
	}
	
	copyJson(){
		// Clone the tree:
		var {node, selection} = this.state;
		var {clonedNode, clonedSelection} = this.cloneNodeAndSelection(node, selection);
		
		// Get the set of all selected nodes:
		var selectedNodes = this.sliceSelection(clonedSelection, clonedNode);
		
		// Get start/ end again:
		var endCaretPosition = this.getCaretPosition(clonedSelection.endNode, clonedSelection.endOffset);
		var startCaretPosition = this.getCaretPosition(clonedSelection.startNode, clonedSelection.startOffset);
		
		// Mark each and their parents as keep, unless they're a block parent:
		selectedNodes.forEach(n => {
			n._keepChildren = true;
			while(n){
				n._keep = true;
				n = n.parent;
				
				if(n && n.parent && !this.isInline(n)){
					// Block parent. Only keep this if if started in the selection.
					if(n.caretStart < startCaretPosition || n.caretStart > endCaretPosition){
						
						// Remove it, but keep its child nodes.
						this.removeNode(n, clonedSelection, true);
					}
				}
				
			}
		});
		
		this.prune(clonedNode, clonedSelection, n => n._keepChildren ? 2 : (n._keep ? 1 : 0));
		
		var json = JSON.stringify(this.toCanvasFormat(clonedNode));
		return json;
	}
	
	prune(node, selection, filterFunc){ // filterFunc(Node) return true to keep.
		var retain = filterFunc(node);
		if(!retain){
			return false;
		}
		
		// Retain of 2 means don't check the children - we keep all of them anyway.
		if(retain == 1){
			if(node.content){
				for(var i=node.content.length - 1;i>=0;i--){
					var n = node.content[i];
					if(!this.prune(n, selection, filterFunc)){
						this.removeNode(n, selection);
					}
				}
			}
			
			if(node.roots){
				for(var k in node.roots){
					var r = node.roots[k];
					if(!this.prune(r, selection, filterFunc)){
						this.removeNode(r, selection);
					}
				}
			}
		}
		
		return true;
	}
	
	componentDidMount(){
		var {node} = this.state;
		this.updateRefs(node, node.dom.current);
		window.addEventListener("mouseup", this.onMouseUp);
	}
	
	componentWillUnmount(){
		window.removeEventListener("mouseup", this.onMouseUp);
	}
	
	/*
	* Gets dom node for the given state node. This is only valid after updateRefs.
	*/
	getDom(node){
		return node.dom ? node.dom.current : null;
	}
	
	componentDidUpdate() {
		var {node, sourceMode, selection} = this.state;
		
		if(sourceMode){
			return;
		}
		
		// Update all refs:
		var cur = node.dom.current;
		this.updateRefs(node, cur);
		
		// Relocate the cursor, if we need to.
		var domSelection = window.getSelection();
		
		if(!selection){
			// If domSelection is inside the editor, clear it.
			if(domSelection && domSelection.anchorNode && cur){
				if(this.isDomParentOf(cur, domSelection.anchorNode) || this.isDomParentOf(cur, domSelection.focusNode)){
					domSelection.removeAllRanges();
				}
			}
			return;
		}
		
		domSelection.removeAllRanges();
		
		var range = document.createRange();
		range.setStart(this.getDom(selection.startNode), selection.startOffset);
		range.setEnd(this.getDom(selection.endNode), selection.endOffset);
		domSelection.addRange(range);
	}
}

// TODO:

/*
* Bullet point lists are unstable
* Complex objects like Grid, as well as the 4 basic ones - xUI/Video, xUI/Link, xUI/Image, UI/Token
* Deletion of block nodes could be better. A <ul> or <ol> can get stuck in the editor, even though they are being selected. Try also: <p>text<p>|text</p></p> - it won't delete the paragraph.
* On Firefox, when deleting mixed text (bold, regular, strikethrough) if transitioning from a text type to regular when holding down the back button, it skips deletion of the first regular text char it sees.
	example - ab<b>c</b>    Given that value, holding backspace from the very end will result in 'b' being skipped over and 'c' and 'a' being deleted. 
* If the last character in the content editor is not content editable, you can not select the end to add text. such as the case with value='{"c":["a",{"t":"UI/Link","r":{"href":"testcomurl"},"c":["bc"]}]}'
	where Link's last's char is an uneditable ')'.
* Component editor - need more robust features such as url link etc. Rn its just basic string entry.
* Ability to add components is added and seems stable, but please use and provide feeback if broken! -Michael R

Later: 
* Handle e.g. page structures with nested custom components.
* Put caret at end of pasted content
* Shift+enter is not obvious that it has actually worked until you start typing.
* Pressing DEL key is very minimally handled at the moment
* The server must validate canvas JSON on save. It must check it contains only permitted types. As it's JSON and HTML nodes are never permitted to have data/ props, this validation can be non-allocating.
* When <Canvas> encounters a HTML node, it MUST ignore all props. Only custom elements in permitted modules are allowed to have props.
* The undo stack has no size limit atm!

Other todo from Rob's feedbacK:
* In the template and editor, you must be able to disable text input, ie a content area may only be made of images, so in a way a the text element is actually a component.
* for any given template layout the text features that are available must be configurable.  For example in blogs the body content may only allow H2,H3 and no font changes, However in the about page we may allow for H1-H5.
* Any one section of the site must be able to support a defined number of content types.  I can demonstrate this to you in NWS in Umbraco, it might also allow you to see where we need to end up
* Failure handling when json is messed up - offer to use older revisions if exist or to delete it.
* Failure handling when component is jacked up - offer to delete bad components that cause breaking error.
* Failure handling on crash - We need to store the session data on system crashes.
*/