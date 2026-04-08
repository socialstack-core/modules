/**
 * The Wrapper React component.
 * @param props React props.
 */
const PopoverWrapper: React.FC = (props) => {
	const wrapperClasses = ['ui-popover__wrapper'];

	if (props.className?.length) {
		wrapperClasses.push(props.className);
	}

	return (
		<div className={wrapperClasses.join(' ')}>
			{props.children}
		</div>
	);
}

// required for preact (without preact/compat layer) support
PopoverWrapper.displayName = "PopoverWrapper";

export default PopoverWrapper;
