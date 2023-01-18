import { getRootInfo } from './Utils';
import Graph from 'UI/Functions/GraphRuntime/Graph';
import Draft from 'Admin/CanvasEditor/DraftJs/Draft.min.js';
const { EditorState, convertFromHTML, ContentState, convertToRaw } = Draft;


export default class CanvasState{
	
	constructor(){
		this.propertyTab = 1;
		this.showRightPanel = true;
		this.showLeftPanel = true;
	}
	
	load(value){
		this.rootRef = this.rootRef || React.createRef();
		
		if(value){
			this.loadCanvas(value, 1);
		}
		
		if(!this.node){
			this.node = {
				content: [],
				dom: this.rootRef
			};
		}
		
		// If initial root state is an empty array, insert a paragraph.
		var rt = this.node;
		
		if(Array.isArray(rt.content) && !rt.content.length){
			var rootPara = {type: 'richtext', editorState: EditorState.createEmpty()};
			rootPara.parent = rt;
			rt.content.push(rootPara);
		}
		
		// Initial snapshot:
		return this.addStateSnapshot();
	}
	
	normalise(isMinorState){
		return this.addStateSnapshot(isMinorState, this.node, this.selection);
	}
	
	loadCanvas(json, init){
		try{
			var root = JSON.parse(json);
			
			var convertedRoot = this.convertToNodesFromCanvas(root);
			
			if(convertedRoot.graph){
				// Can't have a graph as a root node.
				convertedRoot = {content: [convertedRoot]};
			}
			
			if(!convertedRoot.type && !convertedRoot.content && !convertedRoot.graph){
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
			
			convertedRoot.dom = this.rootRef;
			this.node = convertedRoot;
			this.error = null;
			
		}catch(e){
			console.error(e);
			this.error = {e, src: json};
		}
	}
	
	loadCanvasChildren(node, result){
		var c = node.c;
		if(typeof c == 'string'){
			// It has one child which is a text node (no ID or templateID on this).
			var text = {type: 'richtext', editorState: this.loadRteState(c, true), parent: result};
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
					child = {type: 'richtext', editorState: this.loadRteState(child, true), parent: result};
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
	
	loadRteState(html, plain){
		// Plain is true if html is actually text that should be treated as-is.
		if(plain){
			// Ensure any < etc in the text are encoded.
			var p = document.createElement("p");
			p.textContent = html;
			html = p.innerHTML;
		}
		
		var blocksFromHTML = convertFromHTML(html);

		var state = ContentState.createFromBlockArray(
			blocksFromHTML.contentBlocks,
			blocksFromHTML.entityMap,
		);
	
		return EditorState.createWithContent(
			state,
			// decorator,
		);
	}
	
	selectNode(node){
		var snap = this.addStateSnapshot();
		snap.selectedNode = node;
		snap.propertyTab = node ? 2:1;
		return snap;
	}
	
	setThemeState(state){
		var snap = this.addStateSnapshot();
		if(state){
			snap.graphState = false;
		}
		snap.themeState = state;
		return snap;
	}
	
	setGraphState(state){
		var snap = this.addStateSnapshot();
		if(state){
			snap.themeState = false;
		}
		snap.graphState = state;
		return snap;
	}
	
	setShowSourceState(state){
		var snap = this.addStateSnapshot();
		snap.sourceMode = state;
		return snap;
	}
	
	changePropertyTab(tab){
		var snap = this.addStateSnapshot();
		snap.propertyTab = tab;
		return snap;
	}
	
	changeLeftPanel(state){
		var snap = this.addStateSnapshot();
		snap.showLeftPanel = state;
		return snap;
	}
	
	changeRightPanel(state){
		var snap = this.addStateSnapshot();
		snap.showRightPanel = state;
		return snap;
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
		
		if(node.g){
			// It's a graph node.
			result.graph = new Graph(node.g);
			
			// It can also have child nodes:
			result.content = [];
			if(node.c){
				this.loadCanvasChildren(node, result);
			}
			
			return result;
		}
		
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
							var resultRoot = {};
							this.loadCanvasChildren(n, resultRoot);
							roots[i + ''] = resultRoot;
						})
					}else{
						for(var key in node.r){
							var resultRoot = {};
							this.loadCanvasChildren(node.r[key], resultRoot);
							roots[key] = resultRoot;
						}
					}
				}
				
