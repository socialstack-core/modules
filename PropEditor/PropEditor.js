import Modal from 'UI/Modal';
import Input from 'UI/Input';
import Loop from 'UI/Loop';

var TEXT = '#text';
var __linkTypes = null;

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
		console.log("updateField");
		fieldInfo.value = value;
		console.log("targetNode",targetNode);
		console.log("fieldInfo", fieldInfo);
		console.log("value", value);
		if(targetNode.typeName.indexOf('/') != -1){
			// Targeting a contentNode. Must go into the roots.
			if(targetNode.type.propTypes.hasOwnProperty(fieldInfo.name)) {
				// what is the propType? If its jsx, it needs to live in the roots.
				if(targetNode.type.propTypes[fieldInfo.name] == "jsx") {
					// This means, we need to place it in a jsx object. Let's see if there one.
					if(targetNode.roots.hasOwnProperty(fieldInfo.name) && targetNode.roots[fieldInfo.name].content) {

						if(Array.isArray(targetNode.roots[fieldInfo.name].content)) {
							targetNode.roots[fieldInfo.name].content.forEach(con => {
								if(con.type == TEXT) {
									con.text = value;
								}
							})
						}
					}
				} else {
					console.log("not a root");

					// Let's set it straight to the prop value since its not jsx.
					if(!targetNode.props) {
						targetNode.props = {};
					}
					targetNode.props[fieldInfo.name] = value;
					console.log("targetNode after update");
					console.log(targetNode);
				}
			}
		}else{
			// Goes direct into targetNode:
			targetNode[fieldInfo.name] = value;
		}
		
		// At this point, we need to update the target node in state which will update the values in real time in the editor.
		this.props.updateTargetNode && this.props.updateTargetNode(targetNode);
	}

    renderOptions(contentNode, module){
		if(!contentNode){
			return;
		}

		var Module = module || contentNode || "div";
		console.log("Module", Module);
		var dataValues = {...contentNode.data};

		var props = Module.type.propTypes;
		var defaultProps = Module.type.defaultProps || {};
		
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

				var value = null;

				// What is our proptype type? if its jsx, we need to check the roots for the value.
				if(propType.type == "jsx") {
					// Let's get the jsx value in the roots.
					if(Module.roots && Module.roots[fieldName]) {
						var root = Module.roots[fieldName];

						if(Array.isArray(Module.roots[fieldName].content)) {
							Module.roots[fieldName].content.forEach(con => {
								if(con.type == TEXT) {
									value = con.text;
								}
							})
						}
					}

				} else {
					// Not a root, let's look in the props
					if(Module.props && Module.props[fieldName]) {
						value = Module.props[fieldName];
					}
				}

				var val = { propType, defaultValue: defaultProps[fieldName], value};
				dataFields[fieldName] = val;
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
				console.log("fieldInfo", fieldInfo);
				console.log("fieldInfo.value", fieldInfo.value);
				console.log("fieldInfo.value.type", fieldInfo.value.type);
				// It's a linked field.
				// Show its options instead.
				var linkTypes = this.collectLinkTypes();
				console.log(linkTypes);
				var typeInfo = this.collectLinkTypes().map[fieldInfo.value.type];
				console.log


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

	closeLinkModal(){
		this.setState({
			linkSelectionFor: null
		});
		//this.props.updateTargetNode && this.props.updateTargetNode(targetNode);
	}

	renderLinkSelectionModal(){
		var lsf = this.state.linkSelectionFor;
		if(!lsf){
			return;
		}

		console.log("lsf", lsf);
		
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

    render(){
		var content = this.props.optionsVisibleFor;
		console.log("PropEditor");
		console.log("content", content);
		if(!content){
			console.log("!content");
			return;
		}

		return <>
			<Modal
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
			{this.renderLinkSelectionModal()}
		</>
	}
}