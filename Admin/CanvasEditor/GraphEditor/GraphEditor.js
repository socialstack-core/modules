import Input from 'UI/Input';
import Loading from 'UI/Loading';
import MapInteraction from './Map';
import { colorAsHsl, typeCompatibility, getType, isDefaultType } from './Types';
import TypeIcon from './TypeIcon';
import SelectNodeType from './SelectNodeType';
import {niceName} from './Utils';
import {getAllContentTypes} from 'Admin/Functions/GetAutoForm';
import Modal from 'UI/Modal';

var defaultNamespace = 'Admin/CanvasEditor/GraphEditor/NodeSet/';

var dragging;

// Connect the input "ontypecanvas" render event:
var inputTypes = global.inputTypes = global.inputTypes || {};

// type="graph"
inputTypes.ontypegraph = function(props, _this){
	return <>
		<GraphEditor context={props.context} inputRef={props.inputRef} name={props.name} value={props.value} 
		objectOutput={props.objectOutput} namespace={props.namespace || props.ns}
		onChange={props.onChange}
		/>
	</>;
};

function saveNodes(nodeSet, namespace) {
	// Set node index
	nodeSet.forEach((node,i) => {
		node.arrayIndex = i;
	});
	
	var lcNs = namespace.toLowerCase();
	
	var graphContent = {
		c: nodeSet.map(node => {
			var nodeJson = node.toJson();
			
			if(nodeJson.t){
				if(namespace){
					// Common prefix is truncated:
					if(nodeJson.t.toLowerCase().indexOf(lcNs) === 0){
						nodeJson.t = nodeJson.t.substring(namespace.length);
					}
				}
				
				// Admin/ is truncated:
				if(nodeJson.t.toLowerCase().indexOf('admin/') === 0){
					nodeJson.t = nodeJson.t.substring(6);
				}
			}
			
			return nodeJson;
		})
	};
	
	return graphContent;
};

function setRootNode(node, nodeSet){
	// Clear everything else
	nodeSet.forEach((node,i) => {
		delete node.root;
	});
	
	node.root = true;
}