				// Does the type have any roots that need adding?
				var rootSet = getRootInfo(result.type);
				
				for(var i=0;i<rootSet.length;i++){
					var rootInfo = rootSet[i];
					if(!roots[rootInfo.name]){
						// Note: Empty roots are given an RTE a little further down.
						roots[rootInfo.name] = {};
					}
				}
				
				if(node.c){
					// Simplified case for a common scenario of the node just having children only in it.
					// Wrap it in a root node and set it as roots.children.
					var resultRoot = {};
					this.loadCanvasChildren(node, resultRoot);
					roots.children = resultRoot;
				}else if(node.content){
					// Canvas 1 (depr)
					
					var converted = this.convertToNodesFromCanvas({type: 'span', content: node.content});
					
					var resultRoot = {};
					roots.children = resultRoot;
					resultRoot.content = converted.content;
				}
				
				for(var k in roots){
					// Indicate it is a root node by ensuring the type is blank and add a dom ref/ parent:
					var root = roots[k];
					root.type = null;
					root.parent = result;
					root.dom = React.createRef();
					
					// Must always be an aray as the root content.
					if(root.content === null || root.content === undefined){
						root.content = [];
					}else if(!Array.isArray(root.content)){
						root.content = [root.content];
					}
					
					// If root content is empty, insert an RTE.
					if(!root.content.length){
						var rootPara = {type: 'richtext', editorState: EditorState.createEmpty()};
						rootPara.parent = root;
						root.content.push(rootPara);
					}
				}
				
