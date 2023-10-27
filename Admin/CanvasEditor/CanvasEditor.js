import Alert from 'UI/Alert';
import Input from 'UI/Input';
import Graph from 'UI/Functions/GraphRuntime/Graph';
import Collapsible from 'UI/Collapsible';
import { ErrorCatcher } from 'UI/Canvas';
import getBuildDate from 'UI/Functions/GetBuildDate';
import ModuleSelector from 'Admin/CanvasEditor/ModuleSelector'
import PropEditor from 'Admin/CanvasEditor/PropEditor';
import omit from 'UI/Functions/Omit';
import RichEditor from 'Admin/CanvasEditor/RichEditor';
import Draft from 'Admin/CanvasEditor/DraftJs/Draft.min.js';
const { EditorState, convertToRaw } = Draft;
import Immutable from 'Admin/CanvasEditor/DraftJs/immutable.min.js';
import ThemeEditor from 'Admin/CanvasEditor/ThemeEditor';
import PanelledEditor from 'Admin/Layouts/PanelledEditor';
import CanvasState from './CanvasState';
import { getRootInfo } from './Utils';
import { createLinkDecorator } from 'Admin/CanvasEditor/Link';
var nodeKeys = 1;

const DEFAULT_BLOCK_TYPE = 'p';

// Connect the input "ontypecanvas" render event:
var inputTypes = global.inputTypes = global.inputTypes || {};

