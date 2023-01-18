import Input from 'UI/Input';
import Loop from 'UI/Loop';
import Alert from 'UI/Alert';
import Graph from 'UI/Functions/GraphRuntime/Graph';
import getContentTypes from 'UI/Functions/GetContentTypes';
import ArrayBuilder from 'Admin/CanvasEditor/PropEditor/ArrayBuilder';
import ArrayEditor from 'Admin/CanvasEditor/PropEditor/ArrayEditor';
import webRequest from 'UI/Functions/WebRequest';

var TEXT = '#text';

var __contentTypeCache = null;
var __themeCache = null;
var __contentCacheByType = {};

export default class PropEditor extends React.Component {

    constructor(props){
        super(props);
        this.state = {
			mode: 'options'
        };
		this.inputKey = 1;
    }

    niceName(label, override) {

		if (override && override.length) {
			return override;
		}

		label = label.replace(/([^A-Z])([A-Z])/g, '$1 $2');
		return label[0].toUpperCase() + label.substring(1);
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

    updateField(targetNode, fieldInfo, value){
		fieldInfo.value = value;
		
		if(!targetNode.props) {
			targetNode.props = {};
		}
		targetNode.props[fieldInfo.name] = value;
		
		// Tell the editor:
		this.props.onChange(targetNode, fieldInfo, value);
	}

    renderOptions(contentNode){
		if(!contentNode){
			return;
		}
		
		if(contentNode.graph){
			return `Graph nodes can currently only be edited in the graph editor. In the (near) future, static values will be identified and listed here too.`;
		}
		
		var dataValues = {...contentNode.props};

		var props = contentNode.type.propTypes;
		var defaultProps = contentNode.type.defaultProps || {};
		
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
				
				if(propType.type == "jsx") {
					continue;
				}
				
				var value = null;
				
				// Got a value?
				if(dataValues[fieldName]) {
					value = dataValues[fieldName];
				}
				
				var val = { propType, defaultValue: defaultProps[fieldName], value};
				dataFields[fieldName] = val;
				atLeastOneDataField = true;
			}
		}
		
		if(atLeastOneDataField){
			return (<div>
				{this.renderOptionSet(dataFields, contentNode)}
			</div>);
		}
		
		return <div>
			{`No other options available`}
		</div>;
	}
	
	componentWillReceiveProps(props){
		if(!this.props || props.optionsVisibleFor != this.props.optionsVisibleFor){
			this.inputKey++;
		}
	}
	
	specialField(fieldName){
		return fieldName == 'children' || fieldName == 'editButton'
	}
	
	getContentDropdown(typeName, field, filterFunc){
		if(!__contentCacheByType[typeName]){
			__contentCacheByType[typeName] = [];
			
			webRequest(typeName.toLowerCase() + '/list').then(result => {
				__contentCacheByType[typeName] = result.json.results;
				this.setState({});
			});
		}
		
		var set = __contentCacheByType[typeName];
		
		if(filterFunc){
			set = set.filter(filterFunc);
		}
		
		return set.map(item => {
			var name = (item.name || item.title || item.firstName || 'Untitled') + ' (#' + item.id + ')';
			
			return (
				<option value={item[field || 'id']}>{name}</option>
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
				<option value={item.name.toLowerCase()}>{name}</option>
			);
		});
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
			
			{/*
			if(fieldInfo.value && fieldInfo.value.type && fieldInfo.value.type != 'module'){
				// It's a linked field.
				// Show its options instead.
				var linkTypes = this.collectLinkTypes();
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
					
					if(linkPT.type == 'jsx'){
						continue;
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
			}*/}
			
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
			}else if(propType.type == 'id' || propType.type == 'contentField'){
				inputType = 'select';
				inputContent = this.getContentDropdown(propType.content || propType.contentType, propType.field || 'id', propType.filterFunction);
				inputContent.unshift(<option>Pick some content</option>);
			}else if(propType.type == 'contentType'){
				inputType = 'select';
				inputContent = this.getContentTypeDropdown();
				inputContent.unshift(<option>Pick a content type</option>);
			}else if(propType.type == 'array'){
				if (propType.fields) {
					inputType = ArrayBuilder;
				} else {
					inputType = ArrayEditor;
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
				case 'bool':
				case 'boolean':
				case 'radio':

					if (val == undefined && fieldInfo.defaultValue !== undefined && fieldInfo.defaultValue !== null) {
						val = fieldInfo.defaultValue;
					}

					break;
			}

			options.push(
				<Input key={this.inputKey + '_' + fieldName} label={this.niceName(label, propType.label)} type={inputType} defaultValue={val} placeholder={placeholder}
					help={propType.help} helpPosition={propType.helpPosition}
					customMeta={propType}
					fieldName={fieldName} disabledBy={propType.disabledBy} enabledBy={propType.enabledBy} onChange={e => {
					var value = e.target.value;

					if (inputType == 'checkbox' || inputType == "bool" || inputType == "boolean") {
						value = !!e.target.checked;
					} else if (inputType?.name == 'ArrayBuilder') {
						value = e.target.groupData;
					}

					this.updateField(targetNode, fieldInfo, value);
				}} onKeyUp={e => {
					var value = e.target.value;

					if (inputType == 'checkbox' || inputType == "bool" || inputType == "boolean") {
						value = !!e.target.checked;
					} else if (inputType?.name == 'ArrayBuilder') {
						value = e.target.groupData;
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
	
    render(){
		var content = this.props.optionsVisibleFor;
		
		if(!content){
			return;
		}
		
		var {mode} = this.state;
		var typeName = content.typeName || content.type;
						
		return <div className="prop-editor">
			<div className="toolbar">
				<button className={"btn btn-outline-primary"} onClick={e=>{
					
					e.preventDefault();
				
					// If it is not already a graph node, convert it into one.
					if(!content.graph){
						// Conversion time!
						
						if(typeName == 'richtext'){
							// Can't do this for static RTE content.
							console.warn("Can't convert richtext nodes to a graph. They are fundamentally static HTML-style content nodes.");
							return;
						}
						
						var convertedGraph = {
							c: [
								{r:true, t: "Component", d: {
									componentType: typeName,
									...content.props
								}}
							]
						};
						
						delete content.type;
						delete content.props;
						content.graph = new Graph(convertedGraph);
						this.props.onChange && this.props.onChange();
					}
					
					// Open graph view for the selected component.
					this.props.setGraphState(true);
					
				}} disabled={typeName == 'richtext'}>Edit graph</button>
				<button className={"btn btn-outline-primary"} onClick={e=>{
					
					e.preventDefault();
					
					// Open theme view for the selected component.
					this.props.setThemeState(true);
					
				}}>Edit Theme</button>
			</div>
			<div className="py-3">
				{this.renderOptions(content)}
			</div>
		</div>
	}
}