				result.roots = roots;
				
			}else{
				// HTML node.
				result.type = 'richtext';
				
				// Flatten to html string (for now).
				result.editorState = this.editorStateFromHtmlNode(node);
				
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
			// String (text node). Does not have any child nodes.
			result.editorState = this.loadRteState(node.s, true);
			result.type = 'richtext';
		}
		
		return result;
	}
	
	editorStateFromHtmlNode(node){
		var html = this.convertToHtmlFromCanvas(node);
		return this.loadRteState(html);
	}
	
	convertToHtmlFromCanvas(node, canvas2) {
		if(typeof node === 'string'){
			if(canvas2){
				// Encode html entities.
				var p = document.createElement("p");
				p.textContent = node;
				return p.innerHTML;
			}else{
				return node;
			}
		}
		
		var type = node.type || node.t;
		
		if (!type) {
			return '';
		}

		if (type == '#text') {
			return node.text;
		}
		
		var str = '<' + type + '>';

		if (Array.isArray(node.c)) {

			node.c.forEach(n => {
				str += this.convertToHtmlFromCanvas(n);
			});
		} else if (node.c) {
			str += this.convertToHtmlFromCanvas(node.c, true);
		} else if(node.s) {
			// String content
			str += this.convertToHtmlFromCanvas(node.s, true);
		} else if( node.content ) {
			
			// Canvas 1.
			var content = node.content;
			
			if(!Array.isArray(content)){
				content = [content];
			}
			
			content.forEach(child => {
				
				if(typeof child == 'string'){
					// HTML as-is:
					str += child;
				}else if(child.s){
					str += this.convertToHtmlFromCanvas(child.s, true);
				}else if(!child.type && !child.t){
					// typeless nodes from C1 can have an array of content in them which should be effectively merged in.
					if(child.content){
						child.content.forEach(c1n => {
							str += this.convertToHtmlFromCanvas(c1n);
						});
					}
				}else{
					str += this.convertToHtmlFromCanvas(child);
				}
				
			});
			
		}

		return str + '</' + type + '>';
	}
	
	toCanvasFromRawRTE(raw){
		// Raw contains a list of blocks. These will generally be "unstyled" blocks, 
		// meaning they are some text with possible inline styles on them, but then simply followed by a br.
		var blocks = raw.blocks;
		var children = [];
		
		for(var i=0;i<blocks.length;i++){
			var block = blocks[i];
			
			// Convert the block:
			var blockNode = this.toTreeFromRTE(block);
			
			children.push(blockNode);
		}
		
		// If there is only 1 child, return that.
		if(children.length == 1){
			return children[0];
		}
		
		return {t: 'div', c: children};
	}
	
	toTreeFromRTE(block){
		
		var blockType = 'div';
		
		if(block.type != 'unstyled'){
			
			switch(block.type){
				case 'header-one':
					blockType = 'h1';
				break;
				case 'header-two':
					blockType = 'h2';
				break;
				case 'header-three':
					blockType = 'h3';
				break;
				case 'header-four':
					blockType = 'h4';
				break;
				case 'header-five':
					blockType = 'h5';
				break;
				case 'header-six':
					blockType = 'h6';
				break;
				case 'blockquote':
					blockType = 'blockquote';
				break;
				case 'unordered-list-item':
					blockType = 'ul';
				break;
				case 'ordered-list-item':
					blockType = 'ol';
				break;
				case 'paragraph':
				default:
					blockType = 'p';
				break;
			}
			
		}
		
		var text = block.text;
		var inlines = block.inlineStyleRanges;
		
		if(inlines && inlines.length){
			
			// Sort them by length:
			inlines.forEach(isr => {
				isr.rangeEnd = isr.offset + isr.length;
			});
			
			inlines.sort((a,b) => a.length - b.length);
			
			var chars = [];
			chars.length = text.length;
			
			var styleMap = {
				'BOLD': {tag:'b'},
				'ITALIC': {tag:'i'},
				'UNDERLINE':{tag:'u'},
				'CODE':{tag:'code'}
			};
			
			inlines.forEach(isr => {
				var start = isr.offset;
				var rangeType = styleMap[isr.style];
				
				if(!rangeType){
					console.log("Range type not found: ", isr);
					return;
				}
				
				for(var i=0;i<isr.length;i++){
					var charOffset = start + i;
					
					if(!chars[charOffset]){
						chars[charOffset] = [rangeType];
					}else{
						chars[charOffset].push(rangeType);
					}
				}
			});
			
			// chars now contains stacks of each of the range. If two neighbouring stacks are different, we are adding/ removing some number of open/ close tags.
			
			var currentNode = null;
			var rootNode = {};
			var currentStack = [];
			var empty = [];
			var previousStack = [];
			
			for(var i=0;i<text.length;i++){
				
				var styleStack = chars[i];
				
				if(!styleStack){
					styleStack = empty;
				}
				
				// Compare styleStack to previous stack.
				var maxHeight = styleStack.length > previousStack.length ? styleStack.length : previousStack.length;
				var diffAt = -1;
				
				for(var h=0;h<maxHeight;h++){
					var prevStyle = h >=previousStack.length ? null : previousStack[h];
					var curStyle = h >=styleStack.length ? null : styleStack[h];
					
					if(prevStyle != curStyle){
						// Closing a tag, opening a tag, or both.
						diffAt = h;
					}
				}
				
				if(diffAt != -1){
					// Close all tags that are at diffAt and above:
					for(var h=diffAt;h<previousStack.length;h++){
						currentStack.pop();
					}
					
					// Open the new ones:
					for(var h=diffAt;h<styleStack.length;h++){
						var newNode = {t: styleStack[h].tag};
						
						var parentNode = currentStack.length ? currentStack[currentStack.length - 1] : rootNode;
						
						if(!parentNode.c){
							parentNode.c = [];
						}else if(typeof parentNode.c === 'string'){
							parentNode.c = [parentNode.c];
						}
						
						parentNode.c.push(newNode);
						currentStack.push(newNode);
					}
				}
				
				// The letter goes in the node at the current top of the stack.
				var parentNode = currentStack.length ? currentStack[currentStack.length - 1] : rootNode;
				
				if(parentNode.c === undefined){
					parentNode.c = text[i];
				}else if(typeof parentNode.c === 'string'){
					parentNode.c += text[i];
				}else{
					// It's an array. Is the last one text?
					if(!parentNode.c.length){
						parentNode.c.push(text[i]);
					}else{
						var last = parentNode.c[parentNode.c.length - 1];
						
						if(typeof last === 'string'){
							parentNode.c[parentNode.c.length - 1] = last + text[i];
						}else{
							// It was some object. Need to push the letter as a new string.
							parentNode.c.push(text[i]);
						}
					}
				}
				
				previousStack = styleStack;
			}
			
			return {t: blockType, c:rootNode.c};
			
		}else{
			// Just a raw text only node.
			return {t: blockType, c:text};
		}
		
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
		
		if(node.type == 'richtext'){
			
			var cc = node.editorState.getCurrentContent();
			var raw = convertToRaw(cc);
			resultNode = this.toCanvasFromRawRTE(raw);
			attribs = true;
		}else{
			if(node.graph){
				resultNode.g = node.graph.structure;
				attribs = true;
			}else if(node.type){
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
			
			if((!node.parent || typeof node.type == 'string' || node.graph) && node.content && node.content.length){
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
	
	deepClone(node, selection, clonedSelection){
		if(!node){
			return null;
		}
		var result = {
			graph: node.graph,
			type: node.type,
			typeName: node.typeName,
			text: node.text,
			editorState: node.editorState,
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
	
	toCanvasJson(pretty, withoutIds){
		var options = {};
		var snapshot = this.latestSnapshot;
		
		if(!withoutIds){
			// Add IDs in the output if they don't already have them.
			options.id = this.getMaxId(snapshot.node, 0) || 1;
		}
		
		var cfNode = this.toCanvasFormat(snapshot.node, options);
		
		if(!cfNode){
			return '';
		}
		
		var json = JSON.stringify(cfNode, null, pretty ? '\t' : null);
		return json;
	}
	
	addStateSnapshot(isMinor, node, selection){
		
		if(!node){
			// Using the same node and selection as the ones in the state here:
			node = this.node;
			selection = this.selection;
		}
		
		// Clone them:
		var {clonedNode, clonedSelection} = this.cloneNodeAndSelection(node, selection);
		
		// Build the snapshot object:
		var snapshot = {
			isMinor,
			time: Date.now(),
			node: clonedNode,
			selection: clonedSelection,
			restore: () => {
				// Snapshots must be cloned on restore. Otherwise if you edited it and then tried to go 
				// back/ forward to the same snapshot, it will have been modified.
				
				var selection = {
					startOffset: snapshot.selection.startOffset,
					endOffset: snapshot.selection.endOffset
				};
				
				var node = this.deepClone(snapshot.node, snapshot.selection, selection);
				
				// Use root dom ref:
				node.dom = this.node.dom;
				snapshot.time = Date.now();
				
				this.setState({node, selection});
			}
		};
		
		var latest = this.latestSnapshot;
		snapshot.previous = this;
		
		// If the latest snapshot is also minor, it just replaces it if the time diff is short enough.
		if(isMinor && latest && latest.isMinor && (snapshot.time - latest.time) < 500){
			// Replace it.
			snapshot.previous = latest.previous;
		}
		
		var newState = new CanvasState();
		newState.rootRef = this.rootRef;
		newState.selectedNode = this.selectedNode;
		newState.graphState = this.graphState;
		newState.themeState = this.themeState;
		newState.node = node;
		newState.selection = selection;
		newState.showLeftPanel = this.showLeftPanel;
		newState.showRightPanel = this.showRightPanel;
		newState.sourceMode = this.sourceMode;
		newState.redoSnapshot = null;
		newState.latestSnapshot = snapshot;
		newState.propertyTab = this.propertyTab;
		return newState;
	}
	
	// Note: after removing one or more nodes, you must snapshot the state.
	// It is this way just in case you are making multiple changes in one user action.
	removeNode(node, retainChildren){
		if(!node.parent || !node.type){
			// It's a root. Just empty it instead via putting an RTE in there.
			var rootPara = {type: 'richtext', editorState: EditorState.createEmpty()};
			node.content = [rootPara];
			rootPara.parent = node;
			return;
		}
		
		var index = node.parent.content.indexOf(node);
		
		if(retainChildren){
			if(retainChildren == 2){
				// Kids are merged into neighbour.
				var neighbour = node.parent.content[index - 1];
				
				if(neighbour.type == TEXT){
					
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
				}
				
				// Remove from parent:
				node.parent.content.splice(index, 1);
			}else{
				// kids are merged into parent
				
				node.parent.content = node.parent.content.slice(0, index).concat(node.content || []).concat(node.parent.content.slice(index + 1));
				node.content.forEach(child => child.parent = node.parent);
			}
			
		}else{
			// Remove from parent:
			node.parent.content.splice(index, 1);
		}
		
	}
	
	// Note: after adding one or more nodes, you must snapshot the state.
	// It is this way just in case you are making multiple changes in one user action.
	addNode(node, parent, index, isReplace){
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
			parent.content.splice(index,isReplace ? 1 : 0,node);
		}
		node.parent = parent;
		return node;
	}
	
}