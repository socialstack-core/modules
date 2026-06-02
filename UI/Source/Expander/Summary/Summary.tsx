interface ExpanderSummaryProps {
	/**
	 * optional additional classes
	 */
	className?: string,

	/**
	 * summary label
	 */
	label?: string,

	/**
	 * 
	 */
	children?: React.ReactNode | React.ReactNode[] | undefined;
}

/**
 * The Expander Summary React component.
 * @param props React props.
 */
const Summary: React.FC<React.PropsWithChildren<ExpanderSummaryProps>> = (props) => {
	const { label, children, className } = props;

	const componentClasses = ['ui-expander__summary'];

	if (className?.length) {
		componentClasses.push(className);
	}

	return (
		<div className={componentClasses.join(' ')}>
			{children || label}
		</div>
	);
}

export default Summary;
