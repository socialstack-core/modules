import Modal from 'UI/Modal';
import Input from 'UI/Input';

var TEXT = '#text';

export default class PropEditor extends React.Component {

    constructor(props){
        super(props);

        this.state = {

        };
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
		if(targetNode.typeName.indexOf('/') != -1){
			console.log("updateField#1", targetNode);

			// Targeting a contentNode. Must go into the roots.
			if(targetNode.propTypes.hasOwnProperty(fieldInfo.name)) {
				console.log("has " + fieldInfo.name);

				// what is the propType? If its jsx, it needs to live in the roots.
				if(targetNode.propTypes[fieldInfo.name] == "jsx") {
					console.log("is jsx");
					// This means, we need to place it in a jsx object. Let's see if there one.
					if(targetNode.roots.hasOwnProperty(fieldInfo.name) && targetNode.roots[fieldInfo.name].content) {
						console.log("has root");
						if(Array.isArray(targetNode.roots[fieldInfo.name].content)) {
							targetNode.roots[fieldInfo.name].content.forEach(con => {
								console.log("content in root",con);
								if(con.type == TEXT) {
									con.text = value;
								}
							})
						}
					} else {
						console.log("doesn't have a root.");
					}
				} else {
					console.log("is not jsx");
					// Let's set it straight to the prop value since its not jsx.
					targetNode.props[fieldInfo.name] = value;
				}
			} else {
				console.log("doesn't have " + fieldInfo.name);
			}
			
		}else{
			console.log("updateField#2", targetNode);
			// Goes direct into targetNode:
			targetNode[fieldInfo.name] = value;
		}
		
		// At this point, we need to update the selection snapshot, set it as the current selection, then normalise. :fingers-crossed"
		this.props.updateTargetNode && this.props.updateTargetNode(targetNode);
	}

    renderOptions(contentNode, module){
		if(!contentNode){
			return;
		}

		var Module = module || contentNode.type || "div";
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

    render(){
		var content = this.props.optionsVisibleFor;

		console.log("PropEditor content:", {...content});
		if(!content){
			return;
		}

		return <Modal
			className={"module-options-modal"}
			buttons={[
				{
					label: "Close",
					onClick: this.props.closeModal
				}
			]}
			title={this.displayModuleName(content.typeName)}
			onClose={this.props.closeModal}
			visible={true}
		>
			{
				this.renderOptions(content)
			}
		</Modal>

	}
}