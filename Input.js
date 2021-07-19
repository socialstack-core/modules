var id = 1;

import Loop from 'UI/Loop';
import omit from 'UI/Functions/Omit';
import Checkbox from 'UI/Input/Checkbox';
import Radio from 'UI/Input/Radio';

const DefaultPasswordStrength = 5;

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
	
	renderLabel() {
		// possible to hide all labels globally with $ss_form_field_label_hidden;
        // this allows us to specify fields which should always display their label
        var labelClass = this.props.labelImportant ? "label-important" : "form-label";

		if(!this.props.label || 
            this.props.type == "submit" || 
            this.props.type == "checkbox" || 
            this.props.type == "radio" ||
            this.props.type == "toggle"){
			return null;
		}
		
		return <label htmlFor={this.props.id || this.fieldId} className={labelClass}>
			{this.props.label}
			{!this.props.hideRequiredStar && this.props.validate && this.props.validate.indexOf("Required")!=-1 && <span className="is-required-field"></span>}
		</label>;
	}
	
    renderField() {
        var { help, labelPosition } = this.props;
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
				{labelPosition != 'float' && this.renderLabel()}
                {
                    help && (helpPosition == 'above' || helpPosition == 'top') && (
                        <div id={this.helpBeforeFieldId} className="form-text form-text-above">
                            {help}
                        </div>
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
				
				{labelPosition == 'float' && this.renderLabel()}
				
                {
                    help && (helpPosition == 'below' || helpPosition == 'bottom') && (
                        <div id={this.helpAfterFieldId} className="form-text">
                            {help}
                        </div>
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
		
        var groupClass = this.props.groupClassName ? "mb-3 " + this.props.groupClassName : "mb-3";
		
		if(this.props.labelPosition == 'float'){
			groupClass = 'form-floating ' + groupClass;
		}

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

        if (field.type === 'password') {
            this.setState({strength: this.passwordStrength(this.inputRef.value)});
        }

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

    passwordStrength(pw) {
        var reUpperCase = /[A-Z]+/;
        var reLowerCase = /[a-z]+/;
        var reNum       = /[0-9]+/;
        var reUnique    = /[^A-Za-z0-9]+/;

        var rePass     = /[Pp][Aa4][Ss5$]{2}[Ww][Oo0][Rr][Dd]/;
        var reAlphaSeq = /abcdef?g?h?i?j?k?l?m?n?o?p?q?r?s?t?u?v?w?x?y?z?/;
        var reKbWalk1  = /[1!][2"][3£][4$][5%][6^]?[7&]?[8*]?[9(]?[0)]?[-_]?[=+]?/;
        var reKbWalk2  = /[Qq][Ww][Ee][Rr][Tt][Yy]?[Uu]?[Ii]?[Oo]?[Pp]?[[{]?[\]}]?/;
        var reKbWalk3  = /asdfgh?j?k?l?;?'?#?/;
        var reKbWalk4  = /zxcvbn?m?,?\.?\/?/;

        var reward = 0;
        reward += reUpperCase.test(pw) ? 3 : 0;
        reward += reLowerCase.test(pw) ? 3 : 0;
        reward += reNum.test(pw)       ? 3 : 0;
        reward += reUnique.test(pw)    ? 3 : 0;

        var penalty = 0;
        penalty += rePass.test(pw)     ? 30 : 0;
        penalty += reAlphaSeq.test(pw) ? 20 : 0;
        penalty += reKbWalk1.test(pw)  ? 20 : 0;
        penalty += reKbWalk2.test(pw)  ? 20 : 0;
        penalty += reKbWalk3.test(pw)  ? 20 : 0;
        penalty += reKbWalk4.test(pw)  ? 20 : 0;

        var symbolPool = 0;
        symbolPool += reUpperCase.test(pw) ? 26 : 0;
        symbolPool += reLowerCase.test(pw) ? 26 : 0;
        symbolPool += reNum.test(pw)       ? 10 : 0;
        symbolPool += reUnique.test(pw)    ? 33 : 0;

        var uniqueChars = [];
        for (let char of pw) {
            if (!uniqueChars.includes(char)) {
                uniqueChars.push(char);
            }
        }
        var uniqueCharCount = uniqueChars.length;
        reward += (uniqueCharCount) / 4;


        var possibleCombos = Math.pow(symbolPool, pw.length);
        var entropy = Math.log2(possibleCombos);
        var strength = (entropy + reward - penalty) / 1.5;

        return strength;
    }

    pwStrengthClass(strength) {
        if (strength >= 55) {
            return 'strong';
        } else if (strength >= 45) {
            return 'medium';
        } else {
            return 'weak';
        }
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

            var selectClass = this.props.className || "form-select" + (this.state.validationFailure ? ' is-invalid' : '');

            if (defaultValue == undefined) {
                selectClass += " no-selection";
            }

            return (
                <select
					ref={this.setRef}
                    onChange={this.onSelectChange}
                    onBlur={this.onBlur}
                    autocomplete={this.props.autocomplete}
                    value={defaultValue}
                    id={this.props.id || this.fieldId}
                    className={selectClass}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'children', 'defaultValue', 'value', 'inline', 'help', 'helpIcon', 'fieldName'])}
                    data-field={fieldName}
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
                    autocomplete={this.props.autocomplete}
                    id={this.props.id || this.fieldId}
                    className={(this.props.className || "form-control") + (this.state.validationFailure ? ' is-invalid' : '')}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'help', 'helpIcon', 'fieldName'])}
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
                <Checkbox
					ref={this.setRef}
                    id={this.props.id || this.fieldId}
                    className={this.props.className}
                    label={this.props.label}
                    aria-describedby={this.describedById}
                    onChange={this.onChange}
                    onBlur={this.onBlur}
                    data-field={fieldName}
                    invalid={this.state.validationFailure ? true : undefined}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'value', 'defaultValue', 'help', 'helpIcon', 'fieldName'])}
                    checked={this.props.value || this.props.defaultValue}
                    disabled={false}
                    isSwitch={false}
                />
            );
        } else if (type === "radio") {

            return (
                <Radio
					ref={this.setRef}
                    id={this.props.id || this.fieldId}
                    className={this.props.className}
                    name={this.props.name}
                    label={this.props.label}
                    aria-describedby={this.describedById}
                    onChange={this.onChange}
                    onBlur={this.onBlur}
                    data-field={fieldName}
                    invalid={this.state.validationFailure ? true : undefined}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'value', 'defaultValue', 'help', 'helpIcon', 'fieldName'])}
                    checked={this.props.value || this.props.defaultValue}
                    disabled={false}
                />
            );
		} else if(type === "password"){
			var { pwVisible } = this.state;

			if(this.props.visible !== undefined){
				pwVisible = this.props.visible;
			}

            if (this.props.showMeter) {
                var strengthClass = 'none';
                var strength = DefaultPasswordStrength;

                if (this.state.strength) {
                    strength = this.state.strength;
                    strengthClass = this.pwStrengthClass(strength);
                }
            }

			return <>
                    <div className="input-group">
                        <input
                            ref={this.setRef}
                            id={this.props.id || this.fieldId}
                            className={(this.props.className || "form-control") + (this.state.validationFailure ? ' is-invalid' : '')}
                            aria-describedby={this.describedById}
                            type={pwVisible ? 'text' : type}
                            autocomplete={this.props.autocomplete}
                            onChange={this.onChange}
                            onBlur={this.onBlur}
                            onInput={this.onInput}
                            {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'help', 'helpIcon', 'fieldName'])}
                        />
                        {!this.props.noVisiblityButton && !this.props.noVisibilityButton && (
							<span className="input-group-text clickable" onClick={() => {
                                this.setState({pwVisible: !pwVisible});
                            }}>
								<i className={"fa fa-fw fa-eye" + (pwVisible ? '-slash' : '')} />
							</span>
                        )}
                    </div>
                    {this.props.showMeter && (
                        <meter className="meter" min="0" max="100" value={strength} low="44" high="55" optimum="80"></meter>
                    )}
                </>;
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
                    className={(this.props.className || "form-control") + (this.state.validationFailure ? ' is-invalid' : '')}
                    aria-describedby={this.describedById}
                    type={type}
                    onChange={this.onChange}
                    onBlur={this.onBlur}
                    onInput={this.onInput}
                    autocomplete={this.props.autocomplete}
                    data-disabled-by={disabledBy}
                    data-enabled-by={enabledBy}
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