import { DefaultInputType } from 'UI/Input/Default';
import { useId } from 'react';

type CheckboxInputType = DefaultInputType & {
	xs?: boolean,
	sm?: boolean,
	md?: boolean,
	lg?: boolean,
	xl?: boolean,
	isSwitch?: boolean,
	flipped?: boolean,
	groupVariant?: string,
	groupIcon?: string,
	solid?: boolean,
	value?: boolean,
	defaultValue?: boolean
};

// Registering 'checkbox' as being available
declare global {
	interface InputPropsRegistry {
		'checkbox': CheckboxInputType;
		'bool': CheckboxInputType;
		'boolean': CheckboxInputType;
	}
}

interface CheckboxProps {
	field: CheckboxInputType,
	config: CustomInputTypePropsBase
}

const Checkbox: React.FC<CheckboxProps> = (props) => {
	const { field, config } = props;
	const { label, validationFailure, onInputRef, helpFieldId } = config;
	let { isSwitch, flipped, xs, sm, md, lg, xl, groupVariant, groupIcon, solid, className, onChange: fieldOnChange, style, ...attribs } = field;
	const generatedId = useId();
	const id = attribs.id || generatedId;
	
	const classes = className ? className.split(" ") : [];

	if (isSwitch) {
		classes.unshift("form-switch");
	}

	if (flipped) {
		classes.unshift("form-check--flipped");
	}

	if (xs) {
		classes.unshift("form-check--xs");
	}

	if (sm) {
		classes.unshift("form-check--sm");
	}

	if (md) {
		classes.unshift("form-check--md");
	}

	if (lg) {
		classes.unshift("form-check--lg");
	}

	if (xl) {
		classes.unshift("form-check--xl");
	}

	if (validationFailure) {
		classes.push("is-invalid");
	}

	classes.unshift("ui-check");
	classes.unshift("form-check");

	/* wrapped by UI/Input
	// ensure we include a standard bottom margin if one hasn't been supplied
	if (!classes.find(element => element.startsWith("mb-"))) {
		classes.push("mb-3");
	}
	*/

	var checkClass = classes.join(" ");
	var inputClass = "form-check-input" + (solid ? " form-check-input--solid" : "");

	// grouped radio button - requires different markup
	// ref: https://getbootstrap.com/docs/5.1/components/button-group/#checkbox-and-radio-button-groups
	if (groupVariant) {
		return <>
			<input
				ref={(el) => onInputRef && onInputRef(el as HTMLElement)}
				className="btn-check"
				type="checkbox"
				autoComplete="off"
				onInput={fieldOnChange as unknown as React.InputEventHandler<HTMLInputElement>}
				{...attribs}
				id={id}
			/>
			<label className={`btn btn-outline-${groupVariant}`} htmlFor={id}>
				{groupIcon ? <>
					<i className={`fr ${groupIcon}`} />
					<span className="sr-only">{label}</span>
				</> : label}
			</label>
		</>;
	}

	return (
		<div className={field.readOnly ? '' : checkClass} style={style}>
			<input
					ref={(el) => onInputRef && onInputRef(el as HTMLElement)}
					className={inputClass}
					aria-describedby={helpFieldId}
					type="checkbox"
					onInput={fieldOnChange as unknown as React.InputEventHandler<HTMLInputElement>}
					{...attribs}
					role={isSwitch ? "switch" : undefined}
					id={id}
					// haptic support for Safari
					// @ts-ignore
					switch={isSwitch ? true : undefined}
			/>
			<label className="form-check-label" htmlFor={id}>
				{label}
			</label>
		</div>
	);
}

window.inputTypes['checkbox'] = (props: CustomInputTypeProps<"checkbox">) => <Checkbox field={props.field} config={props} />;
window.inputTypes['bool'] = (props: CustomInputTypeProps<"bool">) => <Checkbox field={props.field} config={props} />;
window.inputTypes['boolean'] = (props: CustomInputTypeProps<"boolean">) => <Checkbox field={props.field} config={props} />;