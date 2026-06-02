/**
 * Props for the button component.
 * @icon fal fa-mouse-pointer
 * @description A clickable button.
 */
interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement>  {

	/**
	 * True if the button is disabled.
	 */
	disabled?: boolean,

	/**
	 * The type of tag to use on the button itself.
	 */
	tag?: 'button' | 'input' | 'a',

	/**
	 * optional additional class name(s)
	 */
	className?: string,

	/**
	 * The button type.
	 */
	type?: 'button' | 'reset' | 'submit',

	buttonType?: 'button' | 'reset' | 'submit',

	/**
	 * Optional href if it is a link
	 */
	href?: string,

	/**
	 * True if the link should be opened within a new tab.
	 */
	external?: boolean,

	/**
	 * True if the button should be the extra small style.
	 */
	xs?: boolean,

	/**
	 * True if the button should be the small style.
	 */
	sm?: boolean,

	/**
	 * True if the button should be the regular style.
	 */
	md?: boolean,

	/**
	 * True if the button should be the large style.
	 */
	lg?: boolean,

	/**
	 * True if the button should be the extra large style.
	 */
	xl?: boolean,

	/**
	 * True if the button should function as a close button.
	 */
	close?: boolean,

	/**
	 * True if the button should wrap text inside it.
	 */
	allowWrap?: boolean,

	/**
	 * True if the button should be the outlined style.
	 */
	outlined?: boolean,

	/**
	 * The style variant, "primary", "secondary" etc. "primary" assumed.
	 */
	variant?: string,

	/**
	 * optional external target link to open in a new window when:
	 * shift-clicking, middle-mouse button clicking or pressing shift+enter / shift+space when focused
	 */
	externalLink?: string
}

/**
 * Button component
 * ref: https://getbootstrap.com/docs/5.0/components/buttons/
 */
const Button: React.FC<React.PropsWithChildren<ButtonProps>> = ({
	children, className, tag, href, external, variant,
	disabled, outlined, close, allowWrap, xs, sm, md, lg, xl, ...props
}) => {
	let buttonType = props.type || props.buttonType;
	let externalLink = props.externalLink?.trim();

	if (!externalLink?.length) {
		externalLink = undefined;
	}

	var classes = className ? className.split(" ") : [];
	var Tag = href ? "a" : (tag || "button");

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

	if (allowWrap) {
		classes.unshift("btn--wrapped");
	}

	// sizing
	if (xs) {
		classes.unshift("btn-xs");
	}

	if (sm) {
		classes.unshift("btn-sm");
	}

	if (md) {
		classes.unshift("btn-md");
	}

	if (lg) {
		classes.unshift("btn-lg");
	}

	if (xl) {
		classes.unshift("btn-xl");
	}

	if (close) {
		classes.unshift("btn--close");
	}

	classes.unshift("btn-" + (outlined ? "outline-" : "") + variant);
	classes.unshift("ui-btn");
	classes.unshift("btn");

	var btnClass = classes.join(" ");

	const openInNewTab = (url: string) => {

		if (url.startsWith("//")) {
			url = url.substring(2);
		}

		if (url.startsWith("/")) {
			url = url.substring(1);
		}

		if (!url.startsWith("http://") && !url.startsWith("https://")) {
			url = [window.location.origin, url].join("/");
		}

		window.open(url, "_blank", "noopener noreferrer");
	};

	function handleMouseDown(e: React.MouseEvent<HTMLElement>) {

		if (!externalLink) {
			return;
		}

		// middle-clicking links works OOTB, so ignore those
		// only apply this to buttons acting as links
		if ((e.target as HTMLElement).nodeName != "A" && variant == "link") {

			if (e.button === 1 || (e.button === 0 && e.shiftKey)) {
				openInNewTab(externalLink);
			}

		}

	}

	function handleKeyDown(e: React.KeyboardEvent<HTMLElement>) {

		if (!externalLink) {
			return;
		}

		if ((e.key === 'Enter' || e.key === 'Space') && e.shiftKey) {
			openInNewTab(externalLink);
		}
	}

	// Forced any required 
	const mouseDown: any = (e: any) => handleMouseDown(e as React.MouseEvent<HTMLElement>);
	const keyDown: any = (e: any) => handleKeyDown(e as React.KeyboardEvent<HTMLElement>);

	return (
		<Tag className={btnClass}
			disabled={Tag != "a" && disabled ? true : undefined}
			inert={Tag == "a" && disabled ? true : undefined}
			aria-disabled={disabled ? "true" : undefined}
			aria-label={close ? `Close` : undefined}
			tabIndex={disabled ? -1 : undefined}
			href={Tag == "a" ? href : undefined}
			target={href && external ? "_blank" : undefined}
			rel={href && external ? "noopener noreferrer" : undefined}
			type={Tag == "a" ? undefined : buttonType}
			role={Tag == "a" ? "button" : undefined}
			onMouseDown={mouseDown}
			onKeyDown={keyDown}
			{...(props as any)}
		>
			{children}
		</Tag>
	);
}

export default Button;
