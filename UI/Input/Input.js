var id = 1;

import Loop from 'UI/Loop';
import omit from 'UI/Functions/Omit';
import Checkbox from 'UI/Input/Checkbox';
import Radio from 'UI/Input/Radio';

const DefaultPasswordStrength = 5;

var inputTypes = global.inputTypes = global.inputTypes || {};

function padded(time){
	if(time < 10){
		return '0' + time;
	}
	return time;
}

function dateFormatStr(d){
	return d.getFullYear() + '-' + padded(d.getMonth()+1) + '-' + padded(d.getDate()) + 'T' + padded(d.getHours()) + ':' + padded(d.getMinutes());
}

// introduced as to get a textarea to show a remaining character count, 
// 'maxlength' / 'showLength' props need to be set; 
// this ensures lowercase / camelCase differences don't prevent this from working
function caseInsensitivePropCheck(props, checkFor) {

    if (!props || !checkFor) {
        return false;
    }

    var re = new RegExp(checkFor, "i");
    var actualCase = Object.keys(props).find(key => re.test(key)) !== undefined;

    return actualCase ? props[actualCase] : false;
}

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
        this.onTransparentChange = this.onTransparentChange.bind(this);
    }

    componentWillReceiveProps(props) {
        this.newId();
    }

    componentDidMount() {

        if (this.props.autoFocus && this.inputRef) {
            this.inputRef.focus();
        }

    }

    newId() {
        this.fieldId = 'form-field-' + (id++);
        this.helpBeforeFieldId = this.fieldId + "-help-before";
        this.helpAfterFieldId = this.fieldId + "-help-after";
        this.helpIconFieldId = this.fieldId + "-help-icon";
        this.describedById = this.helpBeforeFieldId || this.helpAfterFieldId || this.helpIconFieldId;
    }
	
	renderLabel() {

		if(!this.props.label || 
            this.props.type == "submit" || 
            this.props.type == "checkbox" || 
            this.props.type == "radio" ||
            this.props.type == "toggle"){
			return null;
		}
		
		return <label htmlFor={this.props.id || this.fieldId} className="form-label">
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

    // Hack to allow alpha values in color input
    onTransparentChange(e) {
        if (this.props.type == "color" && this.inputRef && e && e.target) {
            if (e.target.checked) {
                if (this.inputRef.value && this.inputRef.value.length == 7) {
                    this.inputRef.type = "text";
                    this.inputRef.value += "00";
                    this.inputRef.style.display = "none";
                }
            } else {
                if (this.inputRef.value && this.inputRef.value.length == 9) {
                    this.inputRef.value = this.inputRef.value.slice(0, -2);
                    this.inputRef.type = "color";
                    this.inputRef.style.display = "block";
                }
            }
        }
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
				noSelectionValue = '';
			}

            if (window.matchMedia('(max-width: 752px) and (pointer: coarse) and (orientation: portrait)').matches ||
                window.matchMedia('(max-height: 752px) and (pointer: coarse) and (orientation: landscape)').matches) {
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
            var maxLength = caseInsensitivePropCheck(this.props, 'maxlength');

            return (<>
                <textarea
                    ref={this.setRef}
                    onChange={this.onChange}
                    onBlur={this.onBlur}
                    autocomplete={this.props.autocomplete}
                    id={this.props.id || this.fieldId}
                    className={(this.props.className || "form-control") + (this.state.validationFailure ? ' is-invalid' : '')}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'help', 'helpIcon', 'fieldName'])}
                    oninput={e => {
                        this.setState({ textAreaLength: e.target.textLength });

                        if (typeof this.props.onInput == 'function') {
                            this.props.onInput(e);
                        }
                    }}
                />
                {maxLength && caseInsensitivePropCheck(this.props, 'showLength') && <>
                    <div className="textarea-char-count">
                        {(this.state.textAreaLength ? this.state.textAreaLength : this.props.defaultValue ? this.props.defaultValue.length : 0) + "/" + maxLength}
                    </div>
                </>}
            </>);

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

        } else if (type === "checkbox" || type == "bool" || type == "boolean") {
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
                    checked={this.props.value}
					defaultValue={this.props.defaultValue}
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
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'defaultValue', 'help', 'helpIcon', 'fieldName'])}
                    checked={this.props.defaultValue == this.props.value}
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
                        <meter className="meter password-meter" min="0" max="100" value={strength} low="44" high="55" optimum="80"></meter>
                    )}
                </>;
        } else {
            // E.g. ontypecanvas will fire. This gives a generic entry point for custom input types by just installing them:
            var handler = inputTypes['ontype' + type];
            if (handler) {
                return handler({ ...this.props, onChange: this.onChange, onBlur: this.onBlur }, type, this);
            }
			
			var props = omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'help', 'helpIcon', 'fieldName']);
			
			if(type == 'datetime-local'){
				if(props.defaultValue && props.defaultValue.getTime){
					// It's a date
					var d = props.defaultValue;
					props.defaultValue = dateFormatStr(props.defaultValue);
				}
				
				if(props.value && props.value.getTime){
					// It's a date
					var d = props.value;
					props.value = dateFormatStr(props.value);
				}
				
				if(props.min && props.min.getTime){
					// It's a date
					var d = props.min;
					props.min = dateFormatStr(props.min);
				}
				
				if(props.max && props.max.getTime){
					// It's a date
					var d = props.max;
					props.max = dateFormatStr(props.max);
				}
				
				// Add onGetValue for converting the local time into utc
				props.ref=(r) => {
					this.setRef(r);
					
					if(r){
						r.onGetValue = (v,r) => {
							if(this.inputRef == r && v){
								return new Date(Date.parse(v));
							}
							return v;
						};
					}
				};
			}
			
            var fieldMarkup = (
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
                    {...props}
                />
            );

            if (this.props.type == 'color') {
                const colorValue = this.inputRef && this.inputRef.value
                    ? this.inputRef.value
                    : this.props.defaultValue
                const isTransparent = colorValue && colorValue.length == 9;

                if (isTransparent) {
                    fieldMarkup = (
                        <input
                            ref={this.setRef}
                            id={this.props.id || this.fieldId}
                            className={(this.props.className || "form-control") + (this.state.validationFailure ? ' is-invalid' : '')}
                            aria-describedby={this.describedById}
                            type="text"
                            onChange={this.onChange}
                            onBlur={this.onBlur}
                            onInput={this.onInput}
                            autocomplete={this.props.autocomplete}
                            data-disabled-by={disabledBy}
                            data-enabled-by={enabledBy}
                            style={{display: "none"}}
                            {...props}
                        />
                    );
                }

                return <div className="input-wrapper color-input">
                    {fieldMarkup}
                    {(this.props.allowTransparency || isTransparent) &&
                        <>
                            <label className="transparent-label">Transparent</label>
                            <input 
                                type="checkbox" 
                                name="isTransparent" 
                                checked={isTransparent} 
                                onChange={this.onTransparentChange}
                            />
                        </>
                    }
                    {this.props.icon && <i className={this.props.icon}></i>}
                </div>;
            }

            if (this.props.icon) {
                return <div className="input-wrapper">
                    {fieldMarkup}
                    <i className={this.props.icon}></i>
                </div>;
            }

            if (this.props.type == 'datetime-local' && this.props.roundMinutes && this.props.roundMinutes > 0) {
                fieldMarkup.props.onChange = e => {
                    var [hours, minutes] = e.target.value.slice(-4).split(':');
                    hours = parseInt(hours);
                    minutes = parseInt(minutes);

                    var time = (hours * 60) + minutes; 

                    var rounded = Math.round(time / this.props.roundMinutes) * this.props.roundMinutes;
                   
                    e.target.value =  e.target.value.slice(0, -4) + Math.floor(rounded / 60) + ':' + String(rounded % 60).padStart(2, '0');

                    this.onChange(e);
                };
            }

            return fieldMarkup;
        }

    }

}

Input.propTypes={
	type: 'string',
	name: 'string',
	help: 'string',
	placeholder: 'string',
	label: 'string',
	value: 'string',
};

Input.icon = 'pen-nib';