export default function GraphEditor(props){
	var [nodes, setNodesIntl] = React.useState(null);
	var [graph, setGraph] = React.useState(null);
	var [showConfirmModal, setShowConfirmModal] = React.useState(false);
	var [cantDeleteModal, setCantDeleteModal] = React.useState(false);
	
	var setNodes = (nodes) => {
		if(!graph){
			graph = {nodes};
			setGraph(graph);
		}else{
			graph.nodes = nodes;
		}
		
		nodes.forEach(node => {
			if(node){
				node.graph = graph;
			}
		});
		
		setNodesIntl(nodes);
	};
	
	React.useEffect(() => {
		
		var val = props.value || props.defaultValue;
		
		if(val){
			// todo: indicate loading state.
			
			// Ensure all autoforms are loaded or loading:
			getAllContentTypes().then(() => {
				
				var g = {};
				
				// Load the nodes:
				loadNodes(val, g).then(newNodes => {
					setGraph(g);
					setNodesIntl(newNodes);
				});
				
			});
			
		}else{
			setNodes([]);
		}
		
	}, [props.value, props.defaultValue]);
	
	var loadNodes = async (graphJson, graph) => {
		
		if(typeof graphJson === 'string'){
			graphJson = JSON.parse(graphJson);
		}
		
		if(Array.isArray(graphJson)){
			graphJson = {c: graphJson};
		}
		
		if(!graphJson || !graphJson.c){
			// Empty node set.
			return [];
		}
		
		if(!Array.isArray(graphJson.c)){
			graphJson.c = [graphJson.c];
		}
		
		var nodes = [];
		
		// Admin namespace:
		var ns = props.namespace || defaultNamespace;
		
		// The index in this array is used to establish links.
		// The convention is also that the first entry in the array is the result/ output node.
		for(var i=0;i<graphJson.c.length;i++){
			var nodeJson = graphJson.c[i];
			
			// Initial node expansion by constructing them.
			var nodeType = nodeJson.t;
			
			if(nodeType.indexOf('/') == -1){
				// Unprefixed node - add the common prefix:
				nodeType = ns + nodeType;
			}
			
			var NodeType = require(nodeType).default;
			
			if(!NodeType){
				throw new Error("Unable to load graph due to missing node type: " + nodeType);
			}
			
			// instance the node:
			var node = new NodeType({offsetX: nodeJson.x || 0, offsetY: nodeJson.y || 0});
			node.graph = graph;
			
			// Construct the data state; links will come later:
			if(nodeJson.d){
				for(var key in nodeJson.d){
					node.state[key] = nodeJson.d[key];
				}
			}
			
			if(nodeJson.r){
				node.root = true;
			}
			
			nodes.push(node);
		}
		
		graph.nodes = nodes;
		
		// Now the nodes are constructed, we can now load the links.
		for(var i=0;i<graphJson.c.length;i++){
			var nodeJson = graphJson.c[i];
			var node = nodes[i];
			
			if(nodeJson.l){
				// It has links - load each one:
				for(var key in nodeJson.l){
					var link = nodeJson.l[key];
					
					if(link && link.n >= 0 && link.n < nodes.length){
						var srcNode = nodes[link.n];
						
						// Add the link:
						node.state[key] = {link: true, node: srcNode, field: link.f};
					}
				}
			}
		}
		
		// Finally, loop a third time to validate the state of every node.
		for(var i=0;i<nodes.length;i++){
			var node = nodes[i];
			
			// Set initial context:
			node.context = props.context;
			
			// Validate the state:
			await node.validateState();
		}
		
		return nodes;
	};
	
	var changed = (newNodes) => {
		// Update the state:
		setNodes(newNodes);
		
		// Tell any props about it:
		props.onChange && props.onChange({
			target:{},
			getValue:() => {
				saveNodes(newNodes, props.namespace || defaultNamespace);
			}
		});
	};
	
	if(!nodes){
		return null;
	}
	
	return <>
		<GraphEditorCore nodes={nodes} onSave={() => {
			var json = saveNodes(nodes, props.namespace || defaultNamespace);

			console.log(JSON.stringify(json));
		}} context={props.context} onAddNode={node => {
			var newNodes = [...nodes];
			node._redraw = 1;
			newNodes.push(node);

			if (newNodes.length == 1) {
				// Mark it as the root node:
				node.root = true;
			}

			changed(newNodes);
		}} removeNode={node => {

			node.deleted = true;
			var newNodes = nodes.filter(n => n != node);

			// Ensure any input links are updated:
			newNodes.forEach(node => {

				node.fields.forEach(field => {

					var direction = field.direction;

					if (direction == 'out' || direction == 'none') {
						return;
					}

					// This is an "in" field.
					var fieldKey = field.key;

					var state = node.state[fieldKey];

					if (!state || !state.link || !state.node) {
						// Not a linked field.
						return;
					}

					if (state.node.deleted) {
						// Link source node was deleted (probably the one we just removed).
						delete node.state[fieldKey];
					}
				});

			});

			changed(newNodes);
		}} updatedNode={node => {
			node._redraw++;
			changed([...nodes]);
		}} updatedNodes={newNodes => {
			changed(newNodes);
		}} pullToFront={node => {
			/*var newNodes = nodes.filter(n => n != node);
			newNodes.push(node);
			changed(newNodes);*/
		}} namespace={props.namespace || defaultNamespace}
			showConfirmModal={showConfirmModal}
			setShowConfirmModal={setShowConfirmModal}
			cantDeleteModal={cantDeleteModal}
			setCantDeleteModal={setCantDeleteModal}
		/>
		{(props.name || props.inputRef) && <input className="graph-editor-input-field" ref={ir=>{
			props.inputRef && props.inputRef(ir);
			if(ir){
				ir.onGetValue=(val, ele)=>{
					if(ele == ir){
						var nodeTree = saveNodes(nodes, props.namespace || defaultNamespace);
						
						if(props.objectOutput){
							// Output as an object:
							return nodeTree;
						}
						
						// Stringify the tree:
						return JSON.stringify(nodeTree);
					}
				};
			}
		}} name={props.name} type='hidden' />}
	</>;
}

