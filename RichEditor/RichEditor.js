import Image from 'UI/Image';


/*
* Inline elements
*/
var inlineTypes = [
	'TEXT', 'a', 'abbr', 'acronym', 'b', 'bdo', 'big', 'br', 'button', 'cite', 'code', 'dfn', 'em', 'i', 'img', 'input', 'kbd', 'label', 
	'map', 'object', 'output', 'q', 'samp', 'select', 'small', 'span', 'strong', 'sub', 'sup', 'textarea', 'time', 'tt', 'var'
];

var inlines={};
inlineTypes.forEach(type => {
	inlines[type] = true;
});

/*
* Socialstack's RTE. Influenced by the UX of TinyMCE, and the code design of draft.js
*/
export default class RichEditor extends React.Component {
	
	constructor(props){
		super(props);
		this.onKeyDown = this.onKeyDown.bind(this);
		this.onMouseUp = this.onMouseUp.bind(this);
		this.onBlur = this.onBlur.bind(this);
		this.onPaste = this.onPaste.bind(this);
		
		var node = {
			content: [],
			dom: React.createRef()
		};
		
		this.state={
			node,
			active: {}
		};
		
		/*
		this.insertHtml('<b>Hello world!</b> This is not bold <h1>This is a heading.</h1>', true);
		
		console.log(Image);
		
		this.addNode({
			type: Image
		}, this.state.node);
		*/
	}
	
