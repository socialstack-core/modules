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
const { EditorState } = Draft;
import ThemeEditor from 'Admin/CanvasEditor/ThemeEditor';
import PanelledEditor from 'Admin/Layouts/PanelledEditor';
import CanvasState from './CanvasState';
import { getRootInfo } from './Utils';
var nodeKeys = 1;

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

function renderStructureNode(node, canvasState, onClick) {
	var nodeName = node.typeName || node.type;
	
	if(node.graph){
		nodeName = niceGraphName(node.graph);
	}
	
	if (!node.roots ||
		!node.roots.children ||
		!node.roots.children.content) {
		return <>
			<button type="button" className="btn panelled-editor__structure-item-text" onClick={(e) => {
				e.preventDefault();
				e.stopPropagation();
				onClick && onClick(node);
			}}>
				{nodeName}
			</button>
			<button type="button" className="btn btn-sm btn-outline-danger btn-remove" title={`Remove`}
				onClick={() => console.log('remove node TODO')}>
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
		onClick: function () {
			console.log('remove node TODO')
		}
	};

	return <Collapsible title={nodeName} expanderLeft onClick={() => onClick(node)} buttons={[removeButton]}>
		{node.roots.children.content.map((node, i) => {
			var itemClass = ['collapsible-content__wrapper'];

			if (node == canvasState.selectedNode) {
				itemClass.push('collapsible-content__wrapper--selected');
			}

			return <span className={itemClass.join(' ')}>
				{renderStructureNode(node, canvasState, onClick)}
			</span>
		})}
	</Collapsible>;
}

function renderStructure(canvasState, onClick) {
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
			{renderStructureNode(node, canvasState, onClick)}
		</li>;
	});

}


export default function CanvasEditor (props) {
	
	var [canvasState, setCanvasState] = React.useState(() => {
		var state = new CanvasState();
		state = state.load(props.value || props.defaultValue);
		return state;
	});
	
	var ceCore = <CanvasEditorCore {...props} onSetShowSource={(state) => {
		setCanvasState(canvasState.setShowSourceState(state));
	}} fullscreen={props.fullscreen} canvasState={canvasState} snapshotState={(snap) => {
		
		// Create a snapshot if one is not being provided:
		var newState = snap || canvasState.addStateSnapshot();
		
		// Update:
		setCanvasState(newState);
		
	}} />;

	var ctx = {};
	
	if(props.primary){
		ctx.primary = props.primary;
	}
	
	var coreWrapper = ['rte-component__core-wrapper'];

	if (canvasState.sourceMode) {
		coreWrapper.push('rte-component__core-wrapper--100');
    }

	if(props.fullscreen){
		return <PanelledEditor 
			controls={props.controls}
			feedback={props.feedback}
				rightPanel={() => {
					
					if(!canvasState.selectedNode){
						return null;
					}
					
					// Note: The propEditor must not have any named fields. If it did, they would be submitted when the content is saved.
					return <>
						<PropEditor optionsVisibleFor={canvasState.selectedNode} onChange={() => {
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
						setCanvasState(canvasState.selectNode(node));
						
					});
				}}
			>
			{/* Uses display:none to ensure it always exists in the DOM such that the save button submits everything */}
			<div className={coreWrapper.join(' ')} style={(canvasState.graphState || canvasState.themeState) ? { display: 'none' } : undefined}>
				{ceCore}
			</div>
			{canvasState.graphState && <Input type='graph' objectOutput={true} inputRef={ir=>{
				this.graphIr=ir;
			}} value={canvasState.selectedNode.graph.structure} context={ctx}/>}
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
	
	renderNode(node, canvasState){
		var NodeType = node.type;
		
		if(!node.dom){
			node.dom = React.createRef();
		}
		
		if(!node.key){
			node.key = nodeKeys++;
		}

		var rteClass = node == canvasState.selectedNode ? "rte-component rte-component--selected" : "rte-component";
		
		if(NodeType === 'richtext'){
			// Pass the whole node to the RTE.
			return <div key={node.key} ref={node.dom} className={rteClass} {...node.props}>
				<RichEditor editorState={node.editorState} textonly={this.props.textonly} onAddComponent={() => {
					this.setState({selectOpenFor: {node, isReplace: true}});
				}} onStateChange={(newState) => {
					node.editorState = newState;
					this.props.snapshotState();
				}}/>
			</div>;
		}else if(node.graph){
			
			// Graph node.
			return <ErrorCatcher node={node}>{node.graph.render()}</ErrorCatcher>;
		
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

					var rendered = <div key={root.key} className={rteClass} ref={root.dom}>{this.renderRootNode(root, canvasState)}</div>;

					if(isChildren){
						children = rendered;
					}else{
						props[k] = rendered;
					}
				}
				
				return <div key={node.key} className={rteClass} ref={node.dom}>
						<ErrorCatcher node={node}><NodeType {...props}>{children}</NodeType></ErrorCatcher>
					</div>;
				
			}else{
				// It has no content inside it; it's purely config driven.
				return <div key={node.key} className={rteClass} ref={node.dom}>
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
			
			var className = domNode.className;
			
			if(className == wantedClass){
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
		
		return <div className={"rich-editor " + (toolbar ? "with-toolbar" : "no-toolbar")} data-theme={"main"} onContextMenu={this.onContextMenu}>
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
								module.type = "p";
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
									var emptyPara = {type: 'richtext', editorState: EditorState.createEmpty()};
									emptyPara.parent = rootObj;
									rootObj.content.push(emptyPara);
									roots[rootInfo.name] = rootObj;
								}
							}
							
							if(Object.keys(roots).length){
								module.roots = roots;
							}
							
							this.props.canvasState.addNode(module, insertInto, insertIndex, selectOpenFor.isReplace);
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