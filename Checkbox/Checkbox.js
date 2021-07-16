/* Bootstrap checkbox
 * ref: https://getbootstrap.com/docs/5.0/forms/checks-radios/#checks

 */

export default function Checkbox(props) {
	var { id, className, label, onChange, onBlur, invalid, checked, disabled, solid, isSwitch } = props;
	
	className = className.split(" ") || [];

	if (isSwitch) {
		checkClass.unshift("form-switch");
	}

	if (invalid) {
		className.push("is-invalid");
	}

	className.unshift("form-check");

	// ensure we include a standard bottom margin if one hasn't been supplied
	if (!className.find(element => element.startsWith("mb-"))) {
		className.push("mb-3");
	}

	var checkClass = className.join(" ");
	var inputClass = "form-check-input" + solid ? " form-check-input--solid" : "";

	return (
		<div className={checkClass}>
			<input class={inputClass} type="checkbox" id={id} 
				onChange={onChange}
				onBlur={onBlur}
				checked={checked ? "checked" : undefined} 
				disabled={disabled ? "disabled" : undefined} />
			<label class="form-check-label" htmlFor={id}>
				{label}
			</label>
		</div>
	);
}

Checkbox.propTypes = {
    isSwitch: 'bool'
};

Checkbox.defaultProps = {
    isSwitch: false
}

Checkbox.icon = 'check-square';
