var id = 1;

import getModule from 'UI/Functions/GetModule';
import Loop from 'UI/Loop';
import omit from 'UI/Functions/Omit';

var eventTarget = global.events.get('UI/Input');

/**
 * Helps eliminate a significant amount of boilerplate around <input>, <textarea> and <select> elements.
 * Note that you can still use them directly if you want.
 * You can also use validate={[..]} and provide either a module name from the 
 * UI/Functions/Validation set of modules or a function which receives the value to validate and returns nothing or an error object.
 * See e.g. UI/Functions/Validation/Required/Required.js for the structure of a custom method.
 */
export default class Input extends React.Component {

    constructor(props) {
        super(props);
        this.state = {pwVisible:false};
        this.newId();
        this.onInput = this.onInput.bind(this);
        this.onChange = this.onChange.bind(this);
        this.updateValidation = this.updateValidation.bind(this);
        this.onBlur = this.onBlur.bind(this);
        this.setRef = this.setRef.bind(this);
        this.onSelectChange = this.onSelectChange.bind(this);
    }

    componentWillReceiveProps(props) {
        this.newId();
    }

    newId() {
        this.fieldId = 'form-field-' + (id++);
        this.helpFieldId = this.fieldId + "-help";
    }

    renderField() {
        // possible to hide all labels globally with $ss_form_field_label_hidden;
        // this allows us to specify fields which should always display their label
        var labelClass = this.props.labelImportant ? "label-important" : "";

        return (
            <>
            {
                this.props.label && this.props.type !== "submit" && this.props.type !== "checkbox" && this.props.type !== "radio" && (
                    <label htmlFor={this.props.id || this.fieldId} className={labelClass}>
                        {this.props.label}
                    </label>
                )
            }
            {
                this.props.help && (
                    <small id={this.helpFieldId} className="form-text text-muted">
                        {this.props.help}
                    </small>
                )
            }
            { this.renderInput() }
            {
                this.state.validationFailure && (
                    <div className="validation-error">
                        {this.props.validationFailure ? this.props.validationFailure(this.state.validationFailure) : this.state.validationFailure.ui}
                    </div>
                )
            }
            </>
        )
    }

    render() {
        if (this.props.inline) {
            return this.renderInput();
        }

        if (this.props.noWrapper) {
            return this.renderField();
        }

        var groupClass = this.props.groupClassName ? "form-group " + this.props.groupClassName : "form-group";

        return (
            <div className={groupClass}>
                {this.renderField()}
            </div>
        );
    }

    onInput(e) {
        this.props.onInput && this.props.onInput(e);
        if (e.defaultPrevented) {
            return; 
        }

        // Validation check
        this.revalidate(e);
    }

    onChange(e) {
        this.props.onChange && this.props.onChange(e);
        if (e.defaultPrevented) {
            return;
        }

        // Validation check
        this.revalidate(e);
    }

    onBlur(e) {
        this.props.onBlur && this.props.onBlur(e);
        if (e.defaultPrevented) {
            return;
        }

        // Validation check
        this.revalidate(e);
    }

    onSelectChange(e) {
        this.setState({ selectValue: e.target.value });
        this.props.onChange && this.props.onChange(e);
        if (e.defaultPrevented) {
            return;
        }

        // Validation check
        this.revalidate(e);
    }
	
