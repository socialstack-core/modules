var id = 1;

import tinymce from 'UI/TinyMce';
import CanvasEditor from 'UI/CanvasEditor';
import getModule from 'UI/Functions/GetModule';
import omit from 'UI/Functions/Omit';


/**
 * Helps eliminate a significant amount of boilerplate around <input>, <textarea> and <select> elements.
 * Note that you can still use them directly if you want.
 * You can also use validate={[..]} and provide either a module name from the 
 * UI/Functions/Validation set of modules or a function which receives the value to validate and returns nothing or an error object.
 * See e.g. UI/Functions/Validation/Required/Required.js for the structure of a custom method.
 */
export default class Input extends React.Component {
	
	constructor(props){
		super(props);
		this.state={};
		this.editor = null;
		this.newId();
		this.onChange=this.onChange.bind(this);
		this.onSelectChange=this.onSelectChange.bind(this);
	}
	
	newId(){
		this.fieldId = 'form-field-' + (id++);
		this.helpFieldId = this.fieldId + "-help";
		this.element = null;
		if(this.editor != null){
			tinymce.remove(this.editor);
			this.editor = null;
		}
	}
	
	componentWillReceiveProps(props){
		this.newId();
	}
	
	componentWillUnmount(){
		if(this.editor != null){
			tinymce.remove(this.editor);
			this.editor = null;
		}
		this.element = null;
	}
	
	render() {
		if(this.props.inline){
			return this.renderInput();
		}
		
		return (
			<div className="form-group">
				{this.props.label && this.props.type != "submit" && (
					<label for={this.props.id || this.fieldId}>
						{this.props.label}
					</label>
				)}
				{this.props.help && (
					<small id={this.helpFieldId} className="form-text text-muted">
						{this.props.help}
					</small>
				)}
				{this.renderInput()}
				{this.state.validationFailure && (
					<div className="validation-error">
						{this.props.validationFailure ? this.props.validationFailure(this.state.validationFailure) : this.state.validationFailure.ui}
					</div>
				)}
			</div>
		);
	}
	
	onChange(e) {
		this.props.onChange && this.props.onChange(e);
		if(e.defaultPrevented){
			return;
		}
		
		// Validation check
		this.revalidate(e);
	}
	
	onSelectChange(e) {
		this.setState({selectValue: e.target.value});
		this.props.onChange && this.props.onChange(e);
		if(e.defaultPrevented){
			return;
		}
		
		// Validation check
		this.revalidate(e);
	}
	
	revalidate(e){
		
		var validations = this.props.validate;
		
		if(!validations){
			if(this.state.validationFailure){
				this.setState({validationFailure: null});
			}
			return;
		}
		
		if(!Array.isArray(validations)){
			// Make it one:
			validations = [validations];
		}
		
		var v = e.target.value;
		var vFail = null;
		
		for(var i=0;i<validations.length;i++){
			// If it's a string, include the module.
			// Otherwise it's assumed to be a function that we directly run.
			var valType = validations[i];
			
			if(!valType){
				continue;
			}
			
			switch(typeof valType){
				case "string":
					var mtd = getModule("UI/Functions/Validation/" + valType);
					vFail = mtd(v);
				break;
				case "function":
					// Run it:
					vFail = valType(v);
				break;
				default:
					console.log("Invalid validation type: ", validations, valType, i);
				break;
			}
			
			if(vFail){
				break;
			}
		}
		
		if(vFail || this.state.validationFailure){
			this.setState({validationFailure: vFail});
		}
	}
	
	mountEditor(e) {
		this.element = e;
		setTimeout(function(){
			
			tinymce.init({target:e, setup: editor => {
				
				this.editor = editor;
				editor.on('change', function () {
					tinymce.triggerSave();
				});
				
			}});
			
		}, 50);
	}
	
	renderInput() {
		
		const {type} = this.props;
		
		if(type == "select"){
			
			return (
				<select
					onChange={this.onSelectChange}
					value={typeof this.state.selectValue === 'undefined' ? this.props.defaultValue : this.state.selectValue}
					id={this.props.id || this.fieldId}
					className={this.props.className || "form-control"}
					{...omit(this.props, ['id', 'className', 'onChange', 'type', 'children', 'defaultValue', 'value', 'inline'])}
				>
					{this.props.children}
				</select>
			);
			
		}else if(type == "textarea"){
			
			return (
				<textarea 
					onChange={this.onChange} 
					id={this.props.id || this.fieldId} 
					className={this.props.className || "form-control"}
					{...omit(this.props, ['id', 'className', 'onChange', 'type', 'inline'])}
				/>
			);
		
		}else if(type == "visual"){
			
			return (
				<textarea 
					ref={e=>this.mountEditor(e)}
					onChange={this.onChange} 
					id={this.props.id || this.fieldId} 
					className={this.props.className || "form-control"}
					{...omit(this.props, ['id', 'className', 'onChange', 'type', 'inline'])}
				/>
			);
		
		}else if(type == "canvas"){
			
			return (
				<CanvasEditor 
					id={this.props.id || this.fieldId}
					className={this.props.className || "form-control"}
					{...omit(this.props, ['id', 'className', 'type', 'inline'])}
				/>
			);
			
		}else if(type == "submit" || type == "button"){
			
			return (
				<button  
					className={this.props.className || "btn btn-primary"}
					type={type} 
					{...omit(this.props, ['className', 'type', 'label', 'children', 'inline'])}
				>
					{this.props.label || this.props.children || "Submit"}
				</button>
			);
			
		}else{
			return (
				<input 
					id={this.props.id || this.fieldId} 
					className={this.props.className || "form-control"}
					aria-describedby={this.helpFieldId} 
					type={type} 
					onChange={this.onChange}
					{...omit(this.props, ['id', 'className', 'onChange', 'type', 'inline'])}
				/>
			);
		}
		
	}
	
}