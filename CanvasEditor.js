import omit from 'UI/Functions/Omit';
import Text from 'UI/Text';
import Modal from 'UI/Modal';
import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import Input from 'UI/Input';
import {expand, mapTokens} from 'UI/Functions/CanvasExpand';
import webRequest from 'UI/Functions/WebRequest';
import getContentTypes from 'UI/Functions/GetContentTypes';
import Collapsible from 'UI/Collapsible';

// Connect the input "ontypecanvas" render event:

var inputTypes = global.inputTypes = global.inputTypes || {};

inputTypes.ontypecanvas = function(props, _this){
	
	return <CanvasEditor 
		id={props.id || _this.fieldId}
		className={props.className || "form-control"}
		{...omit(props, ['id', 'className', 'type', 'inline'])}
	/>;
	
};

inputTypes.ontyperenderer = function(props, _this){
	
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
		this.clearContextMenu = this.clearContextMenu.bind(this);
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
				icon: 'paper-plane',
				type: 'contextToken',
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
			var module = global.getModule(modName).default;
			
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
			if(content === null){
				content = [];
			}else{
				content = [content];
			}
			changed = true;
		}
		
		var component = {
			module: moduleInfo.moduleClass,
			moduleName: moduleInfo.publicName,
			data: {},
			expanded: true
		};
		
		content.push(component);
		
		var {propTypes} = component.module;
		
		if(propTypes){
			if(propTypes.children && propTypes.children.default){
				component.content = expand(propTypes.children.default);
				component.useCanvasRender = true;
			}
			
			// Apply defaults:
			for(var key in propTypes){
				var prop = propTypes[key];
				if(prop.default){
					component.data[key] = prop.default;
				}
			}
		}
		
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
	
	renderOptions(contentNode, module){
		if(!contentNode){
			return;
		}
		
		var Module = module || contentNode.module || "div";
		var dataValues = {...contentNode.data};
		
		var props = Module.propTypes;
		var defaultProps = Module.defaultProps || {};
		
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
				dataFields[fieldName] = { propType, defaultValue: defaultProps[fieldName], value: dataValues[fieldName]};
				atLeastOneDataField = true;
			}
		}
		
		for(var fieldName in dataValues){
			if(this.specialField(fieldName) || dataFields[fieldName]){
				continue;
			}
			
			// This data field is in the JSON, but not a prop. The module is essentially missing its public interface.
			// We'll still display it though so it can be edited:
			dataFields[fieldName] = { propType: { type: 'string' }, defaultValue: defaultProps[fieldName], value: dataValues[fieldName]};
			atLeastOneDataField = true;
		}
		
		if(atLeastOneDataField){
			return (<div>
				{this.renderOptionSet(dataFields, contentNode)}
			</div>);
		}
		
		return <div>
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
				inputContent = propType.type.map(entry => {
					var value = entry;
					var name = entry;

					if (entry && typeof entry == "object") {
						value = entry.value;
						name = entry.name;
					}

					return (
						<option value={value}>{name}</option>
					);
				})
				inputContent.unshift(<option>Pick a value</option>);
			} else if (propType.type == 'color' || propType.type == 'colour') {
				inputType = 'color';
			} else if (propType.type == 'checkbox' || propType.type == 'bool' || propType.type == 'boolean') {
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
			}else if(propType.type == 'string'){
				inputType = 'text';
			}else{
				inputType = propType.type;
			}
			
			// The value might be e.g. a url ref.
			var val = fieldInfo.value;
			
			if(val && val.type){
				val = JSON.stringify(val);
			}

			var placeholder = propType.placeholder || (val == "" ? null : fieldInfo.defaultValue);

			// ensure default colour / boolean is set if we don't have a value
			switch (inputType) {
				case 'color':
				case 'checkbox':
				case 'radio':

					if (val == undefined && fieldInfo.defaultValue !== undefined && fieldInfo.defaultValue !== null) {
						val = fieldInfo.defaultValue;
					}

					break;
			}

			options.push(
				<Input label={this.getLinkLabel(this.niceName(label, propType.label), targetNode, fieldInfo)} type={inputType} defaultValue={val} placeholder={placeholder}
					help={propType.help} helpPosition={propType.helpPosition}
					fieldName={fieldName} disabledBy={propType.disabledBy} enabledBy={propType.enabledBy} onChange={e => {
					var value = e.target.value;

					if (inputType == 'renderer') {
						value = e.json;
					} else if (inputType == 'checkbox') {
						value = !!e.target.checked;
					}

					this.updateField(targetNode, fieldInfo, value);
				}} onKeyUp={e => {
					var value = e.target.value;

					if (inputType == 'renderer') {
						value = e.json;
					} else if (inputType == 'checkbox') {
						value = !!e.target.checked;
					}

					this.updateField(targetNode, fieldInfo, value);
					}} onKeyDown={e => {
						if (e.ctrlKey && e.key === 'Delete') {
							// reset field to default value
							e.target.value = fieldInfo.defaultValue;
						}
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
	
	niceName(label, override) {

		if (override && override.length) {
			return override;
		}

		label = label.replace(/([^A-Z])([A-Z])/g, '$1 $2');
		return label[0].toUpperCase() + label.substring(1);
	}
	
	getLinkLabel(label, targetNode, fieldInfo) {
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
	
	renderNode(contentNode, index){
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
						contentNode.data.text = e.target.innerHTML;
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
					dangerouslySetInnerHTML={{__html: contentNode.data.text}}
				/>
			);
		}else{
			
			var dataFields = mapTokens(contentNode.data, this, Canvas);
			
			result = (
				<Module ref={r => {
					if(r == null){
						return;
					}
					
					setTimeout(() => {
						if(r.base == null || r.base.setAttribute == null){
							return;
						}
						
						r.base.setAttribute('draggable', true);
						r.base.ondragstart = e => {
							var json = JSON.stringify(this.buildJsonNode(contentNode, index, false));
							e.dataTransfer.setData("application/json", json);
						};
						r.base.onjsondrop = (target, modules) => {
							
							if(!Array.isArray(contentNode.content)){
								if(contentNode.content === null){
									contentNode.content = [];
								}else{
									contentNode.content = [contentNode.content];
								}
								contentNode.useCanvasRender = true;
							}
							
							contentNode.content.push(expand(modules));
							
							this.setState({content: this.state.content});
							this.updated();
						};
						r.base.contentNode = contentNode;
						r.base.setAttribute('admin-module', true);
					}, 1000);
					
				}} {...dataFields}>
					{contentNode.useCanvasRender ? this.renderNode(contentNode.content) : null}
				</Module>
			);
			
		}
		
		return result;
	}

	renderLayoutNode(contentNode, index) {
		if (!contentNode) {
			return null;
		}

		if (Array.isArray(contentNode)) {
			return contentNode.map((e, i) => this.renderLayoutNode(e, i));
		}

		var result = null;
		var buttons = [];

		var Module = contentNode.module || "div";
		var displayName = this.displayModuleName(contentNode.moduleName);
		var subtitle;
		var kidsSupported = Module.propTypes && Module.propTypes.children;

		if (kidsSupported) {
			buttons.push({
				onClick: () => this.setState({ selectOpenFor: contentNode, rightClick: null }),
				icon: 'far fa-plus',
				text: 'Add component to ' + displayName
			});
		}

		buttons.push({
			onClick: () => this.setState({ optionsVisibleFor: contentNode, rightClick: null }),
			icon: 'far fa-cog',
			text: 'Edit ' + displayName
		});

		buttons.push({
			onClick: () => {
				this.setState({ rightClick: null });
				var parent = this.findParent(contentNode);
				this.removeFrom(contentNode, parent);
				if (parent == this.state) {
					this.setState({ content: parent.content });
				}
				this.updated();
			},
			icon: 'far fa-trash',
			text: 'Delete ' + displayName
		});

		if (contentNode.module == Text) {
			result = (
				<Collapsible title={displayName} buttons={buttons} open>
					<div
						contenteditable
						onInput={e => {
							contentNode.content = e.target.innerHTML;
							this.updated();
						}}
						onKeyDown={e => {

							if (!e.ctrlKey) {
								return;
							}

							switch (e.keyCode) {
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
						className="canvas-editor-module canvas-editor-text"
						dangerouslySetInnerHTML={{ __html: contentNode.content }}
					/>
				</Collapsible>
			);
		} else {

			if (displayName.toLowerCase() == "if") {
				var clauses = [];

				for (const [key, value] of Object.entries(contentNode.data)) {
					clauses.push(key + " = " + value);
				}

				if (clauses.length) {
					subtitle = "(" + clauses.join(", ").trim() + ")";
				}

			}

			result = (
				<Collapsible title={displayName} subtitle={subtitle} buttons={buttons} open>
					{contentNode.useCanvasRender ? this.renderLayoutNode(contentNode.content) : null}
				</Collapsible>
			);
		}

		return result;

		var Module = contentNode.module || "div";
		var result = null;

		if (Module == Text) {

			var divRef = null;

			result = (
				<div
					contenteditable
					onInput={e => {
						contentNode.content = e.target.innerHTML;
						this.updated();
					}}
					onKeyDown={e => {

						if (!e.ctrlKey) {
							return;
						}

						switch (e.keyCode) {
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
					dangerouslySetInnerHTML={{ __html: contentNode.content }}
				/>
			);
		} else {

			var dataFields = mapTokens(contentNode.data, this, Canvas);

			result = (
				<Module ref={r => {
					if (r == null) {
						return;
					}

					setTimeout(() => {
						if (r.base == null || r.base.setAttribute == null) {
							return;
						}

						r.base.setAttribute('draggable', true);
						r.base.ondragstart = e => {
							var json = JSON.stringify(this.buildJsonNode(contentNode, index, false));
							e.dataTransfer.setData("application/json", json);
						};
						r.base.onjsondrop = (target, modules) => {

							if (!Array.isArray(contentNode.content)) {
								if (contentNode.content === null) {
									contentNode.content = [];
								} else {
									contentNode.content = [contentNode.content];
								}
								contentNode.useCanvasRender = true;
							}

							contentNode.content.push(expand(modules));

							this.setState({ content: this.state.content });
							this.updated();
						};
						r.base.contentNode = contentNode;
						r.base.setAttribute('admin-module', true);
					}, 1000);

				}} {...dataFields}>
					{contentNode.useCanvasRender ? this.renderLayoutNode(contentNode.content) : null}
				</Module>
			);

		}

		return result;
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
		var {rightClick} = this.state;
		
		var buttons = [];
		
		var {contentNode} = rightClick;
		
		if(contentNode){
			var Module = contentNode.module || "div";
			var displayName = this.displayModuleName(contentNode.moduleName);
			var kidsSupported = Module.propTypes && Module.propTypes.children;
			
			if(kidsSupported){
				buttons.push({
					onClick: () => this.setState({selectOpenFor: contentNode, rightClick: null}),
					icon: 'plus',
					text: ' to ' + displayName
				});
			}
			
			buttons.push({
				onClick: () => this.setState({optionsVisibleFor: contentNode, rightClick: null}),
				icon: 'cog',
				text: 'Edit ' + displayName
			});
			
			buttons.push({
				onClick: () => {
					this.setState({rightClick: null});
					var parent = this.findParent(contentNode);
					this.removeFrom(contentNode, parent);
					if(parent == this.state){
						this.setState({content: parent.content});
					}
					this.updated();
				},
				icon: 'minus',
				text: 'Delete ' + displayName
			});
		}
		
		buttons.push({
			onClick: () => this.setState({jsonEdit: JSON.stringify(this.buildJson(), null, '\t')}),
			icon: 'code',
			text: 'Edit JSON source'
		});
		
		return <div className="context-menu" style={{
				left: rightClick.x + 'px',
				top: rightClick.y + 'px'
			}}>
			{buttons.map(cfg => {
				
				return (
					<div className="context-btn" onClick={cfg.onClick}>
						<i className={"fa fa-fw fa-" + cfg.icon} />
						{cfg.text}
					</div>
				);
				
			})}
		</div>;
	}
	
	clearContextMenu(e){
		if(this.state.rightClick){
			this.setState({rightClick: null});
		}
	}
	
	componentDidMount(){
		document.addEventListener('click', this.clearContextMenu);
	}
	
	componentWillUnmount(){
		document.removeEventListener('click', this.clearContextMenu);
	}
	
	findComponentRoot(e){
		if(e == null || !e.hasAttribute){
			return null;
		}
		if(e.hasAttribute('admin-module')){
			return e;
		}
		return this.findComponentRoot(e.parentNode);
	}
	
	render() {
		var btnClass = "btn btn-sm btn-primary";
		var isPreviewMode = !this.state.layoutView && !this.state.jsonEdit;
		var previewClass = btnClass + (isPreviewMode ? " active" : "");
		var layoutClass = btnClass + (this.state.layoutView ? " active" : "");
		var sourceClass = btnClass + (this.state.jsonEdit ? " active" : "");
		
		var popups = isPreviewMode || this.state.layoutView;
		
		{/* TODO: grab the value from the JSON source textarea and set it as the loaded canvas */}
		return (
			<>
				{/* preview / layout / source switch*/}
				<div class="btn-group btn-group-toggle canvas-editor-view" data-toggle="buttons">
					<label class={previewClass}>
						<input type="radio" name="canvasViewOptions" id="previewMode" autocomplete="off"
							checked={isPreviewMode} onChange={() => {
								this.setState({
									layoutView: false,
									jsonEdit: false
								});
							}} />
						<i className="far fa-presentation"></i>
						Preview
					</label>
					<label class={layoutClass}>
						<input type="radio" name="canvasViewOptions" id="layoutMode" autocomplete="off"
							checked={this.state.layoutView} onChange={() => {
								this.setState({
									layoutView: true,
									jsonEdit: false
								});
							}} />
						<i className="far fa-sitemap"></i>
						Layout
					</label>
					<label class={sourceClass}>
						<input type="radio" name="canvasViewOptions" id="sourceMode" autocomplete="off"
							checked={this.state.jsonEdit} onChange={() => {
								this.setState({
									layoutView: false,
									jsonEdit: JSON.stringify(this.buildJson(), null, '\t')
								});
							}} />
						<i className="far fa-code"></i>
						Source
					</label>
				</div>

				<input type="hidden" name={this.props.name} ref={ref => {
					this.ref = ref;
					if (ref) {
						ref.onGetValue = (val, field) => {
							if (field != this.ref) {
								return;
							}

							return JSON.stringify(this.buildJson());
						}
					}
				}} />

				{/* preview mode */}
				{isPreviewMode &&
					<div
						className="canvas-editor"
						onDrop={e => {
							e.preventDefault();
							var data = e.dataTransfer.getData("application/json");

							var target = this.findComponentRoot(e.target);

							if (target && target.onjsondrop && window.confirm("Move content?")) {
								target.onjsondrop(e, JSON.parse(data));
							}

						}}
						onDragEnd={
							e => {
								if (this.dragTarget) {
									this.dragTarget.style.backgroundColor = '';
								}
							}
						}
						onDragOver={e => {
							if (this.dragTarget) {
								this.dragTarget.style.backgroundColor = '';
							}

							this.dragTarget = e.target;
							this.dragTarget.style.backgroundColor = 'lightblue';

							e.preventDefault();
						}}
						onContextMenu={e => {
							e.preventDefault();
							return false;
						}}
						onMouseUp={e => {
							// Check for right click
							// TODO: Also check for a long press.
							if (e.button != 2) {
								return;
							}

							// Find nearest admin-module:
							// TODO: Stuff that perfectly fits inside its parent, such as anything inside UI/Align, makes the UI/Align itself unreachable.
							// Maybe find *all* nearest admin-modules and list them?
							var targetModuleElement = this.findComponentRoot(e.target);

							if (!targetModuleElement) {
								return;
							}

							e.preventDefault();

							this.setState({
								rightClick: {
									contentNode: targetModuleElement.contentNode,
									x: e.clientX,
									y: e.clientY
								}
							});
						}}
					>
						{this.props.onPageUrl && (
							<div style={{ marginBottom: '10px' }}>
								<a href={this.props.onPageUrl + '#edit=' + this.props.contentType + '&id=' + this.props.contentId + '&field=' + this.props.name}>
									<div className="btn btn-primary">
										Edit on page
								</div>
								</a>
							</div>
					)}
						<div className="canvas-editor-bordered">
							{this.renderNode(this.state.content, 0)}
							{!this.props.minimal && (
								<button type="button" className="btn btn-secondary btn-add-component" onClick={() => { this.setState({ selectOpenFor: true }) }}>
									<i className="far fa-plus" />
									Add component
								</button>
							)}
						</div>
					</div>
				}

				{/* layout mode */}
				{this.state.layoutView &&
					<div className="canvas-editor">
						{this.renderLayoutNode(this.state.content, 0)}
						{!this.props.minimal && (
							<button type="button" className="btn btn-sm btn-primary btn-add-component" onClick={() => { this.setState({ selectOpenFor: true }) }}>
								<i className="far fa-plus" />
								Add component
							</button>
						)}
					</div>
				}
				
				{popups && (
					<div className="canvas-editor-popups" 
						onContextMenu={e => {
							e.preventDefault();
							return false;
						}}>
						{!this.state.linkSelectionFor && this.renderModuleSelection()}
						{!this.state.linkSelectionFor && this.renderOptionsModal()}
						{this.renderLinkSelectionModal()}
						{this.state.rightClick && this.renderContextMenu()}
					</div>
				)}
				
				{/* source mode */}
				{this.state.jsonEdit &&
					<div className="canvas-editor">
						<Input type="textarea" className="form-control json-preview" name={this.props.name} defaultValue={this.state.jsonEdit} />
					</div>
				}

			</>
		);
	}
}