	validationError(){
        var validations = this.props.validate;

        if (!validations) {
            return false;
        }
	
        if (!Array.isArray(validations)) {
            // Make it one:
            validations = [validations];
        }
		
		if(!this.inputRef){
			return false;
		}
		
        var v = this.inputRef.value;
        var vFail = null;

        for (var i = 0; i < validations.length; i++) {
            // If it's a string, include the module.
            // Otherwise it's assumed to be a function that we directly run.
            var valType = validations[i];

            if (!valType) {
                continue;
            }

            switch (typeof valType) {
                case "string":
                    var mtd = getModule("UI/Functions/Validation/" + valType).default;
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
			
            if (vFail) {
                return vFail;
            }
        }
		
		return false;
	}
	
    revalidate(e) {
		this.updateValidation(e.target);
	}
	
	updateValidation(el){
		if(el != this.inputRef){
			return false;
		}
		var invalid = this.validationError();
        if (this.state.validationFailure != invalid) {
            this.setState({ validationFailure: invalid });
        }
		return !!invalid;
    }
	
	setRef(ref) {
		this.inputRef = ref;
		this.props.inputRef && this.props.inputRef(ref);
		
		if(ref){
			ref.onValidationCheck = this.updateValidation;
		}
	}
	
    renderInput() {

        const { type } = this.props;

        if (type instanceof Function) {
            return type(this);
        }

        if (type === "select") {

            var defaultValue = typeof this.state.selectValue === 'undefined' ? this.props.defaultValue : this.state.selectValue;
            var noSelection = this.props.noSelection || "None Specified";

            return (
                <select
					ref={this.setRef}
                    onChange={this.onSelectChange}
                    onBlur={this.onBlur}
                    value={defaultValue}
                    id={this.props.id || this.fieldId}
                    className={this.props.className || "form-control"}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'children', 'defaultValue', 'value', 'inline'])}
                    data-validation={this.state.validationFailure ? true : undefined}
                >
                    {this.props.contentType ? [
                        <option value='0'>{noSelection}</option>,
						<Loop over={this.props.contentType + '/list'} raw>
							{
								entry => <option value={entry.id} selected={entry.id == defaultValue ? true : undefined}>
									{
										entry[this.props.displayField || 'name']
									}
								</option>
							}
						</Loop>
					] : this.props.children}
                </select>
            );

        } else if (type === "textarea") {

            return (
                <textarea
					ref={this.setRef}
                    onChange={this.onChange}
                    onBlur={this.onBlur}
                    id={this.props.id || this.fieldId}
                    className={this.props.className || "form-control"}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline'])}
                    data-validation={this.state.validationFailure ? true : undefined}
                />
            );

        } else if (type === "submit" || type === "button") {
            var showIcon = this.props.icon;

            return (
                <button
                    className={this.props.className || "btn btn-primary"}
                    type={type}
                    {...omit(this.props, ['className', 'type', 'label', 'children', 'inline'])}
                >
                    {
                        !!showIcon && (
                            <i className={this.props.icon} />
                        )
                    }
                    {this.props.label || this.props.children || "Submit"}
                </button>
            );

        } else if (type === "checkbox") {

            return (
                <div className="custom-control custom-checkbox">
                    <input
						ref={this.setRef}
                        id={this.props.id || this.fieldId}
                        className={this.props.className || "form-control custom-control-input"}
                        aria-describedby={this.helpFieldId}
                        type={type}
                        onChange={this.onChange}
                        onBlur={this.onBlur}
                        data-validation={this.state.validationFailure ? true : undefined}
                        {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'value', 'defaultValue'])}
                        checked={this.props.value || this.props.defaultValue}
                    />
                    <label htmlFor={this.props.id || this.fieldId} className="custom-control-label">
                        {this.props.label}
                    </label>
                </div>
            );
        } else if (type === "radio") {

            return (
                <div className="custom-control custom-radio">
                    <input
						ref={this.setRef}
                        id={this.props.id || this.fieldId}
                        className={this.props.className || "form-control custom-control-input"}
                        name={this.props.name}
                        aria-describedby={this.helpFieldId}
                        type={type}
                        onChange={this.onChange}
                        onBlur={this.onBlur}
                        data-validation={this.state.validationFailure ? true : undefined}
                        {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'value', 'defaultValue'])}
                        checked={this.props.value || this.props.defaultValue}
                    />
                    <label htmlFor={this.props.id || this.fieldId} className="custom-control-label">
                        {this.props.label}
                    </label>
                </div>
            );
		}else if(type === "password"){
			var { pwVisible } = this.state;
			
			if(this.props.visible !== undefined){
				pwVisible = this.props.visible;
			}
			
			return <div className="input-group">
					<input
						ref={this.setRef}
						id={this.props.id || this.fieldId}
						className={this.props.className || "form-control"}
						aria-describedby={this.helpFieldId}
						type={pwVisible ? 'text' : type}
						onChange={this.onChange}
						onBlur={this.onBlur}
						onInput={this.onInput}
						data-validation={this.state.validationFailure ? true : undefined}
						{...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline'])}
					/>
					{!this.props.noVisiblityButton && (
						<div className="input-group-append clickable" onClick={() => {
							this.setState({pwVisible: !pwVisible});
						}}>
							<span className="input-group-text">
								<i className={"fa fa-eye" + (pwVisible ? '-slash' : '')} />
							</span>
						</div>
					)}
				</div>;
			
        } else {
            // E.g. ontypecanvas will fire. This gives a generic entry point for custom input types by just installing them:
            var handler = eventTarget['ontype' + type];
            if (handler) {
                return handler({ ...this.props, onChange: this.onChange, onBlur: this.onBlur }, type, this);
            }

            const fieldMarkup = (
                <input
                    ref={this.setRef}
                    id={this.props.id || this.fieldId}
                    className={this.props.className || "form-control"}
                    aria-describedby={this.helpFieldId}
                    type={type}
                    onChange={this.onChange}
                    onBlur={this.onBlur}
                    onInput={this.onInput}
                    data-validation={this.state.validationFailure ? true : undefined}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline'])}
                />
            );

            if (this.props.icon) {
                return <div className="input-wrapper">
                    {fieldMarkup}
                    <i className={this.props.icon}></i>
                </div>;
            }

            return fieldMarkup;
        }

    }

}

Input.propTypes={
	type: 'string',
	name: 'string',
	placeholder: 'string',
	value: 'string',
};

Input.icon = 'pen-nib';