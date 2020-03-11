import omit from 'UI/Functions/Omit';
import Text from 'UI/Text';
import Modal from 'UI/Modal';
import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Input from 'UI/Input';
import expand from 'UI/Functions/CanvasExpand';
import webRequest from 'UI/Functions/WebRequest';
import getContentTypes from 'UI/Functions/GetContentTypes';

// Connect the input "ontypecanvas" render event:

var eventHandler = global.events.get('UI/Input');

eventHandler.ontypecanvas = function(props, _this){
	
	return <CanvasEditor 
		id={props.id || _this.fieldId}
		className={props.className || "form-control"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
	
};

eventHandler.ontyperenderer = function(props, _this){
	
	return <CanvasEditor 
		moduleSet='renderer'
		id={props.id || _this.fieldId}
		className={props.className || "form-control"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
	
};

var __moduleGroups = null;

function CanvasSelection(){
	
	this.allChildren = (node, result, exclude) => {
		for(var i=0;i<node.childNodes.length;i++){
			this.allChildren(node.childNodes[i], result);
		}
		if(!exclude || !node.childNodes.length){
			result.push(node);
		}
	};
	
	this.surroundTextWith = (eleName) => {
		eleName = eleName.toUpperCase();
		allSelected.forEach(selected => {
			var el = selected.element;
			if(el.nodeName != '#text'){
				return;
			}
			
			if(selected.offset){
				// Only doing a substring of the text
			}
			
			console.log("Create and parent", el);
			
			if(!this.hasParent(el, eleName)){
				var newElement = document.createElement(eleName);
				el.parentNode.insertBefore(newElement, el);
				el.parentNode.removeChild(el);
				newElement.appendChild(el);
			}else{
				console.log("Remove the parent");
				// Delete the parent via also inversing the selection, and then re-adding it.
			}
			
		});
		
	};
	
	var sel = global.getSelection();
	var range = sel.getRangeAt(0);
	var ancestor = range.commonAncestorContainer;
	var allWithinRange = [];
	this.allChildren(ancestor, allWithinRange, true);
	
	// The selection runs backwards if the target is before the anchor.
	var relativePosition = sel.anchorNode.compareDocumentPosition(sel.focusNode);
	var backwards = ((!relativePosition && sel.anchorOffset > sel.focusOffset) || relativePosition === 2 ||  relativePosition === 8);
	
	var allSelected = [];
	for (var i=0; i<allWithinRange.length; i++) {
		var element = allWithinRange[i];
		// The second parameter says to include the element 
		// even if it's not fully selected
		if (sel.containsNode(element, true) ) {
			
			var result = {element};
			var isAnchor = (element == sel.anchorNode);
			// if it's the anchor or target element, then it's partially selected.
			if(isAnchor){
				result.end = backwards ? 1 : 2;
				result.offset = sel.anchorOffset;
			}
			
			if(element == sel.focusNode){
				result.end = backwards ? 2 : 1;
				if(isAnchor){
					// This element is actually both things.
					if(backwards){
						result.offset = sel.focusOffset;
						result.length = sel.anchorOffset - sel.focusOffset;
					}else{
						result.length = sel.focusOffset - sel.anchorOffset;
					}
				}else{
					result.offset = sel.focusOffset;
				}
			}
			
			allSelected.push(result);
		}
	}
	
	this.allSelected = allSelected;
	this.ancestor = ancestor;
	this.backwards = backwards;
	this.startNode = backwards ? sel.focusNode : sel.anchorNode;
	
	this.hasParent = (e, eleName) => {
		var current=e;
		while(current){
			if(current.nodeName == eleName){
				return true;
			}
			current = current.parentNode;
		}
		
		return false;
	};
	
}

export default class CanvasEditor extends React.Component {
	
	constructor(props){
		super(props);
		this.state = {
			content: this.loadJson(props),
		};
		this.buildJson = this.buildJson.bind(this);
		this.closeModal = this.closeModal.bind(this);
	}
	
	componentWillReceiveProps(props){
		this.setState({content: this.loadJson(props)});
	}
	
	loadJson(props){
		var json = props.value || props.defaultValue;
		if(typeof json === 'string'){
			json = JSON.parse(json);
		}
		
		if(!json || (!json.module && !json.content && !Array.isArray(json))){
			json = {content: ''};
		}
		
		return expand(json, null, null, true);
	}
	
	closeModal(){
		this.setState({
			selectOpenFor: null,
			optionsVisibleFor: null
		});
		this.updated();
	}
	
	collectModules(){
		// __mm is the superglobal used by socialstack 
		// to hold all available modules.
		
		var sets = {standard:{}, renderer: {}};
		__moduleGroups={};
		
		for(var modName in __mm){
			// Attempt to get React propTypes.
			var module = require(modName).default;
			
			if(!module){
				continue;
			}
			
			var props = module.propTypes;
			
			if(!props){
				// This module doesn't have a public interface.
				continue;
			}
			
			var set = sets[module.moduleSet || 'standard'];
			
			// modName is e.g. UI/Thing/Thing.js
			
			// Remove the filename, and get the super group:
			var nameParts = modName.split('/');
			nameParts.pop(); 
			var publicName = nameParts.join('/');
			var name = nameParts.pop();
			
			if(nameParts[0] == 'UI'){
				nameParts.shift();
			}
			
			var group = nameParts.join(' > ');
			
			if(!set[group]){
				group = set[group] = {name: group, modules: []};
			}else{
				group = set[group];
			}
			
			group.modules.push({
				name,
				publicName,
				props,
				moduleClass: module
			});
		}
		
		for(var setName in sets){
			var set = sets[setName];
			
			var moduleGroups = [];
			if(set[""]){
				// UI group first always:
				moduleGroups.push(set[""]);
				delete set[""];
			}
			
			for(var gName in set){
				moduleGroups.push(set[gName]);
			}
			
			__moduleGroups[setName] = moduleGroups;
		}
		
	}
	
	addModule(moduleInfo, contentNode){
		var content = contentNode.content;
		var changed = false;
		
		if(!Array.isArray(content)){
			content = [content];
			changed = true;
		}
		
		content.push({
			module: moduleInfo.moduleClass,
			moduleName: moduleInfo.publicName,
			expanded: true
		});
		
		if(changed){
			if(contentNode == this.state){
				this.setState({
					content,
					selectOpenFor: null
				});
			}else{
				contentNode.content = content;
				contentNode.useCanvasRender = true;
				this.setState({
					selectOpenFor: null
				});
			}
		}else{
			this.setState({
				selectOpenFor: null
			});
		}
		
		this.updated();
	}
	
	updated(){
		this.ref.value=JSON.stringify(this.buildJson());
		this.props.onChange && this.props.onChange({
			target: this.ref
		});
	}
	
	renderModuleSelection(){
		
		var set = null;
		
		if(this.state.selectOpenFor){
			if(!__moduleGroups){
				this.collectModules();
			}
			set = this.props.moduleSet ? __moduleGroups[this.props.moduleSet] : __moduleGroups.standard;
		}
		
		return (<Modal
			className={"module-select-modal"}
			buttons={[
				{
					label: "Close",
					onClick: this.closeModal
				}
			]}
			title={"Add something to your content"}
			onClose={this.closeModal}
			visible={this.state.selectOpenFor}
		>
			{set ? set.map(group => {
				return <div className="module-group">
					<h6>{group.name}</h6>
					<Loop asCols over={group.modules} size={4}>
						{module => {
							return <div className="module-tile" onClick={() => {
									this.addModule(module, this.state.selectOpenFor === true ? this.state : this.state.selectOpenFor);
								}}>
								<div>
									{<i className={"fa fa-" + (module.moduleClass.icon || "puzzle-piece")} />}
								</div>
								{module.name}
							</div>;
							
						}}
					</Loop>
				</div>;
				
			}) : null}
		</Modal>);
	}
	
	buildJson(){
		return this.buildJsonNode(this.state.content, 0, true);
	}
	
	buildJsonNode(contentNode, index, isRoot){
		
		if(!contentNode){
			return null;
		}
		
		if(Array.isArray(contentNode)){
			return contentNode.map((node, index) => this.buildJsonNode(node, index, false));
		}
		
		if(contentNode.module == Text){
			return isRoot ? {
				content: contentNode.content
			} : contentNode.content;
		}
		
		// Otherwise, get the module name:
		var result = {
			module: contentNode.moduleName,
			data: contentNode.data
		};
		
		if(contentNode.content){
			result.content = this.buildJsonNode(contentNode.content,0);
		}
		
		return result;
	}
	
	renderOptionsModal(){
		var content = this.state.optionsVisibleFor;
		if(!content){
			return;
		}
		return <Modal
			className={"module-options-modal"}
			buttons={[
				{
					label: "Close",
					onClick: this.closeModal
				}
			]}
			title={this.displayModuleName(content.moduleName)}
			onClose={this.closeModal}
			visible={true}
		>
			{
				this.renderOptions(this.state.optionsVisibleFor)
			}
		</Modal>
	}
	
	findParent(contentNode, current){
		if(!current){
			current = this.state;
		}
		
		var a = current.content;
		
		if(contentNode == a){
			return current;
		}
		
		if(!a){
			return null;
		}
		
		if(Array.isArray(a)){
			for(var i=0;i<a.length;i++){
				var node = a[i];
				if(!node){
					continue;
				}
				
				if(node == contentNode){
					return current;
				}
				
				var parent = this.findParent(contentNode, node);
				if(parent){
					return parent;
				}
			}
		}else if(a.content){
			return this.findParent(contentNode, a);
		}
		return null;
	}
	
	removeFrom(contentNode, parent){ // if you use this, must also call this.updated() or .closeModal()
		if(parent.content == contentNode){
			parent.content = [];
			return true;
		}
		
		var count = parent.content.length;
		parent.content = parent.content.filter(node => node != contentNode);
		return count != parent.content.length;
	}
	
	renderDelete(contentNode){
		return (
			<div style={{marginBottom: '10px'}}>
				<div className="btn btn-danger" onClick={() => {
					var parent = this.findParent(contentNode);
					this.removeFrom(contentNode, parent);
					this.closeModal();
				}}>
					Delete
				</div>
			</div>
		);
	}
	
	renderOptions(contentNode){
		if(!contentNode){
			return;
		}
		
		var Module = contentNode.module || "div";
		var dataValues = {...contentNode.data};
		
		var dataFields = {};
		var atLeastOneDataField = false;
		if(Module.propTypes){
			for(var fieldName in Module.propTypes){
				if(fieldName == 'children'){
					continue;
				}
				var info = Module.propTypes[fieldName];
				dataFields[fieldName] = {type: info, value: dataValues[fieldName]};
				atLeastOneDataField = true;
			}
		}
		
		for(var fieldName in dataValues){
			if(dataFields[fieldName]){
				continue;
			}
			
			// This data field is in the JSON, but not a prop. The module is essentially missing its public interface.
			// We'll still display it though so it can be edited:
			dataFields[fieldName] = {type: 'string', value: dataValues[fieldName]};
			atLeastOneDataField = true;
		}
		
		if(atLeastOneDataField){
			return (<div>
				{this.renderDelete(contentNode)}
				{
				Object.keys(dataFields).map(fieldName => {
					
					// Very similar to how autoform works - auto deduce various field types whenever possible, based on field naming conventions.
					var fieldInfo = dataFields[fieldName];
					var label = fieldName;
					var inputType = 'text';
					var inputContent = undefined;
					
					if(fieldName.endsWith("Ref")){
						inputType = 'file';
						label = label.substring(0, label.length-3);
					}else if(Array.isArray(fieldInfo.type)){
						inputType = 'select';
						inputContent = fieldInfo.type.map(name => {
							return (
								<option value={name}>{name}</option>
							);
						})
					}else if(fieldInfo.type == 'color'){
						inputType = 'color';
					}else if(fieldInfo.type && fieldInfo.type.type == 'id'){
						inputType = 'select';
						inputContent = this.getContentDropdown(fieldInfo);
					}else if(fieldInfo.type == 'set' || (fieldInfo.type && fieldInfo.type.type == 'set')){
						
						return this.renderSetSelection(fieldInfo, label);
						
					}
					
					// helloWorld -> Hello World.
					label = label.replace(/([^A-Z])([A-Z])/g, '$1 $2');
					label = label[0].toUpperCase() + label.substring(1);
					
					// The value might be e.g. a url ref.
					var val = fieldInfo.value;
					
					if(val && val.type){
						val = JSON.stringify(val);
					}
					
					return <Input label={label} type={inputType} defaultValue={val} onChange={e => {
						if(!contentNode.data){
							contentNode.data = {};
						}
						
						contentNode.data[fieldName] = fieldInfo.value = e.target.value;
					}} onKeyUp={e => {
						if(!contentNode.data){
							contentNode.data = {};
						}
						
						contentNode.data[fieldName] = fieldInfo.value = e.target.value;
					}}>{inputContent}</Input>;
				})
			}</div>);
		}
		
		return <div>
			{this.renderDelete(contentNode)}
			No other options available
		</div>;
	}
	
	getContentDropdown(fieldInfo){
		
		if(!fieldInfo.type.loadedSet){
			fieldInfo.type.loadedSet = [];
			
			webRequest(fieldInfo.type.content.toLowerCase() + '/list').then(result => {
				fieldInfo.type.loadedSet = result.json.results;
				this.setState({});
			});
		}
		
		return fieldInfo.type.loadedSet.map(item => {
			var name = (item.name || item.title || item.firstName || 'Untitled') + ' (#' + item.id + ')';
			
			return (
				<option value={item.id}>{name}</option>
			);
		});
	}
	
	getContentTypeDropdown(fieldInfo){
		if(!fieldInfo.type.loadedTypes){
			fieldInfo.type.loadedTypes = [];
			
			getContentTypes().then(set => {
				fieldInfo.type.loadedTypes = set;
				this.setState({});
			});
		}
		
		return fieldInfo.type.loadedTypes.map(item => {
			var name = item.name;
			
			return (
				<option value={item.name}>{name}</option>
			);
		});
	}
	
	renderSetSelection(fieldInfo, label){
		
		// helloWorld -> Hello World.
		label = label.replace(/([^A-Z])([A-Z])/g, '$1 $2');
		label = label[0].toUpperCase() + label.substring(1);
		var val = fieldInfo.value || {type: 'set', renderer: JSON.stringify({module: fieldInfo.type.defaultRenderer}) || '[]'};
		
		return [
			<Input label='Content Type' type='select' defaultValue={val.contentType} onChange={e => {
				if(!contentNode.data){
					contentNode.data = {};
				}
				val.contentType = e.target.value;
			}}>{this.getContentTypeDropdown(fieldInfo)}</Input>,
			<label>Content Filter</label>,
			'Filter coming soon',
			<Input label='Content Renderer' type='renderer' defaultValue={val.renderer} onChange={e => {
				if(!contentNode.data){
					contentNode.data = {};
				}
				val.renderer = e.target.value;
			}} />
		];
		
	}
	
	renderNode(contentNode){
		if(!contentNode){
			return null;
		}
		
		if(Array.isArray(contentNode)){
			return contentNode.map((e,i)=>this.renderNode(e,i));
		}
		
		var Module = contentNode.module || "div";
		
		var result = null;
		
		if(Module == Text){
			
			var divRef = null;
			
			result = (
				<div
					contenteditable
					onInput={e => {
						contentNode.content = e.target.innerHTML;
						this.updated();
					}}
					onKeyDown={e => {
						
						if(!e.ctrlKey){
							return;
						}
						
						switch(e.keyCode){
							case 73:
								// ctrl+i.
								e.preventDefault();
								var selection = new CanvasSelection(this);
								selection.surroundTextWith('i');
							break;
							case 66:
								// ctrl+b.
								e.preventDefault();
								var selection = new CanvasSelection(this);
								selection.surroundTextWith('b');
							break;
							case 85:
								// ctrl+u.
								e.preventDefault();
								var selection = new CanvasSelection(this);
								selection.surroundTextWith('u');
							break;
							case 75:
								// ctrl+k. (hyperlink)
								e.preventDefault();
								var selection = new CanvasSelection(this);
								selection.surroundTextWith('a');
							break;
							
							// Canvas level ones like ctrl+z, ctrl+s, ctrl+y are handled elsewhere.
							// copy/paste is handled by the browser
						}
						
						
					}}
					className="canvas-editor-text"
					dangerouslySetInnerHTML={{__html: contentNode.content}}
				/>
			);
		}else{
			
			result = (
				<Module {...contentNode.data}>
					{contentNode.useCanvasRender ? this.renderNode(contentNode.content) : null}
					{Module.propTypes && Module.propTypes.children && !this.props.minimal && (
						<div style={{marginTop: '10px'}}>
							<div className="btn btn-secondary" onClick={() => {this.setState({selectOpenFor: contentNode})}}>
								<i className="fa fa-plus" />
							</div>
						</div>
					)}
				</Module>
			);
			
		}
		
		return (
			<div className="module-info">
				{!this.props.minimal && (
					<div className="btn btn-secondary" style={{marginBottom: '5px'}} onClick={() => {
						this.setState({optionsVisibleFor: contentNode});
					}}><i className="fa fa-cog"/>
					{' ' + this.displayModuleName(contentNode.moduleName)}
					</div>
				)}
				{result}
				
			</div>
		);
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
	
	render(){
		return (
			<div className="canvas-editor">
				{this.props.onPageUrl && (
					<div style={{marginBottom: '10px'}}>
						<a href={this.props.onPageUrl + '#edit=' + this.props.contentType + '&id=' + this.props.contentId + '&field=' + this.props.name}>
							<div className="btn btn-primary">
								Edit on page
							</div>
						</a>
					</div>
				)}
				<input type="hidden" name={this.props.name} ref={ref => {
					this.ref = ref;
					if(ref){
						ref.value=JSON.stringify(this.buildJson());
					}
				}}/>
				<div>
					{this.renderNode(this.state.content,0)}
					{!this.props.minimal && (
						<div style={{clear: 'both', marginTop: '10px'}}>
							<div className="btn btn-secondary" onClick={() => {this.setState({selectOpenFor: true})}}>
								<i className="fa fa-plus" />
							</div>
						</div>
					)}
				</div>
				{this.renderModuleSelection()}
				{this.renderOptionsModal()}
			</div>
		);
	}
}