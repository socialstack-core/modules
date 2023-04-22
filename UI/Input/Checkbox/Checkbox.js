/* Bootstrap checkbox
 * ref: https://getbootstrap.com/docs/5.0/forms/checks-radios/#checks

 */

export default function Checkbox(props) {
	var { id, className, label, name, onChange, onClick, onBlur, invalid, checked, disabled, solid, isSwitch } = props;

	// check if we mistakenly used onClick instead of onChange
	if (onClick && !onChange) {
		onChange = onClick;
	}
	
	className = className ? className.split(" ") : [];

	if (isSwitch) {
		checkClass.unshift("form-switch");
	}

	if (invalid) {
		className.push("is-invalid");
	}

	className.unshift("form-check");

	/* wrapped by UI/Input
	// ensure we include a standard bottom margin if one hasn't been supplied
	if (!className.find(element => element.startsWith("mb-"))) {
		className.push("mb-3");
	}
	*/

	var checkClass = className.join(" ");
	var inputClass = "form-check-input" + (solid ? " form-check-input--solid" : "");
	
	console.log(checked);
	
	return (
		<div className={props.readonly ? '' : checkClass} style={props.style}>
			{props.readonly ? (
				(props.value === undefined ? props.defaultValue : props.value) ? <b>Yes (readonly) </b> : <b>No (readonly) </b>
			) : <input class={inputClass} type="checkbox" name = {name} id={id} 
				onInput={onChange}
				onBlur={onBlur}
				checked={checked === undefined ? undefined : checked} 
				disabled={disabled ? "disabled" : undefined}
				value={props.value}
				defaultChecked={props.defaultValue}
				onClick={props.onClick}
				onMouseUp={props.onMouseUp}
			/>}
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
