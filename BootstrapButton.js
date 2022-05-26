/* Bootstrap button
 * ref: https://getbootstrap.com/docs/5.0/components/buttons/

 */

function plainLabel(jsx){
    var div = document.createElement("div");
    div.innerHTML = jsx;

    return div.textContent || div.innerText || "";
}

export default function Button(props) {
	var { children, id, className, tag, buttonType, href, onClick, disabled, variant, outlined, noWrap, sm, lg } = props;
	
	className = className ? className.split(" ") : [];

	var Tag = tag ? tag : "button";

	switch (Tag) {
		case 'button':
		case 'a':
		case 'input':
			break;

		default:
			Tag = "button";
			break;
	}

	if (href) {
		Tag = "a";
	}

	if (!buttonType) {
		buttonType = "button";
	}

	switch (buttonType) {
		case 'button':
		case 'submit':
		case 'reset':
			break;

		default:
			buttonType = "button";
			break;
	}

	if (!variant) {
		variant = "primary";
	}

	if (noWrap) {
		className.unshift("text-nowrap");
	}

	if (disabled) {
		className.unshift("disabled");
	}

	if (sm) {
		className.unshift("btn-sm");
	}

	if (lg) {
		className.unshift("btn-lg");
	}

	className.unshift("btn-" + (outlined ? "outline-" : "") + variant);
	className.unshift("btn");

	var btnClass = className.join(" ");

	return (
		<Tag className={btnClass} onClick={onClick} id={id}
			disabled={Tag != "a" && disabled ? "disabled" : undefined}
			aria-disabled={disabled ? "true" : undefined}
			tabindex={disabled ? "-1" : undefined}
			href={Tag == "a" ? href : undefined}
			type={Tag == "a" ? undefined : buttonType}
			role={Tag == "a" ? "button" : undefined}
			value={Tag == "input" ? plainLabel(children) : undefined}>
			{children}
		</Tag>
	);
}

Button.propTypes = {
};

Button.defaultProps = {
}

Button.icon = 'keyboard';
