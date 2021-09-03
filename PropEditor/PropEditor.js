import Input from 'UI/Input';
import Loop from 'UI/Loop';
import Alert from 'UI/Alert';
import getContentTypes from 'UI/Functions/GetContentTypes';
import ArrayEditor from 'UI/RichEditor/PropEditor/ArrayEditor';
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
			No other options available
		</div>;
	}
	
	specialField(fieldName){
		return fieldName == 'children' || fieldName == 'editButton'
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
				<option value={item.name.toLowerCase()}>{name}</option>
			);
		});
	}
	
	getThemeDropdown(){
		if(!__themeCache){
			__themeCache = [];
			
			webRequest("configuration/list", {where:{key: 'Theme'}}).then(set => {
				var res = set.json.results;
				var themes = [];
				res.forEach(result => {
					try{
						result.configJson = JSON.parse(result.configJson);
						result.key = result.configJson.key;
						themes.push(result);
					}catch(e){
						console.log("Invalid theme JSON: ", result, e);
					}
				});
				__themeCache = themes;
				
				
				
				this.setState({});
			});
		}
		
		return __themeCache.map(item => {
			var name = item.name;
			
			return (
				<option value={item.key.toLowerCase()}>{name}</option>
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
			/*}else if(propType.type == 'array'){
				inputType = ArrayEditor;*/
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
				<Input label={this.niceName(label, propType.label)} type={inputType} defaultValue={val} placeholder={placeholder}
					help={propType.help} helpPosition={propType.helpPosition}
					fieldName={fieldName} disabledBy={propType.disabledBy} enabledBy={propType.enabledBy} onChange={e => {
					var value = e.target.value;

					if (inputType == 'checkbox') {
						value = !!e.target.checked;
					}

					this.updateField(targetNode, fieldInfo, value);
				}} onKeyUp={e => {
					var value = e.target.value;

					if (inputType == 'checkbox') {
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
	
	renderTheme(contentNode){
		var val = contentNode.props ? contentNode.props['data-theme'] : null;
		
		return <div>
			<Alert type='info'>
				Visual theme editing - Coming soon
			</Alert>
			
			<Input label={"Theme"} type={"select"} defaultValue={val}
				fieldName={'data-theme'} onChange={e => {
				var value = e.target.value;

				this.updateField(contentNode, {name: 'data-theme', value}, value);
			}}>{this.getThemeDropdown()}</Input>
			
		</div>;
	}
	
    render(){
		var content = this.props.optionsVisibleFor;
		
		if(!content){
			return;
		}
		
		var {mode} = this.state;
		
		return <div className="prop-editor">
			<div className="toolbar">
				<button className={"btn " + (mode == 'options' ? 'btn-primary' : 'btn-outline-primary')} onClick={()=>this.setState({mode: 'options'})}>Options</button>
				<button className={"btn " + (mode == 'theme' ? 'btn-primary' : 'btn-outline-primary')} onClick={()=>this.setState({mode: 'theme'})}>Theme</button>
			</div>
			<div className="p-3">
				{mode == 'options' ? this.renderOptions(content) : this.renderTheme(content)}
			</div>
		</div>
	}
}