function DraggableItem(props) {
	var { node, selected, setSelected, scale, editorProps, selectConnector, redrawLines,
		nodes, updatedNodes,
		showConfirmModal, setShowConfirmModal,
		cantDeleteModal, setCantDeleteModal
	} = props;

	var eleRef = React.useRef();
	
	React.useEffect(() => {
		var domEle = eleRef.current;
		
		node._domElement = domEle;
		
		var touchStart = (e) => {
			e.stopPropagation();
			e = e.touches[0];

			if(!e || editorProps.disabled){
				return;
			}
			
			editorProps.pullToFront && editorProps.pullToFront(node);
			setSelected(node);

			dragging = {
				x: e.clientX,
				y: e.clientY,
				startX: e.clientX,
				startY: e.clientY,
				element: e.target.parentElement,
				node
			};

			var onTouchMove = e => {
				e.stopPropagation();
				e = e.touches[0];
				
				if(!e){
					return;
				}
				
				dragging.x = e.clientX;
				dragging.y = e.clientY;
				
				var dX = (dragging.x - dragging.startX) / scale;
				var dY = (dragging.y - dragging.startY) / scale;
				
				dragging.element.style.top = (node.offsetY + dY) + 'px';
				dragging.element.style.left = (node.offsetX + dX) + 'px';
				
				redrawLines();
			};
			
			var onTouchEnd = e => {
				e.stopPropagation();

				document.removeEventListener("touchend", onTouchEnd); 
				document.removeEventListener("touchmove", onTouchMove);
				
				var dX = (dragging.x - dragging.startX) / scale;
				var dY = (dragging.y - dragging.startY) / scale;
				
				node.offsetX = node.offsetX + dX;
				node.offsetY = node.offsetY + dY;
				
				editorProps.updatedNode && editorProps.updatedNode(node);
			};

			if (!editorProps.disabled) {
				document.addEventListener("touchend", onTouchEnd); 
				document.addEventListener("touchmove", onTouchMove); 
			}	
		};
		
		domEle.addEventListener("touchstart", touchStart, {capture: true});
		
		return () => {
			domEle.removeEventListener("touchstart", touchStart, {capture: true});
		};
	}, [scale, editorProps]);
	
	var saveNodeChanges = (chgs) => {
		for(var k in chgs){
			node[k] = chgs[k];
		}
		
		editorProps.updatedNode(node);
	};
	
	var deleteNode = () => {
		editorProps.removeNode && editorProps.removeNode(node);
	};
	
	// Get the fields:
	var fieldList = node.fields;
	
	// Get the color for the nodes main type:
	var hslColor = node.getTypeColor();
	
	// Get the name:
	var name = node.getName();
	
	var titleBgColor = colorAsHsl(hslColor);
	var titleTextColor = (hslColor[2] < 0.5) ? 'white' : 'black';
	
	fieldList.forEach(meta => {
		var directionOrder = 1; // in is the default
		
		switch(meta.direction){
			case 'out':
				directionOrder = 2;
			break;
			case 'none':
				directionOrder = 0;
			break;
		}
		
		meta.directionOrder = directionOrder;
	});
	
	// Sort by direction
	fieldList.sort((a,b) => a.directionOrder - b.directionOrder);

	return <div ref={eleRef} className="entry node" style={{border: '1px solid black', borderRadius: '8px', top: node.offsetY + 'px', left: node.offsetX + 'px'}} onClick={e => {
		
		e.stopPropagation();
		
	}} onMouseDown={(e) => {
		
		e.stopPropagation();
		
	}}
	>
		<div className="entry-dragbar" style={{color: titleTextColor, background: titleBgColor}} onMouseDown={e => {
			
			editorProps.pullToFront && editorProps.pullToFront(node);
			
			var dragging = {
				x: e.clientX,
				y: e.clientY,
				element: e.target.parentElement,
				node
			};
			
			var onMouseMove = e => {
				e.stopPropagation();
				var dX = (e.clientX - dragging.x) / scale;
				var dY = (e.clientY - dragging.y) / scale;
				
				dragging.element.style.top = (node.offsetY + dY) + 'px';
				dragging.element.style.left = (node.offsetX + dX) + 'px';
				
				redrawLines();
			};
			
			var onMouseUp = e => {
				e.stopPropagation();
				document.removeEventListener("mouseup", onMouseUp); 
				document.removeEventListener("mousemove", onMouseMove);
				
				node.offsetX = node.offsetX + (e.clientX - dragging.x) / scale;
				node.offsetY = node.offsetY + (e.clientY - dragging.y) / scale;
				
				saveNodeChanges({
					offsetX: node.offsetX,
					offsetY: node.offsetY
				});
			};
			
			if (!editorProps.disabled) {
				document.addEventListener("mouseup", onMouseUp); 
				document.addEventListener("mousemove", onMouseMove); 
			}	
		}}>
			<span className="entry-header__name">
				{niceName(name)}
			</span>
			<div className="entry-header__controls">
				{!node.root && <>
					<button type="button" className="btn btn-outline-dark btn-sm" onClick={() => {
						setRootNode(node, nodes);
						props.updatedNodes([...props.nodes]);
					}} title={`Set as main output`}>
						<i className="fa fa-fw fa-sitemap"></i>
					</button>
				</>}
				<button type="button" className="btn btn-outline-danger btn-sm" onClick={() => node.root ? setCantDeleteModal(true) : setShowConfirmModal(node)}
			title={`Remove node`}>
					<i className="fa fa-fw fa-trash"></i>
				</button>
			</div>
	</div>
		<div className="entry-content">
			{fieldList.map(field => {
				var fieldMeta = field;
				var fieldKey = field.key;
				var currentValue = node.state[fieldKey];
				var hasValue = currentValue && currentValue.link && currentValue.node && currentValue.field;
				
				var dir = fieldMeta.direction;
				
				var onUpdate = (newValue) => {
					
					var prevValue = node.state[fieldKey];
					
					var otherNodeToUpdate = null;
					
					if(prevValue && prevValue.link && prevValue.node){
						// Clearing a link. Tell the other node too.
						otherNodeToUpdate = prevValue.node;
					}
					
					node.state[fieldKey] = newValue;
					
					// Ask the node to validate its state, then apply which will trigger a re-render:
					var proms = [node.validateState()];
					
					if(otherNodeToUpdate){
						proms.push(otherNodeToUpdate.validateState());
					}
					
					Promise.all(proms).then(() => {
						// Store and re-render:
						saveNodeChanges({});
					});
				};
				
				var fullType = getType(fieldMeta.type);
				
				var defaultFieldInput = (value, setValue, label) => {
					
					if(isDefaultType(fullType) && !fullType.isArray && !fullType.isAny){
						return <Input compact label={label} type={fullType.name} value={value} defaultValue={value} onChange={e => {
							setValue(e.target.value);
						}}/>;
					} else {
						return <label className="form-label">
							{label}
						</label>;
					}
					
				};
				
				if(dir == 'out'){
					return <div className={fieldMeta.onRender ? 'entry-content__field entry-content__field--vertical entry-content__field__output' : 'entry-content__field entry-content__field__output'}
						data-field={fieldKey}>
						{fieldMeta.onRender && fieldMeta.onRender(currentValue, onUpdate)}
						<span className="entry-content__name">
							{niceName(fieldMeta.name)}
						</span>
						<TypeIcon type={fullType} className={"type-icon--bottom"} onClick={(canConnect) => {
							selectConnector(node, fieldKey, 'out', fieldMeta, canConnect);
						}}/> 
					</div>;
				}else if(dir == 'none'){
					return <div className="entry-content__field entry-content__field--vertical">
						{fieldMeta.onRender ?
							fieldMeta.onRender(currentValue, onUpdate, niceName(fieldMeta.name)) :
							defaultFieldInput(currentValue, onUpdate, niceName(fieldMeta.name))}
					</div>;
				}
				
				return <div className={hasValue ?
					"entry-content__field entry-content__field__input entry-content__field__input" :
					"entry-content__field entry-content__field__input entry-content__field__input--vertical"}
					data-field={fieldKey}>
					<TypeIcon type={fullType} onClick={(canConnect) => {
							selectConnector(node, fieldKey, 'in', fieldMeta, canConnect);
					}} />
					{hasValue && <>
						<span className="entry-content__name">
							{niceName(fieldMeta.name)}
						</span>
					</>}
					{hasValue ?
						<button type="button" className="btn btn-sm btn-outline-danger" onClick={() => {
							onUpdate(null);
						}}>
							<i className="fa fa-fw fa-times"></i>
						</button>
						: (fieldMeta.onRender ?
							fieldMeta.onRender(currentValue, onUpdate, niceName(fieldMeta.name)) :
							defaultFieldInput(currentValue, onUpdate, niceName(fieldMeta.name)))}
				</div>;
				
			})}
		</div>
	</div>;
}

