var id = 1;

import Loop from 'UI/Loop';
import omit from 'UI/Functions/Omit';

var inputTypes = global.inputTypes = global.inputTypes || {};

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
        this.helpBeforeFieldId = this.fieldId + "-help-before";
        this.helpAfterFieldId = this.fieldId + "-help-after";
        this.helpIconFieldId = this.fieldId + "-help-icon";
        this.describedById = this.helpBeforeFieldId || this.helpAfterFieldId || this.helpIconFieldId;
    }

    renderField() {
        // possible to hide all labels globally with $ss_form_field_label_hidden;
        // this allows us to specify fields which should always display their label
        var labelClass = this.props.labelImportant ? "label-important" : "";

        var help = this.props.help;
        var helpPosition = 'above';

        if (help) {

            if (global.invertHelpPosition) {
                helpPosition = 'below';
            }

            if (this.props.helpPosition) {
                helpPosition = this.props.helpPosition;
            }

        }

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
                    help && (helpPosition == 'above' || helpPosition == 'top') && (
                        <small id={this.helpBeforeFieldId} className="form-text text-muted form-text-above">
                            {help}
                        </small>
                    )
            }
            {
                (this.state.validationFailure && this.props.validateErrorLocation && this.props.validateErrorLocation == "above") && (
                    <div className="validation-error">
                        {this.props.validationFailure ? this.props.validationFailure(this.state.validationFailure) : this.state.validationFailure.ui}
                    </div>
                )
            }

                {
                    help && helpPosition == 'icon' && (
                        <div className="form-control-icon-wrapper">
                            {
                                this.renderInput()
                            }
                            <span id={this.helpIconFieldId} className="fa fa-fw fa-info-circle" title={help}></span>
                        </div>
                    )
                }

                {
                    helpPosition != 'icon' && (
                        this.renderInput()
                    )
                }

                {
                    help && (helpPosition == 'below' || helpPosition == 'bottom') && (
                        <small id={this.helpAfterFieldId} className="form-text text-muted">
                            {help}
                        </small>
                    )
                }

            {
                (this.state.validationFailure && (!this.props.validateErrorLocation || (this.props.validateErrorLocation && this.props.validateErrorLocation != "above"))) && (
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
		
		var field = this.inputRef;
		if(!field){
			return false;
		}
		
        var v = field.type=='checkbox' ? field.checked : field.value;
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
                    var mtd = require("UI/Functions/Validation/" + valType).default;
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

        const { type, contentType } = this.props;

        if (type instanceof Function) {
            return type(this);
        }
		
		if(contentType){
			// contentType="a-mime-type". The mime type must be lowercase.
			var handler = inputTypes[contentType];
			
			if(handler != null){
				// Content type handlers come first.
				return handler({ ...this.props, onChange: this.onChange, onBlur: this.onBlur }, type, this);
			}
		}
		
        // TODO: wire up related controls (e.g. field A is disabled based on checkbox B)
        var disabledBy = this.props.disabledBy;
        var enabledBy = this.props.enabledBy;
        var fieldName = this.props.fieldName;

        if (type === "select") {
			var {noSelectionValue} = this.props;
            var defaultValue = typeof this.state.selectValue === 'undefined' ? this.props.defaultValue : this.state.selectValue;
            var noSelection = this.props.noSelection || "None Specified";
            var mobileNoSelection = this.props.mobileNoSelection || "None Specified";
			if(noSelectionValue === undefined)
			{
				noSelectionValue = '0';
			}
			
            var html = document.getElementsByTagName("html");

            if (html.length && html[0].classList.contains("device-mobile")) {
                noSelection = mobileNoSelection;
            }

            return (
                <select
					ref={this.setRef}
                    onChange={this.onSelectChange}
                    onBlur={this.onBlur}
                    value={defaultValue}
                    id={this.props.id || this.fieldId}
                    className={this.props.className || "form-control"}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'children', 'defaultValue', 'value', 'inline', 'help', 'helpIcon', 'fieldName'])}
                    data-field={fieldName}
                    data-validation={this.state.validationFailure ? true : undefined}
                >
                    {this.props.contentType ? [
                        <option value={noSelectionValue}>{noSelection}</option>,
						<Loop over={this.props.contentType + '/list'} raw filter={this.props.filter}>
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
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'help', 'helpIcon', 'fieldName'])}
                    data-validation={this.state.validationFailure ? true : undefined}
                />
            );

        } else if (type === "submit" || type === "button") {
            var showIcon = this.props.icon;

            return (
                <button
                    className={this.props.className || "btn btn-primary"}
                    type={type}
                    {...omit(this.props, ['className', 'type', 'label', 'children', 'inline', 'help', 'helpIcon', 'fieldName'])}
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
                        aria-describedby={this.describedById}
                        type={type}
                        onChange={this.onChange}
                        onBlur={this.onBlur}
                        data-field={fieldName}
                        data-validation={this.state.validationFailure ? true : undefined}
                        {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'value', 'defaultValue', 'help', 'helpIcon', 'fieldName'])}
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
                        aria-describedby={this.describedById}
                        type={type}
                        onChange={this.onChange}
                        onBlur={this.onBlur}
                        data-field={fieldName}
                        data-validation={this.state.validationFailure ? true : undefined}
                        {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'value', 'defaultValue', 'help', 'helpIcon', 'fieldName'])}
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
                    aria-describedby={this.describedById}
						type={pwVisible ? 'text' : type}
						onChange={this.onChange}
						onBlur={this.onBlur}
						onInput={this.onInput}
						data-validation={this.state.validationFailure ? true : undefined}
                        {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'help', 'helpIcon', 'fieldName'])}
					/>
                {!this.props.noVisiblityButton && !this.props.noVisibilityButton && (
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
            var handler = inputTypes['ontype' + type];
            if (handler) {
                return handler({ ...this.props, onChange: this.onChange, onBlur: this.onBlur }, type, this);
            }

            const fieldMarkup = (
                <input
                    ref={this.setRef}
                    id={this.props.id || this.fieldId}
                    className={this.props.className || "form-control"}
                    aria-describedby={this.describedById}
                    type={type}
                    onChange={this.onChange}
                    onBlur={this.onBlur}
                    onInput={this.onInput}
                    data-disabled-by={disabledBy}
                    data-enabled-by={enabledBy}
                    data-validation={this.state.validationFailure ? true : undefined}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'help', 'helpIcon', 'fieldName'])}
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