	insertHtml(html, avoidState){
		var {selection, node} = this.state;
		
		if(!selection){
			// Put caret at end of content.
			selection = {
				startNode: node,
				endNode: node,
				startOffset: node.content.length,
				endOffset: node.content.length
			};
		}
		
		if(selection.startOffset != selection.endOffset || selection.startNode == selection.endNode){
			// Overtyping - remove this content.
			this.deleteContent(selection);
		}
		
		// Parse it:
		var div = document.createElement('div');
		div.innerHTML = html;
		
		// Convert to nodes:
		var parent;
		var parentOffset;
		if(selection.startNode.type == 'TEXT'){
			// TODO: split text when pasting in.
			parent = selection.startNode.parent;
			parentOffset = parent.content.indexOf(selection.startNode) + 1;
		}else{
			parent = selection.startNode;
			parentOffset = selection.startOffset;
		}
		
		// TODO: avoid block elements being children of inline ones.
		// TODO: Pasting e.g. an <img> or other things that should preserve some or all of their attributes.
		
		var nodes = this.convertToNodes(div.childNodes, parent);
		
		var constructedContent = parent.content.slice(0, parentOffset).concat(nodes).concat(parent.content.slice(parentOffset));
		parent.content = constructedContent;
		
		if(!avoidState){
			this.setState({selection});
		}
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
					type: 'TEXT',
					text: node.nodeValue,
					parent
				});
				
			}else if(name != 'script' && name.length && name[0] != '#' && name.indexOf(':') == -1){ // Avoiding comments and garbage from Word.
				var n = {
					type: name
				};
				n.content = this.convertToNodes(node.childNodes, n);
				output.push(n);
			}
		}
		
		return output;
	}
	
	onMouseUp(e){
		// Must wait for next tick such that the window selection (default action) has occurred.
		setTimeout(() => this.updateSelection(), 1);
	}
	
	updateSelection(){
		// Updates our state selection based on wherever the browser elected to put the cursor.
		var domSelection = window.getSelection();
		
		var selection = {
			startOffset: domSelection.anchorOffset,
			startNode: this.getNode(domSelection.anchorNode),
			endOffset: domSelection.focusOffset,
			endNode: this.getNode(domSelection.focusNode)
		};
		
		var parents = this.getUniqueParents(selection);
		
		this.setState({selection, active: parents});
	}
	
	onPaste(e){
		e.preventDefault();
		var paste = (e.clipboardData || window.clipboardData).getData('text/html');
		this.insertHtml(paste);
	}
	
	onBlur(e){
		this.setState({selection: null});
	}
	
	onKeyDown(e){
		console.log(e);
		
		var {selection} = this.state;
		
		if(!selection || e.altKey){
			return;
		}
		
		var {startNode} = selection;
		
		// Skip modifier keys:
		if(
			(e.keyCode <= 47 && e.keyCode != 32 && e.keyCode != 21 && e.keyCode != 25) ||   // control chars (except spacebar, tab and enter)
			(e.keyCode >= 91 && e.keyCode <= 95) || // OS (windows button) keys
			(e.keyCode >= 112 && e.keyCode <= 151) // F keys
		){
			if(e.keyCode >= 35 && e.keyCode <= 40){
				// Arrow keys plus home/ end. Let the browser move the caret.
				setTimeout(() => this.updateSelection(), 1);
				return;
			}
			
			e.preventDefault();
			
			if(e.keyCode == 8){
				// Delete <-
				this.deleteSelectedContent();
				
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
					
					if(startNode.type == 'TEXT'){
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
					
					if(startNode.type == 'TEXT'){
						// Insert another text node, which might involve some part of the existing text node.
						var newText = {
							type: 'TEXT',
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
					var currentBlock = this.getBlock(selection.startNode);
					
					if(!currentBlock){
						// If previous element was not inside a block element, put it and any sibling inlines in a <p>, 
						// and we're then effectively duplicating that.
						currentBlock = {
							type: 'p'
						};
						
						var root = this.state.node;
						
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
						
						console.log(currentBlock, root);
					}
					
					var insertIndex = currentBlock.parent.content.indexOf(currentBlock) + 1;
					
					var newBlock = {
						type: currentBlock.type
					};
					
					if(startNode.type == 'TEXT'){
						// Insert another text node, which might involve some part of the existing text node.
						var newText = {
							type: 'TEXT',
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
				
				this.setState({selection});
				
			}else if(e.keyCode == 46){
				// Delete -> 
				this.deleteSelectedContent(1);
			}
			
			return;
		}
		
		if(e.ctrlKey){
			
			// Special case for formatting shortcuts.
			if(e.keyCode == 66){
				// ctrl+b
				e.preventDefault();
				this.applyInline(selection, 'b');
			}else if(e.keyCode == 73){
				// ctrl+i
				e.preventDefault();
				this.applyInline(selection, 'i');
			}else if(e.keyCode == 85){
				// ctrl+u
				e.preventDefault();
				this.applyInline(selection, 'u');
			}
			
			return;
		}
		
		e.preventDefault();
		
		// Replace selection (if it has anything in it) with the typed key.
		var key = e.key;
		
		if(selection.startOffset != selection.endOffset || startNode == selection.endNode){
			// Overtyping - remove this content.
			this.deleteContent(selection);
		}
		
		if(startNode.type != 'TEXT'){
			// Insert a text node at this location.
			var textNode = {type: 'TEXT', text: ''};
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
		
		this.setState({selection});
	}
	
	rootMostParent(node){
		var root = this.state.node;
		while(node){
			if(node.parent == root){
				return node;
			}
			node = node.parent;
		}
	}
	
	/*
	* Gets the node that is immediately before the given one (in the render order, not necessarily siblings). By default this ignores parents of the given node.
	* Optionally considers elements that are not a parent of node.
	*/
	getPrevious(node, includeParents){
		var prev = null;
		var found = 0;
		this.forEachNode(this.state.node, n => {
			if(found || n == node){
				found = 1;
				return;
			}
			
			if(!includeParents && this.isParentOf(n, node)){
				// Ignore
				return;
			}
			prev = n;
		});
		return prev;
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
	
	/*
	* Gets the node that is immediately after the given one (in the render order, not necessarily siblings).
	*/
	getNext(node){
		var next = null;
		var found = 0;
		this.forEachNode(this.state.node, n => {
			if(n == node){
				found = 1;
			}else if(found == 1){
				next = n;
				found = 2;
			}
		});
		return next;
	}
	
	/*
	* Nearest block element. Null if none.
	*/
	getBlock(node){
		var root = this.state.node;
		while(node){
			if(node != root && !this.isInline(node)){
				return node;
			}
			node = node.parent;
		}
	}
	
	isInline(node){
		return !!inlines[node.type];
	}
	
	deleteSelectedContent(forward){
		var {selection} = this.state;
		
		if(selection.startOffset == selection.endOffset && selection.startNode == selection.endNode){
			
			// No actual selection - select 1 "character", forward or back (if DEL or backspace).
			var node = selection.startNode;
			
			if(forward){
				// Might just be one character.
				if(node.type == 'TEXT' && selection.startOffset < node.text.length){
					// Yep!
					selection.endOffset = selection.startOffset + 1;
				}else{
					// Next will often be a block element. 
					// It's often the thing we're actually deleting, but we need to get its child-most node and put the selection at the start of it.
					// This is such that any inline elements are merged etc.
					var next = this.getNext(node);
					
					if(!next){
						// Hit del at end of content.
						return;
					}
					
					while(next.content && next.content.length){
						next = next.content[0];
					}
					
					selection.endNode = next;
					selection.endOffset = 0;
				}
			}else{
				// Might just be one character.
				if(node.type == 'TEXT' && selection.startOffset > 0){
					// Yep!
					selection.endOffset = selection.startOffset;
					selection.startOffset--;
				}else{
					// Prev will already be child most node, i.e. likely a text node. 
					// TODO: It may also be a parent of node in this case it would be wrong.
					// However the thing we are deleting is the break *between* these nodes, meaning selection start goes just after prev.
					var prev = this.getPrevious(node);
					if(!prev){
						// Backspace at start of the content.
						return;
					}
					selection.startNode = prev.parent;
					selection.startOffset = prev.parent.content.indexOf(prev) + 1;
				}
			}
			
		}
		
		this.deleteContent(selection);
		selection.endOffset = selection.startOffset;
		selection.endNode = selection.startNode;
		this.setState({selection});
	}
	
	/*
	* Useful for setting the active state of formatting buttons on the UI. 
	* Gets the unique parent nodes for the given selection (specifically, it's at the start of the selection).
	*/
	getUniqueParents(selection){
		var parents = {};
		var node = selection.startNode;
		while(node != null){
			if(node.type && node.type != 'TEXT'){
				parents[node.type] = true;
			}
			node = node.parent;
		}
		
		return parents;
	}
	
	/*
	* Wraps (or unwraps) the given selection in an inline ele of the given type, with the given props.
	* If there are any block elements in the selection, the inline element is effectively duplicated and inserted repeatedly.
	*/
	applyInline(selection, type, props){
		if(!selection){
			return;
		}
		var active = this.getUniqueParents(selection);
		
		if(active[type]){
			// It's already active - we'll want to remove it.
			this.removeInline(selection, type, props);
		}else{
			// Insert it.
			this.insertInline(selection, type, props);
		}
		
		// Update active state:
		active = this.getUniqueParents(selection);
		this.setState({active});
	}
	
	/* Unwraps selection with inline ele of given type. May end up generating multiple ele's. */
	removeInline(selection, type, props){
		
		var node = selection.startNode;
		
		if(node == selection.endNode){
			// Within the same node. Likely just all/ part of a text node.
			
		}else{
			// Entire tail of startNode, head of endNode, and some unknown number of nodes in between.
			// Nodes in between are wrapped per block node.
		}
		
	}
	
	/* Wraps selection with inline ele of given type. May end up generating multiple ele's. */
	insertInline(selection, type, props){
		
		var node = selection.startNode;
		
		if(node == selection.endNode){
			// Within the same node. Likely just all/ part of a text node.
			// startNode itself always ends up inside the new inline block.
			if(node.type == 'TEXT'){
				var parent = node.parent;
				var index = parent.content.indexOf(node);
				
				// Remove from parent:
				node.parent.content.splice(index, 1);
				
				var newInline = {type, props};
				this.addNode(newInline, parent, index);
				this.addNode(node, newInline);
				var textAfter = selection.endOffset != node.text.length;
				
				if(selection.startOffset){
					// Non-zero start offset - there's a new text node that gets inserted into node parent just before the new inline thing.
					this.addNode({
						type: 'TEXT',
						text: node.text.substring(0, selection.startOffset)
					}, parent, index);
					index++;
					node.text = node.text.substring(selection.startOffset);
					selection.endOffset -= selection.startOffset;
					selection.startOffset = 0;
				}
				
				if(textAfter){
					// Some content at the end that needs to be relocated to after the newly created inline element.
					this.addNode({
						type: 'TEXT',
						text: node.text.substring(selection.endOffset)
					}, parent, index + 1);
					node.text = node.text.substring(0, selection.endOffset);
				}
				
			}else{
				// the new inline ele becomes a child of this node.
				var toRelocate = this.content.splice(selection.startOffset, selection.endOffset - selection.startOffset);
				var newInline = {type, props, content: toRelocate};
				toRelocate.forEach(tr => tr.parent = newInline);
				this.addNode(toRelocate, selection.startNode, selection.startOffset);
				selection.startNode = selection.endNode = toRelocate;
				selection.startOffset = 0;
				selection.endOffset = toRelocate.length;
			}
			
		}else{
			// Entire tail of startNode, head of endNode, and some unknown number of nodes in between.
			// Nodes in between are wrapped per block node.
		}
		
	}
	
	deleteContent(selection){
		// Delete everything from start -> end
		var node = selection.startNode;
		
		// In case we delete start itself, get previous node:
		var prev = this.getPrevious(node);
		
		if(node == selection.endNode){
			// Within same node. Likely just a substr on some text.
			if(selection.startOffset == selection.endOffset){
				// If they are the same, nothing happens.
				return;
			}
			
			if(this.deleteFromNode(node, selection.startOffset, selection.endOffset)){
				// Node itself was deleted.
				this.relocateStart(selection, prev);
			}
		}else{
			// Entire tail of startNode, head of endNode, and some unknown number of nodes in between.
			var foundStart = false;
			var foundEnd = false;
			var toDelete = [];
			
			this.forEachNode(this.state.node, n => {
				
				if(n == selection.endNode){
					foundEnd = true;
				}
				
				if(foundStart && !foundEnd){
					toDelete.push(n);
				}
				
				if(n == node){
					foundStart = true;
				}
				
			});
			
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
			
			toDelete.map(node => this.removeNode(node));
			
			var startParent = node.parent;
			
			toMerge.map(node => {
				if(node.content && node.content.length){
					// These elements are already in render order, so can be directly merged in:
					node.content.map(c => c.parent = startParent);
					startParent.content = startParent.content.concat(node.content);
				}
			});
			
			// Tail and head (must happen after the above complete nodes, just in case this removes the start/end entirely):
			
			if(this.deleteFromNode(node, selection.startOffset, -1)){
				// Start node was just deleted.
				// Select the end of the previous node, or just the root if there isn't one.
				this.relocateStart(selection, prev);
				node = selection.startNode;
			}
			
			if(!this.deleteFromNode(selection.endNode, 0, selection.endOffset)){
				
				// May now have 2 text nodes as direct siblings (start and what's left of end).
				this.tidyTextNodes(selection);
				
			}
		}
	}
	
	tidyTextNodes(selection){
		var start = selection.startNode;
		var end = selection.endNode;
		if(start.type == 'TEXT' && end.type == 'TEXT' && start.parent == end.parent){
			var endIndex = end.parent.content.indexOf(end);
			var startIndex = start.parent.content.indexOf(start);
			
			if(startIndex == endIndex - 1){
				// Merge these 2 text nodes:
				start.text += end.text;
				this.removeNode(end);
				selection.endNode = start;
				selection.endOffset += start.text.length;
			}
		}
	}
	
	relocateStart(selection, prev){
		if(!prev || !prev.parent){
			selection.startNode = this.state.node;
			selection.startOffset = 0;
		}else if(prev.type == 'TEXT'){
			selection.startNode = prev;
			selection.startOffset = prev.text.length;
		}else{
			selection.startNode = prev.parent;
			selection.startOffset = prev.parent.content.indexOf(prev) + 1;
		}
	}
	
	forEachNode(start, cb){
		cb(start);
		if(start.content){
			for(var i=0;i<start.content.length;i++){
				var child = start.content[i];
				this.forEachNode(child, cb);
			}
		}
	}
	
	deleteFromNode(node, start, end){
		if(node.type == "TEXT") {
			node.text = node.text.substring(0, start) + (end == -1 ? '' : node.text.substring(end));
			if(!node.text){
				// Remove from parent
				this.removeNode(node);
				return true;
			}
		}else if(node.content) {
			// Offsets are relative to the content array in this node.
			var a = node.content;
			var res = a.slice(0, start);
			node.content = end == -1 ? res : res.concat(a.slice(end));
			
			if(!node.content.length){
				// Remove from parent
				this.removeNode(node);
				return true;
			}
		}
	}
	
	addNode(node, parent, index){
		if(!parent.content){
			parent.content = [];
		}
		
		if(index === undefined){
			parent.content.push(node);
		}else{
			// Insert at given index:
			parent.content.splice(index,0,node);
		}
		node.parent = parent;
	}
	
	removeNode(node){
		if(!node.parent){
			// It's the root!
			return;
		}
		
		node.parent.content = node.parent.content.filter(n => n!=node);
		
		if(!node.parent.content.length){
			// Keep the tree nice and tidy:
			this.removeNode(node.parent);
		}
	}
	
	renderNode(node, parent){
		if(Array.isArray(node)){
			return node.map((n,i) => this.renderNode(n, parent));
		}
		
		var NodeType = node.type;
		
		if(NodeType == 'TEXT'){
			return node.text;
		}else if(typeof NodeType === 'string'){
			if(!node.dom){
				node.dom = React.createRef();
			}
			
			return <NodeType ref={node.dom} {...node.props}>{node.content ? this.renderNode(node.content, node) : null}</NodeType>;
		}else{
			// Custom component
			if(!node.dom){
				node.dom = React.createRef();
			}
			
			return <span ref={node.dom}>
				<NodeType ref={node.dom} {...node.props}>{node.content ? this.renderNode(node.content, node) : null}</NodeType>
			</span>;
		}
	}
	
	render() {
		var {node} = this.state;
		return <div className="rich-editor">
			<div className="rte-toolbar">
				{this.surroundButton('Bold', 'b')}
				{this.surroundButton('Underline', 'u')}
				{this.surroundButton('Italic', 'i')}
				{this.surroundButton('Set as header', 'h1', <i className='fa fa-heading' />)}
				{this.renderButton('fa fa-plus', 'Add something', () => {
					// Show modal
					console.log("Show modal");
				})}
			</div>
			<div ref={node.dom} className="rte-content" contentEditable={true} onKeyDown={this.onKeyDown} onMouseUp={this.onMouseUp} onBlur={this.onBlur} onPaste={this.onPaste}>
				{this.renderNode(node.content)}
			</div>
		</div>;
	}
	
	surroundButton(title, type, content){
		if(!content){
			content = title;
		}
		var {active} = this.state;
		return <button 
			title={title}
			onMouseDown={e => {
				// Prevent focus change
				e.preventDefault();
				
				this.applyInline(this.state.selection, type);
			}}
			onMouseUp={e => e.preventDefault()}
			className={active[type] ? "active" : ""}
			onClick={e => {
				// Prevent focus change
				e.preventDefault();
			}}
		>
		{content}
		</button>;
	}
	
	renderButton(icon, title, onClick){
		return <button 
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
			<i className={icon} />
		</button>;
	}
	
	/*
	* Gets the state node related to the given dom node.
	*/
	getNode(domNode, check){
		if(!check){
			check = this.state.node;
		}
		
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
			return this.getNode(domNode, check.content);
		}
	}
	
	updateRefs(node, dom){
		if(!node){
			node = this.state.node;
			dom = node.dom.current;
		}
		
		if(Array.isArray(node.content)){
			node.content.forEach((n,i) => {
				// Basic 1:1 assumption.
				this.updateRefs(n, dom.childNodes[i]);
			});
			return;
		}
		
		// Make sure all DOM nodes are associated correctly.
		if(!node.dom){
			node.dom = {current: dom};
		}else if(node.type == 'TEXT'){
			node.dom.current = dom;
		}
		
	}
	
	componentDidMount(){
		this.updateRefs();
		console.log("Updated refs", this.state.node);
	}
	
	/*
	* Gets dom node for the given state node. This is only valid in DidUpdate or DidMount.
	*/
	getDom(node){
		return node.dom ? node.dom.current : null;
	}
	
	componentDidUpdate() {
		this.updateRefs();
		console.log("Updated refs", this.state.node);
		global.thing = this.state.node;
		// Locate the cursor, if we need to.
		var {selection} = this.state;
		
		if(!selection){
			return;
		}
		
		var domSelection = window.getSelection();
		domSelection.removeAllRanges();
		
		var range = document.createRange();
		range.setStart(this.getDom(selection.startNode), selection.startOffset);
		range.setEnd(this.getDom(selection.endNode), selection.endOffset);
		domSelection.addRange(range);
	}
}


// TODO:

/*
* Overtyping a whole thing deletes everything, because the parent node is briefly empty
* Deletes need to update 'active' state (such that UI buttons reflect what's currently on the UI).
* Parse a canvas, including HTML into the editors internal tree representation.
* Deletes whole formatted node when it's at the start of the text (or start of block?). More broadly delete is wonky!
* Too keen to delete empty blocks. Don't delete a parent block element if it is not included in the selection. "test\nte" then delete the "e" and the "t". The \n will go too.
* Related: If generating a new empty block element, put a fake <br> in it until some text is entered. This ensures it has some height and is therefore visible to the caret.

backwards selections
undo/ redo
handle paste
generic components which are *not* contenteditable

Clicking *SOME* block element button on the UI (header buttons for ex) changes the type of the nearest block element to current selection.

Clicking an inline element button on the UI (italic for ex, or even a link) - if something selected, will analyse all block elements and insert the style *into all of them*.
*/