function getLinkDomNode(node, fieldKey){
	
	var domFields = node._domElement.getElementsByClassName("entry-content__field");
	
	var matchingDomField = null;
	
	for(var i=0;i<domFields.length;i++){
		if(domFields[i].getAttribute("data-field") == fieldKey){
			matchingDomField = domFields[i];
			break;
		}
	}
	
	if(!matchingDomField){
		return null;
	}
	
	var connectorDomNode = matchingDomField.getElementsByClassName("type-icon");
	
	return connectorDomNode.length ? connectorDomNode[0] : null;
}

var updateNode = (node, context) => {
	
	if(!node._needsUpdate){
		return;
	}
	
	// Clear the needs update flag:
	node._needsUpdate = false;
	
	// Does it have any upstream nodes? if so, update them first.
	var s = node.state;
	
	if(Array.isArray(s)){
		for(var i=0;i<s.length;i++){
			var entry = s[i];
			
			if(entry && entry.link && entry.node && entry.field){
				// it's a link to another node, which must be updated before this one is.
				
				// If the field does not exist, or the node was deleted, then delete the link too.
				if(entry.node.deleted){
					s[i] = null;
					continue;
				}
				
				// Request that now:
				updateNode(entry.node, context);
				
				// If the field doesn't exist, delete the link.
				if(!entry.node.getField(entry.field)){
					s[i] = null;
					continue;
				}
			}
		}
	}else{
		for(var key in s){
			var entry = s[key];
			
			if(entry && entry.link && entry.node && entry.field){
				// it's a link to another node, which must be updated before this one is.
				
				if(entry.node.deleted){
					delete s[key];
					continue;
				}
				
				// Request that now:
				updateNode(entry.node, context);
				
				// If the field doesn't exist, delete the link.
				if(!entry.node.getField(entry.field)){
					delete s[key];
					continue;
				}
				
			}
		}
	}
	
	// Update this node now:
	node.update(context);
};

