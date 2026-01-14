import Input from 'UI/Input';
import Loop from 'UI/Loop';
import Alert from 'UI/Alert';
import Graph from 'Admin/CanvasEditor/GraphEditor/Graph';
import getContentTypes from 'UI/Functions/GetContentTypes';
// import ArrayBuilder from 'Admin/CanvasEditor/PropEditor/ArrayBuilder';
// import ArrayEditor from 'Admin/CanvasEditor/PropEditor/ArrayEditor';
import {
	isJsx, getConstantUnion, getContentPropType,
	isNumericPropType, isBooleanPropType, isRefPropType, getTypeName
} from 'Admin/Functions/GetPropTypes';

import componentGroupApi from "Api/ComponentGroup";
import useApi from "UI/Functions/UseApi";
import Loading from "UI/Loading";
import Link from "UI/Link";
import Button from "UI/Button";
import {useState} from "react";
import Icon from "UI/Icon";

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
		
		if(targetNode._doFieldUpdate){
			targetNode._doFieldUpdate(fieldInfo, value);
		}else{
			if(!targetNode.props) {
				targetNode.props = {};
			}
			targetNode.props[fieldInfo.name] = value;
		}
		
		// Tell the editor:
		this.props.onChange(targetNode, fieldInfo, value);
	}

    renderOptions(contentNode){
		if(!contentNode){
			return;
		}
		
		var dataFields = {};
		var atLeastOneDataField = false;
		var isGraph = contentNode.graph;
		
		if(isGraph){
			
			// Graph nodes can specify which fields users would like to expose via having named constants.
			// So, we just need to list the named constant nodes here.
			var { structure } = contentNode.graph;
			
			if(structure && structure.c && structure.c.length){
				var nodes = structure.c;
				for(var i=0;i<nodes.length;i++){
					var node = nodes[i];
					
					if(node && node.t == 'Constant' && node.d && node.d.fieldName){
						// It's a named constant. It can be displayed here.
						atLeastOneDataField = true;
						dataFields[node.d.fieldName] = {
							propType: 'string',
							defaultValue: undefined,
							value: node.d.output,
							contentNode,
							node
						};
					}
				}
			}
			
			contentNode = {
				_doFieldUpdate: (fieldInfo, value) => {
					var {contentGraph, node} = fieldInfo;
					node.d.output = value;
				}
			};
			
		}else{
			var dataValues = {...contentNode.props};
			
			var codeModuleMeta = contentNode.typePropTypes;
			var pt = codeModuleMeta?.propTypes;
			
			if (pt){
				for(var fieldName in pt){
					if(this.specialField(fieldName)){
						continue;
					}

					var propType = pt[fieldName];

					if (isJsx(propType)) {
						continue;
					}
					
					var value = null;
					
					// Got a value?
					if(dataValues[fieldName]) {
						value = dataValues[fieldName];
					}
					
					var val = { propType, codeModuleMeta, defaultValue: undefined, value};
					dataFields[fieldName] = val;
					atLeastOneDataField = true;
				}
			}
		}
				
		if(atLeastOneDataField){
			return (<div>
				{this.renderOptionSet(dataFields, contentNode)}
			</div>);
		}
		
		return <div>
			{isGraph ? `No options currently available. Add named constant nodes to the graph to get options here.` : `No other options available`}
		</div>;
	}
	
	componentWillReceiveProps(props){
		if(!this.props || props.optionsVisibleFor != this.props.optionsVisibleFor){
			this.inputKey++;
		}
	}
	
	specialField(fieldName) {
		// Fields starting with an underscore are now 'private' by convention. Note that any React (isJSX) fields 
		// are omitted separately.
		return fieldName == 'children' || (fieldName?.length > 0 && fieldName[0] == '_')
	}
	
	getContentDropdown(typeName, field, filterFunc, filter){
		if(!__contentCacheByType[typeName]){
			__contentCacheByType[typeName] = [];

			var api = require('Api/' + typeName).default;

			if (api != null) {
				// It's an ApiEndpoints instance
				api.list(filter).then(response => {
					__contentCacheByType[typeName] = response.results;
					this.setState({});
				});
			}

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
			var codeModuleMeta = fieldInfo.codeModuleMeta;
			
			if(Object.keys(customPropEditors).includes(fieldName)) {
				options.push(
					<CustomComponentsInput 
						fieldName={fieldName} 
						niceName={this.niceName(fieldName)} 
						onChange={(value) => {
							this.updateField(targetNode, fieldInfo, value);
						}} 
						value={fieldInfo.value}
					/>
				)
				return;
			}
			
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

			var constantUnion = getConstantUnion(propType, codeModuleMeta);
			var contentTypeName = getContentPropType(propType, codeModuleMeta);
			
			if (fieldName.endsWith("Ref") || isRefPropType(propType)){
				inputType = 'file';
				label = label.substring(0, label.length-3);
			} else if (constantUnion){
				inputType = 'select';
				inputContent = constantUnion.map(entry => {
					return (
						<option value={entry}>{entry}</option>
					);
				})
				inputContent.unshift(<option disabled value="">{`Pick a value`}</option>);
			} else if (contentTypeName){
				inputType = 'select';
				inputContent = this.getContentDropdown(contentTypeName, 'id');
				inputContent.unshift(<option disabled value="">{`Pick some content`}</option>);
			}else if(isNumericPropType(propType)){
				inputType = 'number';
			} else if (isBooleanPropType(propType)) {
				inputType = 'checkbox';
			} else {
				var customType = getTypeName(propType)?.toLowerCase();

				if (customType && global.inputTypes[customType]) {
					inputType = customType;
				} else {
					inputType = 'text';
				}
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
					}

					this.updateField(targetNode, fieldInfo, value);
				}} onKeyUp={e => {
					var value = e.target.value;

					if (inputType == 'checkbox' || inputType == "bool" || inputType == "boolean") {
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
	
    render(){
		var content = this.props.optionsVisibleFor;
		
		if(!content){
			return;
		}
		
		var {mode} = this.state;
		var typeName = content.typeName || content.type;
						
		return <div className="prop-editor">
			<div className="toolbar">
				<button type="button" className={"btn btn-outline-primary"} onClick={e=>{
					
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
				<button type="button" className={"btn btn-outline-primary"} onClick={e=>{
					
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

const customPropEditors = {};

const setCustomPropEditor = (fieldName, component) => {
	customPropEditors[fieldName] = component;
}

/**
 * The containing component to render custom inputs in the prop editor.
 * @param {CustomComponentsInputProps} props
 * @component
 */
const CustomComponentsInput = (props) => {
	
	if (!customPropEditors[props.fieldName]) {
		return (
			<Alert variant={'warning'}>{`No custom handler for the field '${props.fieldName}'`}</Alert>
		)
	}
	
	const Renderer = customPropEditors[props.fieldName];
	
	return (
		<Renderer {...props} />
	)
}

export {
	setCustomPropEditor
}