var id = 1;

import omit from 'UI/Functions/Omit';

export default class Toggle extends React.Component {

    constructor(props) {
        super(props);
        this.state = {};
        this.newId();
        this.onChange = this.onChange.bind(this);
        this.onBlur = this.onBlur.bind(this);
    }

    componentWillReceiveProps(props) {
        this.newId();
    }

    newId() {
        this.fieldId = 'form-field-slider-' + (id++);
        this.helpFieldId = this.fieldId + "-help";
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

    revalidate(e) {

        var validations = this.props.validate;

        if (!validations) {
            if (this.state.validationFailure) {
                this.setState({ validationFailure: null });
            }
            return;
        }

        if (!Array.isArray(validations)) {
            // Make it one:
            validations = [validations];
        }

        var v = e.target.value;
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
                break;
            }
        }

        if (vFail || this.state.validationFailure) {
            this.setState({ validationFailure: vFail });
        }
    }

    renderInput() {

        const { type } = this.props;
		
        if (type instanceof Function) {
            return type(this);
        }

        return (
            <div className={this.props.className ? "custom-control custom-checkbox custom-checkbox-slider " + this.props.className : "custom-control custom-checkbox custom-checkbox-slider"}>
                <input
                    id={this.props.id || this.fieldId}
                    className="form-control custom-control-input"
                    aria-describedby={this.helpFieldId}
                    type="checkbox"
                    onChange={this.onChange}
                    onBlur={this.onBlur}
                    data-validation={this.state.validationFailure ? true : undefined}
                    {...omit(this.props, ['id', 'className', 'onChange', 'onBlur', 'type', 'inline', 'value', 'defaultValue', 'checkedLabel'])}
                    checked={this.props.value === undefined ? this.props.defaultValue : this.props.value}
                />
                <label htmlFor={this.props.id || this.fieldId} className="custom-control-label">
                    <span className="unchecked-label">{this.props.label}</span>
                    <span className="checked-label">{this.props.checkedLabel ? this.props.checkedLabel : this.props.label}</span>
                </label>
            </div>
        );

    }

    render() {
        if (this.props.inline) {
            return this.renderInput();
        }

        return (
            <div className="form-group">
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

}