var updateAllNodesInOrder = (nodes, context) => {
	// Ensures all upstream nodes of a node are updated before a node is updated.
	
	nodes.forEach(node => {
		node._needsUpdate = true;
	});
	
	nodes.forEach(node => {
		updateNode(node, context);
	});
	
};

export function GraphEditorCore(props){
	var [selected, setSelected] = React.useState();
	var [selectedConnector, setSelectedConnector] = React.useState(null);
	var [selectNodeType, setSelectNodeType] = React.useState(false);
	var hostElementRef = React.useRef();
	var canvasRef = React.useRef();
	var { showConfirmModal, setShowConfirmModal, cantDeleteModal, setCantDeleteModal, nodes } = props;

	React.useEffect(() => {
		document.querySelector("html").classList.add("page-graph");

		return () => {
			document.querySelector("html").classList.remove("page-graph");
		};

	}, []);
	
	var redrawLines = () => {
		
		// Do not set state in here. This is exclusively for interacting with the canvas context and the DOM.
		
		var nodes = props.nodes;
		
		var lines = [];
		
		nodes.forEach(node => {
			
			// For each of a nodes input links (because a dir:in can only have one input), update the line start/end.
			if(!node.fields || !node._domElement){
				return;
			}
			
			node.fields.forEach(field => {
				
				var direction = field.direction;
				
				if(direction == 'out' || direction == 'none'){
					return;
				}
				
				// This is an "in" field.
				var fieldKey = field.key;
				
				var state = node.state[fieldKey];
				
				if(!state || !state.link || !state.node){
					// Not a linked field.
					return;
				}
				
				if(state.node.deleted){
					// Source node was deleted.
					console.log("Line drawer has identified invalid link state. A node was deleted but it is still linked by other nodes.");
					return;
				}
				
				// The DOM connection point is..
				var targetDomNode = getLinkDomNode(node, fieldKey);
				
				if(targetDomNode){
					// Find the remote one too:
					var sourceDomNode = getLinkDomNode(state.node, state.field);
					
					if(!sourceDomNode){
						console.log("Line drawer has identified invalid link state. An upstream input field no longer exists, but the link was not cleared.");
						return;
					}
					
					var target = targetDomNode.getBoundingClientRect();
					var src = sourceDomNode.getBoundingClientRect();
					
					var type = state.node.getFieldType(state.field);
					
					var typeInfo = type ? getType(type) : null;
					
					lines.push({
						from: src,
						to: target,
						style: (typeInfo && typeInfo.color ? colorAsHsl(typeInfo.color) : 'black')
					});
				}
				
			});
			
		});
		
		var canvas = canvasRef.current;
		
		if(!canvas){
			return;
		}
		
		var ctx = canvas.getContext('2d');
		var canvasPos = canvas.getBoundingClientRect();
		
		var xOffset = -canvasPos.x;
		var yOffset = -canvasPos.y;
		
		ctx.clearRect(0, 0, canvas.width, canvas.height);
		ctx.lineWidth = 3;
		
		lines.forEach(line => {
			
			ctx.strokeStyle = line.style;
			
			// Also offset each one by half the icon box (40px at 100%)
			var halfBox = line.from.width / 2;
			
			ctx.beginPath();
			ctx.moveTo(line.from.x + xOffset + halfBox, line.from.y + yOffset + halfBox);
			ctx.lineTo(line.to.x + xOffset + halfBox, line.to.y + yOffset + halfBox);
			ctx.stroke();
			
		});
		
	};
	
	React.useEffect(() => {
		
		redrawLines();
		
	});
	
	var tryConnect = (source, target) => {
		var compatibilityState = typeCompatibility(source.fieldMeta.type, target.fieldMeta.type);
		
		if(!compatibilityState.ok){
			console.warn("Can't connect incompatible types", compatibilityState);
			return Promise.resolve(); // reject in future
		}
		
		// Specify a link between source and target fields.
		
		// Input fields only have one incoming link so they are stored from its point of view.
		// source.node.connect(source.fieldMeta, target.node, target.fieldMeta);
		target.node.connect(target.fieldMeta, source.node, source.fieldMeta);
		
		// Run state validation of both nodes.
		var proms = [];
		
		if(target.node){
			proms.push(target.node.validateState());
		}
		
		if(source.node){
			proms.push(source.node.validateState());
		}
		
		return Promise.all(proms);
	};
	
	var selectConnector = (node, fieldKey, dir, fieldMeta, canConnect) => {
		
		if(dir != 'in' && dir != 'out'){
			return;
		}
		
		var sc = {
			node, fieldKey, dir, fieldMeta
		};
		
		if(selectedConnector){
			// Attempt to connect this one and the already selected one.
			if(selectedConnector.node == node || !canConnect){
				// Mouse went down then up on the same node, 
				// or the mouse just went down on a node and we're waiting for it to go back up.
				// This allows click->drag from one node to another or clicking one then clicking another.
				return;
			}
			
			if(dir == selectedConnector.dir){
				console.warn("Can't connect " +dir + " to another "+dir);
				setSelectedConnector(null);
				return;
			}
			
			var targ = selectedConnector;
			setSelectedConnector(null);
			
			// tryConnect always takes them with the input node first.
			var prom = null;
			
			if(dir == 'in'){
				prom = tryConnect(targ, sc).then(() => {
					// Indicate the node has updated.
					props.updatedNode(sc.node);
				});
				
			}else{
				prom = tryConnect(sc, targ).then(() => {
					// Indicate the node has updated.
					props.updatedNode(targ.node);
				});
			}
			
			prom.then(() => {
				redrawLines();
				// console.log("Selected is now", selectedConnector);
			});
			
		}else{
			setSelectedConnector(sc);
		}
		
	};
	
	var renderNodes = nodes => {
		
		if(!nodes){
			return <Loading />;
		}
		
		// Sort them by edited date:
		var editingImage = null;
		
		/*
		nodes = nodes.filter(image => {
			// Omit the image being edited such that it can be pushed last always.
			if(image.id == editingImageId){
				editingImage = image;
				return false;
			}
			
			return true;
		});
		
		if(editingImage){
			// Editing image always top of stack:
			nodes.push(editingImage);
		}
		*/
		
		// initial translation at the center of the nodes plus half the screen size:
		var defaultValue = {
			scale: 1,
			translation: {
				x: 0,
				y: 0
			}
		};
		
		// When translated to 0,0 a image at 0,0 is in the top left corner of the screen.
		// We'd like the midpoint of all nodes to be at the middle of the host element.
		// So, first, how big is our host element?
		
		var screenOffset = {x: 0, y: 0};
		
		if(hostElementRef.current){
			var hostSize = hostElementRef.current.getBoundingClientRect();
			
			// Shift to the center:
			screenOffset.x = hostSize.width / 2;
			screenOffset.y = hostSize.height / 2;
			defaultValue.translation.x = screenOffset.x;
			defaultValue.translation.y = screenOffset.y;
		}
		
		if (nodes.length) {

			var sumX = 0;
			var sumY = 0;

			nodes.forEach(node => {

				sumX += node.offsetX;
				sumY += node.offsetY;

			});

			defaultValue.translation.x -= sumX / nodes.length;
			defaultValue.translation.y -= sumY / nodes.length;
        }
		
		// Originates from UI/SlippyMap
		// Has the job of allowing the user to drag/ zoom the image space
		return <MapInteraction
			showControls
			defaultValue={defaultValue}
			onRenderControls={
				(map, step) => {
					
					var addNodeOfType = (nodeTypeInfo) => {

						// What exactly is the middle of the pov - what should the node position be?
						var mapPos = map.props.value.translation;

						var offsetX = (screenOffset.x - mapPos.x) / map.props.value.scale;
						var offsetY = (screenOffset.y - mapPos.y) / map.props.value.scale;
						
						var node = new nodeTypeInfo.Type({offsetX, offsetY});

						props.onAddNode && props.onAddNode(node);
						
						setSelected(node);
						setSelectNodeType(null);
					};
					
					var addNode = () => {
						// Show node type selection UI
						setSelectNodeType({onSelected: addNodeOfType});
					};
					
					var deleteItem = () => {
							
						if(selected){
							nodes = nodes.filter(i => i != selected);
							props.updatedNodes(nodes);
							setSelected(null);
						}
						
					};
					
					var moveToFront = () => {
						
						if(selected){
							// Sort selected back 1 place in the stack:
							var index = nodes.indexOf(selected);
							
							if(index == -1 || index >= nodes.length - 1){
								return;
							}
							
							var next = nodes[index+1];
							nodes[index+1] = selected;
							nodes[index] = next;
							
							props.updatedNodes(nodes);
						}
						
					};
					
					return <header className="graph-editor__ui">
						<div className="zoom-widget">
							<button title={`Zoom out`} type="button" className="btn" onPointerUp={() => map.changeScale(-step)}>
								-
							</button>
							<span className="zoom-level" onPointerUp={() => { map.resetScale() }}>
								{map.getScale()}%
							</span>
							<button title={`Zoom in`} type="button" className="btn" onPointerUp={() => map.changeScale(step)}>
								+
							</button>
						</div>
						<button title={`Add node`} type="button" className="btn btn-sm btn-primary graph-ui-btn" onPointerUp={addNode}>
							<i className="far fa-fw fa-plus"></i> {`Add`}
						</button>
						{/*<button title="Save" type="button" className="btn graph-ui-btn" onPointerUp={props.onSave}>Save (temp)</button>
						{selected && <>
						<button title="Delete" type="button" className="btn graph-ui-btn" onPointerUp={deleteItem}><i className='fa fa-trash' /></button>
						</>}*/}
						</header>;
				}
			}
			nodes={nodes}
			heading={<>{`Graph Editor`}</>}
			instructions={`Add nodes to connect a graph`}
		>
		  {
			({ translation, scale }) => {
				props.onUpdateTransform && props.onUpdateTransform(translation, scale);
				
				// Moved or translated - make sure the lines are up to date:
				setTimeout(() => {
					redrawLines();
				}, 10);
				
				// Ensure all nodes are updated in order of their input dependency.
				updateAllNodesInOrder(nodes, props.context);
				
			  // Translate first and then scale.  Otherwise, the scale would affect the translation.
			  const transform = `translate(${translation.x}px, ${translation.y}px) scale(${scale})`;
			  return (
				<div
				  style={{
					height: '100%',
					width: '100%',
					position: 'relative', // for absolutely positioned children
					overflow: 'hidden',
					touchAction: 'none', // Not supported in Safari :(
					msTouchAction: 'none',
					cursor: 'all-scroll',
					WebkitUserSelect: 'none',
					MozUserSelect: 'none',
					msUserSelect: 'none'
				  }}
				>
				  <div
					style={{
					  display: 'inline-block', // size to content
					  transform: transform,
					  transformOrigin: '0 0 '
					}}
				  >
						  {nodes.map(node => <DraggableItem
							  redrawLines={redrawLines}
							  key={node.id} rdid={node._redraw} node={node}
							  scale={scale} selected={selected} setSelected={setSelected}
							  editorProps={props} selectConnector={selectConnector}
							  nodes={nodes} updatedNodes={props.updatedNodes}
							  showConfirmModal={showConfirmModal} setShowConfirmModal={setShowConfirmModal}
							  cantDeleteModal={cantDeleteModal} setCantDeleteModal={setCantDeleteModal}

					/>)}
				  </div>
				</div>
			  );
			}
		  }
		</MapInteraction>
	}
	
	return <div ref={hostElementRef} className="graph-editor">
		<canvas ref={canvasRef} width={'1920'} height={'1080'} style={{position: 'absolute'}}/>
		{renderNodes(props.nodes)}
		{selectNodeType && <SelectNodeType namespace={props.namespace} onClose={() => setSelectNodeType(null)} onSelected={nodeType => {
			// Run the method:
			selectNodeType.onSelected(nodeType);
		}} />}
		{showConfirmModal && <Modal visible className="confirm-delete-modal" onClose={() => setShowConfirmModal(false)}>
			<p>
				{`Are you sure you want to delete this?`}
			</p>
			<footer className="confirm-delete-modal__footer">
				<button type="button" className="btn btn-danger" onClick={() => {
					// Delete the node
					var toDelete = showConfirmModal;
					var newNodes = props.nodes.filter(i => i != toDelete);
					props.updatedNodes(newNodes);
					setShowConfirmModal(false);
                }}>
					{`Yes, delete it`}
				</button>
				<button type="button" className="btn btn-primary" onClick={() => setShowConfirmModal(false)}>
					{`Cancel`}
				</button>
			</footer>
		</Modal>}
		{cantDeleteModal && <Modal visible className="cant-delete-modal" onClose={() => setCantDeleteModal(false)}>
			<p>
				{`Unable to remove this node as it's currently set as the main output. Please first assign a different node as the main output to be able to delete this node.`}
			</p>
			<footer className="cant-delete-modal__footer">
				<button type="button" className="btn btn-primary" onClick={() => setCantDeleteModal(false)}>
					{`Close`}
				</button>
			</footer>
		</Modal>}
	</div>;
	
}