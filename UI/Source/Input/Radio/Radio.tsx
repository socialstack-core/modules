import { DefaultInputType } from 'UI/Input/Default';
import { useId } from 'react';

type RadioInputType = DefaultInputType & {
	xs?: boolean,
	sm?: boolean,
	md?: boolean,
	lg?: boolean,
	xl?: boolean,
	flipped?: boolean,
	groupVariant?: string,
	groupIcon?: string,
	solid?: boolean,
	value?: boolean,
	defaultValue?: boolean,
}

// Registering 'radio' as being available
declare global {
	interface InputPropsRegistry {
		'radio': RadioInputType;
	}
}

const Radio: React.FC<CustomInputTypeProps<"radio">> = (props) => {
	const { field, validationFailure, label, onInputRef, helpFieldId } = props;
	const { className, flipped, xs, sm, md, lg, xl, groupVariant, groupIcon, solid, onChange, ...attribs } = field;
	const generatedId = useId();
	const id = attribs.id || generatedId;

	const classes = className ? className.split(" ") : [];

	if (validationFailure) {
		classes.push("is-invalid");
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

	classes.unshift("ui-check");
	classes.unshift("form-check");

	// NB: don't auto-apply a bottom margin as radio buttons will be rendered in groups and should remain in close proximity
	// TODO: apply bottom margin to wrapping radio-group element
	/*
	// ensure we include a standard bottom margin if one hasn't been supplied
	// (only apply this on the last item in the group)
	if (!className.find(element => element.startsWith("mb-"))) {
		className.push("mb-3");
	}
	*/

	var radioClass = classes.join(" ");
	var inputClass = "form-check-input" + (solid ? " form-check-input--solid" : "");

	// grouped radio button - requires different markup
	// ref: https://getbootstrap.com/docs/5.1/components/button-group/#checkbox-and-radio-button-groups
	if (groupVariant) {
		return <>
			<input
				ref={(el: HTMLInputElement) => onInputRef && onInputRef(el)}
				className="btn-check"
				type="radio"
				autoComplete="off"
				onInput={onChange}
				{...attribs}
				id={id}
			/>
			<label className={`btn ui-btn btn-outline-${groupVariant}`} htmlFor={id}>
				{groupIcon ? <>
					<i className={`fr ${groupIcon}`} />
					<span className="sr-only">{label}</span>
					</> : label}
			</label>
		</>;
	}

	return (
		<div className={attribs.readOnly ? '' : radioClass}>
			<input
				ref={(el: HTMLInputElement) => onInputRef && onInputRef(el)}
				className={inputClass}
				aria-describedby={helpFieldId}
				type="radio"
				onInput={onChange}
				{...attribs}
				id={id} />
			<label className="form-check-label" htmlFor={id}>
				{label}
			</label>
		</div>
	);
}

window.inputTypes['radio'] = Radio;
export default Radio;

