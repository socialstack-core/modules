import omit from 'UI/Functions/Omit';
import Text from 'UI/Text';
import Modal from 'UI/Modal';
import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Input from 'UI/Input';
import {expand, mapTokens} from 'UI/Functions/CanvasExpand';
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

var __contentTypeCache = null;
var __contentCacheByType = {};
var __cachedForms = null;

var __linkTypes = null;

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
		this.closeLinkModal = this.closeLinkModal.bind(this);
	}
	
	componentWillReceiveProps(props){
		this.setState({content: this.loadJson(props)});
	}
	
	loadJson(props){
		var json = props.value || props.defaultValue;
		if(typeof json === 'string'){
			json = json.length ? JSON.parse(json) : null;
		}
		
		if(!json || (!json.module && !json.content && !Array.isArray(json))){
			json = {content: ''};
		}
		
		return expand(json);
	}
	
	closeModal(){
		this.setState({
			selectOpenFor: null,
			optionsVisibleFor: null
		});
		this.updated();
	}
	
	closeLinkModal(){
		this.setState({
			linkSelectionFor: null
		});
		this.updated();
	}
	
	collectLinkTypes(){
		
		if(__linkTypes == null){
			
			var all = [];
			
			__linkTypes = {all};
			
			all.push({
				icon: 'terminal',
				type: 'urlToken',
				propTypes: {
					name: 'string'
				}
			});
			
			all.push({
				icon: 'server',
				type: 'endpoint',
				propTypes: {
					url: 'string'
				}
			});
			
			all.push({
				icon: 'list',
				type: 'set',
				propTypes: {
					contentType: 'contentType',
					filter: {
						type: 'filter',
						forContent: {
							type: 'field',
							name: 'contentType'
						}
					},
					renderer: {
						type: 'renderer',
						forContent: {
							type: 'field',
							name: 'contentType'
						}
					}
				}
			});
			
			var map = __linkTypes.map = {};
			
			all.forEach(linker => {
				if(linker.name){
					return;
				}
				var type = linker.type;
				map[type] = linker;
				linker.name = this.niceName(type);
			});
		}
		
		// __linkTypes is any globally available ones.
		// Next add scope-specific ones (such as fields available to a renderer).
		var all = __linkTypes.all;
		var map = __linkTypes.map;
		if(this.props.itemMeta){
			var fieldLinker = {
				icon: '',
				type: 'field',
				name: this.niceName(this.props.itemMeta.contentType),
				propTypes: {
					name: this.props.itemMeta.fields.map(field => field.data.name)
				}
			};
			
			all = __linkTypes.all.concat(fieldLinker);
			map = {...map};
			map[fieldLinker.type] = fieldLinker;
		}
		
		return {
			all,
			map
		};
		
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
			var setsToJoin = null;
			
			if(module.rendererPropTypes){
				// Both sets
				setsToJoin = [sets.standard, sets.renderer];
			}else{
				// 1 set
				setsToJoin=[sets[module.moduleSet || 'standard']];
			}
			
			for(var i=0;i<setsToJoin.length;i++){
				var set = setsToJoin[i];
				if(!set[group]){
					group = set[group] = {name: group, modules: []};
				}else{
					group = set[group];
				}
				
				group.modules.push({
					name,
					publicName,
					props: set == sets.renderer && module.rendererPropTypes ? module.rendererPropTypes : props,
					moduleClass: module
				});
			}
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
		var json = this.buildJson();
		this.props.onChange && this.props.onChange({
			target: this.ref,
			json
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
			isLarge
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
	
	onChange(){
		this.setState({});
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
		
		var data = {...contentNode.data};
		
		if(data){
			// If any are renderers then we need to build those too as they'll be expanded.
			for(var field in data){
				if(data[field] && data[field].type == 'set'){
					var setInfo = {...data[field]};
					setInfo.renderer = this.buildJsonNode(data[field].renderer, 0, true);
					data[field] = setInfo;
				}
			}
		}
		
		// Otherwise, get the module name:
		var result = {
			module: contentNode.moduleName,
			data
		};
		
		if(contentNode.content){
			result.content = this.buildJsonNode(contentNode.content,0);
		}
		
		return result;
	}
	
	renderLinkSelectionModal(){
		var lsf = this.state.linkSelectionFor;
		if(!lsf){
			return;
		}
		
		var targetNode = lsf.targetNode;
		var fieldInfo = lsf.fieldInfo;
		var scopeLinks = this.collectLinkTypes().all;
		
		return <Modal
			className={"module-link-selection-modal"}
			buttons={[
				{
					label: "Close",
					onClick: this.closeLinkModal
				}
			]}
			title={'Linking ' + fieldInfo.name}
			onClose={this.closeLinkModal}
			visible={true}
		>
			<p>
				Your field value can also be set dynamically - this is called linking. For example if you want it to come from something in the URL. Here's the linking options you have:
			</p>
			<Loop asCols over={scopeLinks} size={4}>
				{linker => {
					return <div className="module-tile" onClick={() => {
							//if(targetNode.module){
								if(!targetNode.data){
									targetNode.data = {};
								}
								targetNode.data[fieldInfo.name] = {
									type: linker.type
								};
							/*}else{
								targetNode[fieldInfo.name] = {
									type: linker.type
								};
							}*/
							
							this.closeLinkModal();
						}}>
						<div>
							{<i className={"fa fa-" + (linker.icon || "puzzle-piece")} />}
						</div>
						{linker.name}
					</div>;
					
				}}
			</Loop>
		</Modal>
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
	
	renderOptions(contentNode, module){
		if(!contentNode){
			return;
		}
		
		var Module = module || contentNode.module || "div";
		var dataValues = {...contentNode.data};
		
		var props = Module.propTypes;
		
		if(this.props.moduleSet == 'renderer'){
			props = Module.rendererPropTypes || props;
		}
		
		var dataFields = {};
		var atLeastOneDataField = false;
		if(props){
			for(var fieldName in props){
				if(this.specialField(fieldName)){
					continue;
				}
				var propType = props[fieldName];
				if(!propType.type){
					propType = {type: propType};
				}
				dataFields[fieldName] = {propType, value: dataValues[fieldName]};
				atLeastOneDataField = true;
			}
		}
		
		for(var fieldName in dataValues){
			if(dataFields[fieldName]){
				continue;
			}
			
			// This data field is in the JSON, but not a prop. The module is essentially missing its public interface.
			// We'll still display it though so it can be edited:
			dataFields[fieldName] = {propType: {type: 'string'}, value: dataValues[fieldName]};
			atLeastOneDataField = true;
		}
		
		if(atLeastOneDataField){
			return (<div>
				{this.renderDelete(contentNode)}
				{this.renderOptionSet(dataFields, contentNode)}
			</div>);
		}
		
		return <div>
			{this.renderDelete(contentNode)}
			No other options available
		</div>;
	}
	
	specialField(fieldName){
		return fieldName == 'children' || fieldName == 'editButton'
	}
	
	renderOptionSet(dataFields, targetNode){
		
		var options = [];
		
		Object.keys(dataFields).forEach(fieldName => {
			
			// Very similar to how autoform works - auto deduce various field types whenever possible, based on field naming conventions.
			var fieldInfo = dataFields[fieldName];
			fieldInfo.name = fieldName;
			var label = fieldName;
			var inputType = 'text';
			var inputContent = undefined;
			var propType = fieldInfo.propType;
			
			if(propType.type == 'set' && (!fieldInfo.value || !fieldInfo.value.type)){
				// The same as rendering a set linker.
				this.updateField(targetNode, fieldInfo, {
					type: 'set',
					renderer: {
						module: propType.defaultRenderer
					}
				});
			}
			
			if(fieldInfo.value && fieldInfo.value.type && fieldInfo.value.type != 'module'){
				// It's a linked field.
				// Show its options instead.
				var typeInfo = this.collectLinkTypes().map[fieldInfo.value.type];
				
				var fields = typeInfo.propTypes;
				var linkDF = {};
				
				for(var linkFN in typeInfo.propTypes){
					if(this.specialField(linkFN)){
						continue;
					}
					var linkPT = typeInfo.propTypes[linkFN];
					if(!linkPT.type){
						linkPT = {type: linkPT};
					}
					linkDF[linkFN] = {propType: linkPT, value: fieldInfo.value[linkFN]};
				}
				
				// Add the options:
				options.push(
					<div>
						<label>
							{this.niceName(label)}
						</label>
						<div style={{padding: '10px', border: '1px solid lightgrey'}}>
							<p style={{color: 'lightgrey'}}>
								{typeInfo.name}
							</p>
							{this.renderOptionSet(linkDF, fieldInfo.value)}
						</div>
					</div>
				);
				return;
			}
			
			var extraProps = {};
			
			if(fieldName.endsWith("Ref")){
				inputType = 'file';
				label = label.substring(0, label.length-3);
			}else if(Array.isArray(propType.type)){
				inputType = 'select';
				inputContent = propType.type.map(name => {
					return (
						<option value={name}>{name}</option>
					);
				})
				inputContent.unshift(<option>Pick a value</option>);
			}else if(propType.type == 'color'){
				inputType = 'color';
			}else if(propType.type == 'checkbox' || propType.type == 'bool' || propType.type == 'boolean'){
				inputType = 'checkbox';
			}else if(propType.type == 'id'){
				inputType = 'select';
				inputContent = this.getContentDropdown(propType.content || propType.contentType);
				inputContent.unshift(<option>Pick some content</option>);
			}else if(propType.type == 'contentType'){
				inputType = 'select';
				inputContent = this.getContentTypeDropdown();
				inputContent.unshift(<option>Pick a content type</option>);
			}else if(propType.type == 'filter'){
				return ("Filter WIP");
			}else if(propType.type == 'renderer'){
				inputType = 'renderer';
				
				// resolve forContent.
				if(propType.forContent){
					var contentType = propType.forContent;
					if(propType.forContent.type == "field"){
						var contentTypeField = dataFields[propType.forContent.name];
						if(contentTypeField){
							contentType = contentTypeField.value;
						}else{
							contentType = null;
						}
					}
				}
				
				// Get the info for that content type, if we haven't already.
				if(contentType){
					this.getFieldsForType(contentType, (fields, fromCache) => {
						if(fromCache){
							extraProps.itemMeta = {
								fields,
								contentType
							};
						}else{
							this.setState({});
						}
						
					});
				}
			}
			
			// The value might be e.g. a url ref.
			var val = fieldInfo.value;
			
			if(val && val.type){
				val = JSON.stringify(val);
			}
			
			options.push(
				<Input label={this.getLinkLabel(this.niceName(label), targetNode, fieldInfo)} type={inputType} defaultValue={val} onChange={e => {
					var value = e.target.value;
					
					if(inputType == 'renderer'){
						value = e.json;
					}else if(inputType == 'checkbox'){
						value = !!e.target.checked;
					}
					
					this.updateField(targetNode, fieldInfo, value);
				}} onKeyUp={e => {
					var value = e.target.value;
					
					if(inputType == 'renderer'){
						value = e.json;
					}else if(inputType == 'checkbox'){
						value = !!e.target.checked;
					}
					
					this.updateField(targetNode, fieldInfo, value);
				}} {...extraProps}>{inputContent}</Input>
			);
		});
		
		return options;
	}
	
	getFieldsForType(contentType, cb){
		contentType = contentType.toLowerCase();
	
		if(__cachedForms){
			return cb(__cachedForms[contentType], true);
		}
		
		__cachedForms = {};
		webRequest("autoform").then(result => {
			var structure = result.json;
			for(var i=0;i<structure.forms.length;i++){
				var form = structure.forms[i];
				__cachedForms[form.endpoint.substring(3)] = form.fields;
			}
			cb(__cachedForms[contentType]);
		});
	}
	
	updateField(targetNode, fieldInfo, value){
		fieldInfo.value = value;
		if(targetNode.module){
			// Targeting a contentNode. Must go into the data array.
			if(!targetNode.data){
				targetNode.data = {};
			}
			targetNode.data[fieldInfo.name] = value;
		}else{
			// Goes direct into targetNode:
			targetNode[fieldInfo.name] = value;
		}
		
		console.log('Warning: this setState call invalidates the targetNode of this.state.linkSelectionFor, breaking >1 field change', targetNode);
		// -> Needs to tell the canvas to redraw but without ruining the state info.
		// -> The detach happens because linkSelectionFor refs the json structure in the *parent canvas's data array* 
		//    and not the one that gets regenerated in the child canvas editor.
		this.setState({});
	}
	
	niceName(label){
		label = label.replace(/([^A-Z])([A-Z])/g, '$1 $2');
		return label[0].toUpperCase() + label.substring(1);
	}
	
	getLinkLabel(label, targetNode, fieldInfo){
		
		return [label, <i className='fa fa-link value-link' onClick={e => {
			// This blocks the input field click that happens as we're surrounded by a <label>
			e.preventDefault();
			
			this.setState({
				linkSelectionFor: {
					targetNode,
					fieldInfo
				}
			});
			
		}}/>];
		
	}
	
	getContentDropdown(typeName){
		if(!__contentCacheByType[typeName]){
			__contentCacheByType[typeName] = [];
			
			webRequest(typeName.toLowerCase() + '/list').then(result => {
				__contentCacheByType[typeName] = result.json.results;
				this.setState({});
			});
		}
		
		return __contentCacheByType[typeName].map(item => {
			var name = (item.name || item.title || item.firstName || 'Untitled') + ' (#' + item.id + ')';
			
			return (
				<option value={item.id}>{name}</option>
			);
		});
	}
	
	getContentTypeDropdown(){
		if(!__contentTypeCache){
			__contentTypeCache = [];
			
			getContentTypes().then(set => {
				__contentTypeCache = set;
				this.setState({});
			});
		}
		
		return __contentTypeCache.map(item => {
			var name = item.name;
			
			return (
				<option value={item.name}>{name}</option>
			);
		});
	}
	
	renderNode(contentNode){
		if(!contentNode){
			return null;
		}
		
		if(Array.isArray(contentNode)){
			return contentNode.map((e,i)=>this.renderNode(e,i));
		}
		
		var Module = contentNode.module || "div";
		
		var kidsSupported = false;
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
			
			var dataFields = mapTokens(contentNode.data, this, Canvas);
			kidsSupported = Module.propTypes && Module.propTypes.children;
			var customEdit = Module.propTypes && Module.propTypes.editButton;
			var displayName = ' ' + this.displayModuleName(contentNode.moduleName);
			result = (
				<Module {...dataFields} editButton={customEdit && !this.props.minimal ? () => {
					// Custom edit button
					return <div className="btn btn-secondary edit-button" onClick={() => {
						this.setState({optionsVisibleFor: contentNode});
						}}><i className="fa fa-cog"/>
						{' ' + this.displayModuleName(contentNode.moduleName)}
					</div>
				} : undefined}>
					{kidsSupported && !this.props.minimal && !customEdit && (
						<div className="edit-button" style={{flex: '0 0 100%'}}>
							<div className="btn btn-secondary" style={{marginBottom: '5px'}} onClick={() => {
								this.setState({optionsVisibleFor: contentNode});
							}}><i className="fa fa-cog"/>
							{displayName}
							</div>
						</div>
					)}
					{contentNode.useCanvasRender ? this.renderNode(contentNode.content) : null}
					{kidsSupported && !this.props.minimal && (
						<div style={{marginTop: '10px'}}>
							<div className="btn btn-secondary" onClick={() => {this.setState({selectOpenFor: contentNode})}}>
								<i className="fa fa-plus" />
								{' to ' + displayName}
							</div>
						</div>
					)}
				</Module>
			);
			
		}
		
		if(kidsSupported || this.props.minimal){
			return result;
		}
		
		// Does it have a custom edit button placement?
		if(customEdit){
			return result;
		}
		
		// Display the config button before the element:
		return [
			<div className="edit-button btn btn-secondary" style={{marginRight: '5px', marginBottom: '5px'}} onClick={() => {
				this.setState({optionsVisibleFor: contentNode});
			}}><i className="fa fa-cog"/>
			{' ' + this.displayModuleName(contentNode.moduleName)}
			</div>,
			result
		];
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
						ref.onGetValue = (val, field) => {
							if(field != this.ref){
								return;
							}
							
							return JSON.stringify(this.buildJson());
						}
					}
				}}/>
				<div style={{border: '1px solid lightgrey', borderRadius: '4px', padding: '0.375rem 0.75rem'}}>
					{this.renderNode(this.state.content,0)}
					{!this.props.minimal && (
						<div style={{clear: 'both', marginTop: '10px'}}>
							<div className="btn btn-secondary" onClick={() => {this.setState({selectOpenFor: true})}}>
								<i className="fa fa-plus" />
							</div>
						</div>
					)}
				</div>
				{!this.state.linkSelectionFor && this.renderModuleSelection()}
				{!this.state.linkSelectionFor && this.renderOptionsModal()}
				{this.renderLinkSelectionModal()}
			</div>
		);
	}
}