// type="canvas"
inputTypes.ontypecanvas = function(props, _this){
	
	return <CanvasEditor
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
	return <CanvasEditor
		id={props.id || _this.fieldId}
		className={props.className || "form-control"}
		toolbar 
		modules
		enableAdd
		groups = {"formatting"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
};

var blockRenderMap = Immutable.Map({
	// Add support for paragraph
	'paragraph': {
		element: 'p'
	},
	'linebreak': {
		element: 'br'
	},
	'center': {
		element: 'center'
	},
	'left': {
		element: 'left'
	},
	'right': {
		element: 'right'
	},
	// Overwrite DefaultDraftBlockRenderMap as it defines aliased element 'p' for 'unstyled'
	'unstyled': {
		element: DEFAULT_BLOCK_TYPE
	}
  });
var extendedBlockRenderMap = window.SERVER ? null : Draft.DefaultDraftBlockRenderMap.merge(blockRenderMap);

var TEXT = '#text';

function ComponentAdd(props) {
	return <div className="rte-component-add" onClick={() => {
		props.onAdd && props.onAdd();
	}}>
		<div className="rte-component-add__line"></div>
		<i className="fa fa-plus" />
	</div>;
}

function niceGraphName(graph){
	if(!graph.structure || !Array.isArray(graph.structure.c)){
		return 'Graph [empty!]';
	}
	
	var c = graph.structure.c;
	
	// c(ontent) is an array of nodes. One has r:true.
	var root = null;
	
	for(var i=0;i<c.length;i++){
		if(c[i].r){
			root = c[i];
			break;
		}
	}
	
	if(!root){
		return 'Graph [no root!]';
	}
	
	if(root.t && root.t.toLowerCase() == 'component'){
		return root.d && root.d.componentType || 'Component';
	}
	
	return root.t;
}

function renderStructureNode(node, canvasState, onClick, snapshotState) {
	var nodeName = node.typeName || node.type;
	var isGridColumn = node.parent && node.parent.typeName == 'UI/Grid';

	if (isGridColumn) {
		// default for the grid is 1 row, 2 columns; check for overrides
		var rows = node.parent.props.rows || 1;
		var cols = node.parent.props.columns || 2;
		cols = parseInt(cols, 10);

		var actualCol = (node.index % cols) + 1;
		var actualRow = Math.floor(node.index / cols) + 1;

		nodeName = `Col ${actualCol} Row ${actualRow}`;
    }

	if (nodeName == 'richtext') {
		nodeName = `Formatted text`;
    }
	
	if(node.graph){
		nodeName = niceGraphName(node.graph);
	}
	
	var removeNode = () => {
		canvasState.removeNode(node, false, true);
		snapshotState();
	};

	var hasChildren = node.roots && node.roots.children && node.roots.children.content;
	var nodeParent = hasChildren ? node.roots.children.content : null;

	// check for v2 canvas (e.g. UI/Grid)
	if (!hasChildren && node.roots) {
		nodeParent = Object.values(node.roots);

		nodeParent.forEach(value => {
			if (value.content) {
				hasChildren = true;
            }
		});
    }

	if (!hasChildren && node.content && node.content.length) {
		nodeParent = Object.values(node.content);
		nodeParent.map(n => {
			return {
				content: n
			}
		});

		hasChildren = true;
	}


	if (!hasChildren) {
		return <>
			<button type="button" className="btn panelled-editor__structure-item-text" onClick={(e) => {
				e.preventDefault();
				e.stopPropagation();
				onClick && onClick(node);
			}}>
				{nodeName}
			</button>
			<button type="button" className="btn btn-sm btn-outline-danger btn-remove" title={`Remove`}
				onClick={() => removeNode()}>
				<i className="fa fa-fw fa-trash"></i>
			</button>
		</>;
	}

	var removeButton = {
		//disabled: false,
		icon: 'fa fa-fw fa-trash',
		text: `Remove`,
		showLabel: false,
		variant: 'danger',
		onClick: function (e) {
			if(e){
				e.stopPropagation();
				e.preventDefault();
			}
			removeNode()
		}
	};

	return <Collapsible title={nodeName} expanderLeft onClick={() => onClick(node)} buttons={isGridColumn ? undefined : [removeButton]}>
		{nodeParent.map((childNode, i) => {
			var itemClass = ['collapsible-content__wrapper'];

			if (childNode == canvasState.selectedNode) {
				itemClass.push('collapsible-content__wrapper--selected');
			}

			// new instances of UI/Grid won't label underlying UI/Columns without this
			if (!childNode.parent) {
				childNode.parent = node;
			}

			// give UI/Grid columns access to their index
			if (childNode.parent.typeName == "UI/Grid") {
				childNode.index = i;
            }

			return <span className={itemClass.join(' ')}>
				{renderStructureNode(childNode, canvasState, onClick, snapshotState)}
			</span>
		})}
	</Collapsible>;
}

function renderStructure(canvasState, onClick, snapshotState) {
	var content = canvasState && canvasState.node;

	if(!content){
		content = {content: []};
	}
	
	// Root nodes always have an array as their content.
	return content.content.map((node, i) => {
		var itemClass = ['panelled-editor__structure-item'];

		if (node == canvasState.selectedNode) {
			itemClass.push('panelled-editor__structure-item--selected');
        }

		return <li className={itemClass.join(' ')}>
			{renderStructureNode(node, canvasState, onClick, snapshotState)}
		</li>;
	});

}

function applyCustomNodeUpdate(node) {
	// A component can define a static onEditorUpdate function which runs when a node is added or has its props updated in the editor.
	// This allows the editor node to be manipulated however the component would like - 
	// for example, UI/Grid uses it to create root spaces for the flexible number of cells.

	if (node && node.type && typeof node.type !== 'string') {

		var onEditorUpdate = node.type.onEditorUpdate;
		var decorator = createLinkDecorator();

		onEditorUpdate && onEditorUpdate(node, {
			addEmptyRoot: (node, rootName) => {

				if (!node.roots) {
					node.roots = {};
				}

				// Adding an empty root with an RTE.
				var rootObj = { content: [] };
				var emptyPara = { type: 'richtext', editorState: EditorState.createEmpty(decorator) };
				emptyPara.parent = rootObj;
				rootObj.content.push(emptyPara);
				node.roots[rootName] = rootObj;

			}
		});
	}

}

export default function CanvasEditor (props) {
	var [canvasState, setCanvasState] = React.useState(() => {
		var state = new CanvasState(extendedBlockRenderMap);
		state = state.load(props.value || props.defaultValue);
		return state;
	});
	
	var snapshotState = (snap) => {
		
		// Create a snapshot if one is not being provided:
		var newState = snap || canvasState.addStateSnapshot();
		
		// Update:
		setCanvasState(newState);
		
		return newState;
	};

	var ceCore = <CanvasEditorCore {...props} onSetShowSource={(state) => {
		setCanvasState(canvasState.setShowSourceState(state));
	}} fullscreen={props.fullscreen} canvasState={canvasState} snapshotState={snapshotState}
		onSelectNode={(e, node) => {
			e.stopPropagation();
			setCanvasState(canvasState.selectNode(canvasState.selectedNode == node ? null : node));
		}} />;

	var ctx = {};
	
	if(props.primary){
		ctx.primary = props.primary;
	}
	
	var coreWrapper = ['rte-component__core-wrapper'];

	if (canvasState.sourceMode) {
		coreWrapper.push('rte-component__core-wrapper--100');
    }

	if (props.fullscreen) {
		return <PanelledEditor
			graphState={canvasState.graphState}
			title={props.currentContent ? `Edit Page` : `Create Page`}
			controls={props.controls}
			feedback={props.feedback}
				rightPanel={() => {
					
					if(!canvasState.selectedNode){
						return null;
					}
					
					// Note: The propEditor must not have any named fields. If it did, they would be submitted when the content is saved.
					return <>
						<PropEditor optionsVisibleFor={canvasState.selectedNode} onChange={() => {

						applyCustomNodeUpdate(canvasState.selectedNode);

							setCanvasState(canvasState.addStateSnapshot());
						}} setGraphState={
							targetState=>{
								setCanvasState(canvasState.setGraphState(targetState));
							}
						} setThemeState={
							targetState=>{
								setCanvasState(canvasState.setThemeState(targetState));
							}
						}/>
					</>;
				}}
				showRightPanel={canvasState.showRightPanel && !canvasState.sourceMode && !canvasState.graphState && !canvasState.themeState}
				toggleRightPanel={newState => {
					setCanvasState(canvasState.changeRightPanel(newState));
				}}
				showLeftPanel={canvasState.showLeftPanel && !canvasState.sourceMode && !canvasState.graphState && !canvasState.themeState}
				toggleLeftPanel={newState => {
					setCanvasState(canvasState.changeLeftPanel(newState));
				}}
				changeRightTab={newTab=>{
					setCanvasState(canvasState.changePropertyTab(newTab));
				}}
				propertyTab={canvasState.propertyTab}
				additionalFieldsTitle={`Page`}
				rightPanelTitle={`Component`}
				leftPanelTitle={`Structure`}
				additionalFields={props.additionalFields} 
				showSource={!!canvasState.sourceMode || !!canvasState.graphState || !!canvasState.themeState} 
				onSetShowSource={(state) => {
					if(canvasState.graphState){
						// Exiting graph editor. Ensure the value is baked in.
						var val = this.graphIr.onGetValue ? this.graphIr.onGetValue(null, this.graphIr) : this.graphIr.value;
						
						// Update the graph:
						canvasState.selectedNode.graph = new Graph(val);
						setCanvasState(canvasState.setGraphState(false));
					}else if(canvasState.themeState){
						// Exiting theme editor.
						setCanvasState(canvasState.setThemeState(false));
					}else{
						setCanvasState(canvasState.setShowSourceState(state));
					}
				}}
				breadcrumbs={props.breadcrumbs}
				leftPanel={() => {
					return renderStructure(canvasState, (node) => {
						
						// Select the node:
						setCanvasState(canvasState.selectNode(canvasState.selectedNode == node ? null : node));
						
				}, snapshotState);
				}}
			>
			{/* Uses display:none to ensure it always exists in the DOM such that the save button submits everything */}
			<div className={coreWrapper.join(' ')} style={(canvasState.graphState || canvasState.themeState) ? { display: 'none' } : undefined}>
				{ceCore}
			</div>
			{canvasState.graphState && <Input type='graph' objectOutput={true} inputRef={ir=>{
				this.graphIr=ir;
			}} value={canvasState.selectedNode.graph.structure} context={ctx} onChange={() => {
					setCanvasState(canvasState.addStateSnapshot());
				}}/>}
			{canvasState.themeState && <ThemeEditor 
				inputRef={ir=>{
					this.themeIr=ir;
				}} 
				node={canvasState.selectedNode}
				onChange={() => {
					setCanvasState(canvasState.addStateSnapshot());
				}}
			/>}
		</PanelledEditor>;
	}
	
	return ceCore;
}

/*
	Canvas editor - Socialstack's main content editor.
	Handles addition of components in a generic way along with rich text editing.
*/
class CanvasEditorCore extends React.Component {
	
	constructor(props){
		super(props);
		// this.onKeyDown = this.onKeyDown.bind(this);
		this.onContextMenu = this.onContextMenu.bind(this);
		this.onMouseUp = this.onMouseUp.bind(this);
		this.closeModal = this.closeModal.bind(this);
		this.onReject = this.onReject.bind(this);
	}
	
	onReject(e){
		e.preventDefault();
	}
	
	onMouseUp(e){
		// Needs to look out for e.g. selecting text but mouse-up outside the text area.
		// Because of the above, this is a global mouseup handler. Ensure it does the minimal amount possible.
		
		if(e.button == 2){
			// Right click menu
			var {canvasState} = this.props;
			var node = canvasState.node;
			var current = e.target;
			var target = this.getNode(current, node);
			
			while(!target && current){
				current = current.parentNode;
				target = this.getNode(current, node);
			}
			
			if(target){
			
				this.setState({
					rightClick: {
						node: target,
						x: e.clientX,
						y: e.clientY
					}
				});
			
			}
			
		}else if(this.state.rightClick){
			this.setState({rightClick: null});
		}
	}
	
	onContextMenu(e){
		e.preventDefault();
		return false;
	}
	
	/*
	* Gets the state node related to the given dom node.
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
	
	componentDidMount(){
		window.addEventListener("mouseup", this.onMouseUp);
	}
	
	componentWillUnmount(){
		window.removeEventListener("mouseup", this.onMouseUp);
	}
	
	closeModal(){
		this.setState({
			selectOpenFor: null,
			optionsVisibleFor: null,
			rightClick: null
		});
		//this.updated();
	}
	
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
				
				((c) => {
					if(typeof cur.type != 'string'){
					
						var displayName = this.displayModuleName(c.typeName);
						buttons.push({
							onClick: (e) => {
								e.preventDefault();
								
								if(this.props.fullscreen){
									// Node is selected and displayed in the right side panel.
									this.props.snapshotState(this.props.canvasState.selectNode(c));
									
									return;
								}
								
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
											<PropEditor optionsVisibleFor = {c} onChange={() => {
												this.props.snapshotState();
											}}/>
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
									this.props.canvasState.removeNode(c);
									this.props.snapshotState();
								},
								icon: 'trash',
								text: 'Delete ' + displayName
							});
							
							/*
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
							*/
							
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
					
					} else {
						// If this is the root text node, display a delete option.
						if (!this.props.textonly) {
							buttons.push({
								onClick: (e) => {
									e.preventDefault();
									this.props.canvasState.removeNode(c);
									this.props.snapshotState();
								},
								icon: 'trash',
								text: 'Delete text'
							});
						}
					}
					
				})(cur);
				
			} else {

				((c) => {
					if (!this.props.textonly) {
						buttons.push({
							onClick: (e) => {
								e.preventDefault();
								this.setState({selectOpenFor: {node: c, isAfter: false}})
							},
							icon: 'plus',
							text: 'Add here'
						});
					}
				})(cur);
			}
			cur = cur.parent;
		}
		
		buttons.push({
			onClick: () => this.props.onSetShowSource(true),
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

	renderRootNode(node, canvasState){
		if(!Array.isArray(node.content)){
			throw new Error("Root nodes must have an array as their content.");
		}

		var nodeSet = [];

		if (!this.props.textonly) { 
			nodeSet.push(<ComponentAdd key={"base-add"} onAdd={() => {
				this.setState({selectOpenFor: {node, insertIndex: 0}});
			}}/>);
		} else {
			nodeSet.push(<div className="empty-node" />);
		}

		node.content.forEach((n,i) => {
			var rendered = this.renderNode(n, canvasState);

			if (!this.props.textonly) {
				nodeSet.push(rendered, <ComponentAdd key={n.key + "-add"} onAdd={() => {
					this.setState({selectOpenFor: {node, insertIndex: (i+1)}});
				}}/>);
			}else {
				nodeSet.push(rendered, <div className="empty-node" />);
			}
		});
		
		return nodeSet;
	}
	
	renderNode(node, canvasState) {
		var NodeType = node.type;
		
		if(!node.dom){
			node.dom = React.createRef();
		}
		
		if(!node.key){
			node.key = nodeKeys++;
		}

		var rteClass = ['rte-component'];

		if (node == canvasState.selectedNode && NodeType != 'richtext' && !this.props.textonly) { // Not richtext because clicking on the node focuses & unfocuses it repeatedly
			rteClass.push('rte-component--selected');
		}

		if (this.props.textonly) {
			rteClass.push('rte-component--text-only');
		}

		var rteClasses = rteClass.join(' ');
		
		if(NodeType === 'richtext'){
			var editorContext = this.props.canvasContext ? [...this.props.canvasContext] : [];

			if (editorContext) {
				editorContext = editorContext.filter(c => c.name != this.props.name);
			}

			// Pass the whole node to the RTE.
			return <div key={node.key} ref={node.dom} data-component-type={node.typeName} className={rteClasses} {...node.props} onClick={(e) => {
					if(node != canvasState.selectedNode){
						this.props.onSelectNode(e, node);
					}
				}}>
				<RichEditor editorState={node.editorState} selectedNode={node} textonly={this.props.textonly} context={editorContext} blockRenderMap={extendedBlockRenderMap} onAddComponent={() => {
					this.setState({selectOpenFor: {node, isReplace: true}});
				}} onStateChange={(newState) => {
					node.editorState = newState;
					this.props.snapshotState();
				}}/>
			</div>;
		} else if (node.graph) {
			let component = node.graph.structure?.c?.filter(c => c.t == 'Component');
			let componentType = '';

			if (component && component.length) {
				componentType = component[0].d?.componentType;
			}

			// Graph node.
			return <div key={node.key} ref={node.dom} data-component-type={componentType} className={rteClasses} {...node.props} onClick={(e) => this.props.onSelectNode(e, node)}>
				<ErrorCatcher node={node}>
					{node.graph.render()}
				</ErrorCatcher>
			</div>;
		
		}else{
			// Custom component
			var props = {...node.props, _rte: this};
			
			if(node.roots){
				var children = null;
				
				for(var k in node.roots){
					var root = node.roots[k];
					
					var isChildren = k == 'children';
					
					if(!root.dom){
						root.dom = React.createRef();
					}
					
					if(!root.key){
						root.key = nodeKeys++;
					}

					var rendered = <>
						<div key={root.key} data-component-type={node.typeName} className={rteClasses} ref={root.dom} onClick={(e) => this.props.onSelectNode(e, node)}>
							{this.renderRootNode(root, canvasState)}
						</div>
					</>;

					if(isChildren){
						children = rendered;
					}else{
						props[k] = rendered;
					}
				}

				return <div key={node.key} data-component-type={node.typeName} className={rteClasses} ref={node.dom} onClick={(e) => this.props.onSelectNode(e, node)}>
						<ErrorCatcher node={node}><NodeType {...props}>{children}</NodeType></ErrorCatcher>
					</div>;

			}else{
				// It has no content inside it; it's purely config driven.
				return <div key={node.key} data-component-type={node.typeName} className={rteClasses} ref={node.dom} onClick={(e) => this.props.onSelectNode(e, node)}>
					<ErrorCatcher node={node}><NodeType {...props} /></ErrorCatcher>
				</div>;
			}
		}
	}
	
	isCustom(node){
		return (node && node.type && typeof node.type != 'string');
	}
	
	componentWillReceiveProps(props){
		if(props.canvasState.sourceMode != this.props.canvasState.sourceMode){
			
			if(props.canvasState.sourceMode){
				// Generate default value for the source editor:
				this.sourceModeValue = props.canvasState.toCanvasJson(true, props.withoutIds);
			}else{
				// Departing source mode. Grab the value from the input ref:
				if(this.ir){
					var val = this.ir.onGetValue ? this.ir.onGetValue(null, this.ir) : this.ir.value;
					
					setTimeout(() => {
						
						props.snapshotState(props.canvasState.load(val));
						
					}, 1);
				}
			}
		}
	}
	
	findParentElement(domNode, wantedClass){
		while(domNode != null){

			if (domNode.classList.contains(wantedClass)) {
				return domNode;
            }
			
			domNode = domNode.parentElement;
		}
		
		return domNode;
	}
	
	render() {
		var {error} = this.state;
		var {fullscreen, canvasState} = this.props;
		var node = canvasState.node;
		
		var { toolbar, name } = this.props;
		
		if(canvasState.sourceMode){
			return <div className="rich-editor with-toolbar">
				{!fullscreen && <div className="rte-toolbar-source">
						<button onClick={e => {
							e.preventDefault();
							this.props.onSetShowSource(false);
						}}>Preview</button>
				</div>}
				{error && <Alert type='error'>
					<p>
						<b>Uh oh!</b>
					</p>
					<p>
						This editor wasn't able to load a preview because your JSON is invalid.
					</p>
					<p>{error.e.message}</p>
				</Alert>}
				<Input 
					inputRef={ir=>this.ir=ir}
					name={name}
					type="textarea"
					contentType="application/json"
					className="form-control json-preview"
					defaultValue={this.sourceModeValue}
				/>
			</div>;
		}

		var reClass = ['rich-editor'];
		reClass.push(toolbar ? 'with-toolbar' : 'no-toolbar');

		if (this.props.textonly) {
			reClass.push('rich-editor--text-only');
        }
		
		return <div className={reClass.join(' ')} data-theme={"main"} onContextMenu={this.onContextMenu}>
			<div ref={node.dom} className="rte-content" 
				onKeyDown={this.onKeyDown} onDragStart={this.onReject}>
				{this.renderRootNode(node, canvasState)}
			</div>
			<input ref={ir=>{
				this.mainIr=ir;
				if(ir){
					ir.onGetValue=(val, ele)=>{
						if(ele == ir){
							var canvasState = this.props.canvasState;
							
							if(canvasState.graphState){
								// The graph view is open. Update its value into the canvas state first.
								// We don't have a reference to the hidden input field though, so we'll use its class to find it.
								var parentPanels = this.findParentElement(ir, "panelled-editor");
								var inputs = parentPanels.getElementsByClassName("graph-editor-input-field");
								
								if(inputs.length){
									var graphNode = inputs[0];
									var val = graphNode.onGetValue ? graphNode.onGetValue(null, graphNode) : graphNode.value;
									
									// Update the graph:
									canvasState.selectedNode.graph = new Graph(val);
									
									// Snapshot it:
									var cs = this.props.snapshotState();
									return cs.toCanvasJson(true);
								}
							}
							
							return canvasState.toCanvasJson(true);
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
							var decorator = createLinkDecorator();
							
							if(selectOpenFor.insertIndex !== undefined){
								// Insert directly into the given array at a specified index.
								insertIndex = selectOpenFor.insertIndex;
								insertInto = selectOpenFor.node;
							}else{
								var insertInto = selectOpenFor.node.parent;
								var insertIntoRoot = false;
								
								if(!insertInto) {
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
							}
							
							var pubName = module.publicName;
							
							var module = {
								type: module.moduleClass,
								parent: insertInto,
								props: {}
							}
							
							if(pubName == "UI/Text"){
								module.type = "richtext";
								module.editorState = EditorState.createEmpty(decorator);
							}else{
								module.typeName = pubName;
							}
							
							// Build the root set
							var roots = {};
							
							// Does the type have any roots that need adding?
							var rootSet = getRootInfo(module.type);

							for(var i=0;i<rootSet.length;i++){
								var rootInfo = rootSet[i];
								if(!roots[rootInfo.name]){
									// Adding an empty root with an RTE.
									var rootObj = {content: []};
									var emptyPara = {type: 'richtext', editorState: EditorState.createEmpty(decorator)};
									emptyPara.parent = rootObj;
									rootObj.content.push(emptyPara);
									roots[rootInfo.name] = rootObj;
								}
							}
							
							if(Object.keys(roots).length){
								module.roots = roots;
							}
							
							this.props.canvasState.addNode(module, insertInto, insertIndex, selectOpenFor.isReplace);

						applyCustomNodeUpdate(module);

							this.props.snapshotState();
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
	
}