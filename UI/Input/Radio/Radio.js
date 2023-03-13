/* Bootstrap radio button
 * ref: https://getbootstrap.com/docs/5.0/forms/checks-radios/#radios

 */

export default function Radio(props) {
	var { id, className, name, label, onClick, onChange, onBlur, invalid, checked, disabled, solid } = props;
	
	// check if we mistakenly used onClick instead of onChange
	if (onClick && !onChange) {
		onChange = onClick;
	}

	className = className ? className.split(" ") : [];

	if (invalid) {
		className.push("is-invalid");
	}

	className.unshift("form-check");

	// NB: don't auto-apply a bottom margin as radio buttons will be rendered in groups and should remain in close proximity
	// TODO: apply bottom margin to wrapping radio-group element
	/*
	// ensure we include a standard bottom margin if one hasn't been supplied
	// (only apply this on the last item in the group)
	if (!className.find(element => element.startsWith("mb-"))) {
		className.push("mb-3");
	}
	*/

	var radioClass = className.join(" ");
	var inputClass = "form-check-input" + (solid ? " form-check-input--solid" : "");
	
	return (
		<div className={props.readonly ? '' : radioClass}>
			{props.readonly ? (
				(props.value === undefined ? props.defaultValue : props.value) ? <b>Yes (readonly) </b> : <b>No (readonly) </b>
			) : 
			<input class={inputClass} type="radio" name={name} id={id} 
				onChange={onChange}
				onBlur={onBlur}
				checked={checked ? "checked" : undefined} 
				disabled={disabled ? "disabled" : undefined} 
				value={props.value}
				defaultChecked={props.defaultValue}
			/>}
			<label class="form-check-label" htmlFor={id}>
				{label}
			</label>
		</div>
	);
}

Radio.propTypes = {
};

Radio.defaultProps = {
}

Radio.icon = 'dot-circle';
